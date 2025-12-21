using Microsoft.EntityFrameworkCore;
using RewindPM.Web.Components;
using RewindPM.Infrastructure.Write;
using RewindPM.Infrastructure.Write.SQLite;
using RewindPM.Infrastructure.Write.Services;
using RewindPM.Infrastructure.Read;
using RewindPM.Infrastructure.Read.SQLite;
using RewindPM.Infrastructure.Read.Services;
using RewindPM.Application.Write;
using RewindPM.Application.Read;
using RewindPM.Projection;
using RewindPM.Web.Data;
using MediatR;
using RewindPM.Application.Read.Queries.Projects;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddOutputCache();

builder.Services.AddHttpClient();

// データベース接続文字列の取得
var eventStoreConnectionString = builder.Configuration.GetConnectionString("EventStore")
    ?? throw new InvalidOperationException("Connection string 'EventStore' not found.");
var readModelConnectionString = builder.Configuration.GetConnectionString("ReadModel")
    ?? throw new InvalidOperationException("Connection string 'ReadModel' not found.");

// Infrastructure.Write層の登録（DB非依存の共通サービス）
builder.Services.AddInfrastructureWrite();

// Infrastructure.Write.SQLite層の登録（EventStore, SqliteEventStore）
builder.Services.AddInfrastructureWriteSQLite(eventStoreConnectionString);

// Infrastructure.Read層の登録（DB非依存の共通サービス）
builder.Services.AddInfrastructureRead(builder.Configuration);

// Infrastructure.Read.SQLite層の登録（ReadModelRepository, ReadModelDbContext）
builder.Services.AddInfrastructureReadSQLite(readModelConnectionString);

// Application.Write層の登録（MediatR for Commands）
builder.Services.AddApplicationWrite();

// Application.Read層の登録（MediatR for Queries）
builder.Services.AddApplicationRead();

// Projection層の登録（Event Handlers）
// ProjectionInitializerがHostedServiceとしてアプリケーション起動時にイベントハンドラーを登録
// EventStoreへのアクセスをサービス経由で抽象化（CQRS境界を維持）
builder.Services.AddProjection(async (serviceProvider) =>
{
    using var scope = serviceProvider.CreateScope();
    var eventStoreReader = scope.ServiceProvider.GetRequiredService<RewindPM.Domain.Common.IEventStoreReader>();
    return await eventStoreReader.HasEventsAsync();
});

var app = builder.Build();

// データベースマイグレーションの自動適用
// - 保留中のマイグレーションがある場合: 常に適用（初回起動やスキーマ変更時）
// - 保留中のマイグレーションがない場合: AUTO_MIGRATE_DATABASE=true または開発環境でのみ再実行
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    // ReadModelデータベースの処理
    var readModelMigrationService = services.GetRequiredService<IReadModelMigrationService>();
    var hasPendingReadModelMigrations = await readModelMigrationService.HasPendingMigrationsAsync();

    if (hasPendingReadModelMigrations)
    {
        Console.WriteLine($"[Startup] Applying ReadModel migrations...");
        await readModelMigrationService.ApplyMigrationsAsync();
        Console.WriteLine($"[Startup] ReadModel migrations applied.");
    }
    else
    {
        var autoMigrate = builder.Configuration.GetValue<bool>("AUTO_MIGRATE_DATABASE", app.Environment.IsDevelopment());
        if (autoMigrate)
        {
            await readModelMigrationService.ApplyMigrationsAsync();
        }
    }

    // EventStoreデータベースの処理
    var eventStoreMigrationService = services.GetRequiredService<IEventStoreMigrationService>();
    var hasPendingEventStoreMigrations = await eventStoreMigrationService.HasPendingMigrationsAsync();

    if (hasPendingEventStoreMigrations)
    {
        Console.WriteLine($"[Startup] Applying EventStore migrations...");
        await eventStoreMigrationService.ApplyMigrationsAsync();
        Console.WriteLine($"[Startup] EventStore migrations applied.");
    }
    else
    {
        var autoMigrate = builder.Configuration.GetValue<bool>("AUTO_MIGRATE_DATABASE", app.Environment.IsDevelopment());
        if (autoMigrate)
        {
            await eventStoreMigrationService.ApplyMigrationsAsync();
        }
    }

    // タイムゾーン変更の検出とReadModel再構築
    var readModelRebuildService = services.GetRequiredService<RewindPM.Infrastructure.Read.Services.IReadModelRebuildService>();
    var readModelWasCleared = await readModelRebuildService.CheckAndRebuildIfTimeZoneChangedAsync();

    // タイムゾーン変更でデータがクリアされた場合、DbContextの変更を確実に反映させる
    if (readModelWasCleared)
    {
        // 変更を確実に反映するため、DbContextをリフレッシュ
        readModelMigrationService.ClearChangeTracking();
    }

    // ReadModelが空かどうかを確認（初回起動またはクリア後）
    var readModelIsEmpty = await readModelMigrationService.IsEmptyAsync();
    Console.WriteLine($"[Startup] ReadModel empty: {readModelIsEmpty}, ReadModel cleared by timezone change: {readModelWasCleared}");

    // ReadModelが空の場合、EventStoreからイベントをリプレイしてReadModelを再構築する
    // 注: タイムゾーン変更によってReadModelがクリアされた場合、readModelIsEmptyも必ず真になる
    if (readModelIsEmpty)
    {
        var eventReplayService = services.GetRequiredService<RewindPM.Projection.Services.IEventReplayService>();
        var hasEvents = await eventReplayService.HasEventsAsync();
        Console.WriteLine($"[Startup] EventStore has events: {hasEvents}");

        if (hasEvents)
        {
            Console.WriteLine("[Startup] Replaying events from EventStore to rebuild ReadModel...");
            eventReplayService.RegisterAllEventHandlers();

            // EventStoreからイベントデータを取得してリプレイ（サービス経由でCQRS境界を維持）
            var eventStoreReader = services.GetRequiredService<RewindPM.Domain.Common.IEventStoreReader>();
            await eventReplayService.ReplayAllEventsAsync(
                async (ct) => await eventStoreReader.GetAllEventsAsync(ct));

            Console.WriteLine("[Startup] ReadModel rebuild from EventStore completed.");

            // リプレイ後、タイムゾーンメタデータが存在しない場合は初期化
            // 注: この処理は意図的にif (hasEvents)ブロック内にある
            // EventStoreが空の場合、タイムゾーンメタデータの初期化は後のシードデータ実行時に行われる
            var storedTimeZone = await readModelRebuildService.GetStoredTimeZoneIdAsync();
            if (storedTimeZone == null)
            {
                await readModelRebuildService.InitializeTimeZoneMetadataAsync();
                Console.WriteLine("[Startup] Initialized timezone metadata.");
            }
        }

    }

    // 初回起動時にサンプルデータを追加
    var mediator = services.GetRequiredService<IMediator>();
    var projects = await mediator.Send(new GetAllProjectsQuery());
    Console.WriteLine($"[Startup] ReadModel project count: {projects.Count}");

    if (projects.Count == 0)
    {
        // ReadModelが空でもEventStoreに既存イベントがある場合、
        // シードでEventStoreへ書き込むのは避ける（タイムゾーン変更でReadModelのみ再構築したいケース）
        var eventStoreReader = services.GetRequiredService<RewindPM.Domain.Common.IEventStoreReader>();
        var hasEventStoreEvents = await eventStoreReader.HasEventsAsync();
        Console.WriteLine($"[Startup] EventStore has events: {hasEventStoreEvents}");

        if (hasEventStoreEvents)
        {
            Console.WriteLine("[Startup] ReadModel empty but EventStore contains events. Skipping seed to avoid writing to EventStore.");
        }
        else
        {
            Console.WriteLine("[Startup] Seeding sample data...");

            // SeedData実行前にProjectionハンドラーを登録（イベントをReadModelに反映するため）
            var eventReplayService = services.GetRequiredService<RewindPM.Projection.Services.IEventReplayService>();
            eventReplayService.RegisterAllEventHandlers();

            // ハンドラー登録後、シードデータを実行してイベントをReadModelに反映する
            var seedData = new SeedData(app.Services);
            await seedData.SeedAsync();
            Console.WriteLine("[Startup] Sample data seeded successfully.");
        }
    }
    else
    {
        Console.WriteLine($"[Startup] Database already contains {projects.Count} project(s). Skipping seed data.");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseAntiforgery();

app.UseOutputCache();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

// Weather API endpoint
string[] summaries = ["Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"];

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

// アプリケーション起動時にブラウザを自動的に開く
app.Lifetime.ApplicationStarted.Register(() =>
{
    try
    {
        // アプリケーションのURLを取得
        var addresses = app.Urls;
        if (addresses.Any())
        {
            var url = addresses.First();
            Console.WriteLine($"[Web] ブラウザを開きます: {url}");
            
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            var process = System.Diagnostics.Process.Start(psi);
            if (process == null)
            {
                Console.WriteLine("[Web] ブラウザの起動に失敗しました: Process.Startがnullを返しました");
            }
        }
        else
        {
            Console.WriteLine("[Web] アプリケーションのURLが見つかりません");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Web] ブラウザの起動に失敗しました: {ex.Message}");
    }
});

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

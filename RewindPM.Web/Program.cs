using Microsoft.EntityFrameworkCore;
using RewindPM.Web.Components;
using RewindPM.Infrastructure;
using RewindPM.Infrastructure.Read;
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

// Infrastructure層の登録（EventStore, EventPublisher, AggregateRepository）
builder.Services.AddInfrastructure(eventStoreConnectionString);

// Infrastructure.Read層の登録（ReadModelRepository, ReadModelDbContext, TimeZoneService）
builder.Services.AddInfrastructureRead(readModelConnectionString, builder.Configuration);

// Application.Write層の登録（MediatR for Commands）
builder.Services.AddApplicationWrite();

// Application.Read層の登録（MediatR for Queries）
builder.Services.AddApplicationRead();

// Projection層の登録（Event Handlers）
// ProjectionInitializerがHostedServiceとしてアプリケーション起動時にイベントハンドラーを登録
// EventStoreへのアクセスを提供するファクトリ関数を渡す
builder.Services.AddProjection(async () =>
{
    using var scope = builder.Services.BuildServiceProvider().CreateScope();
    var eventStoreContext = scope.ServiceProvider.GetRequiredService<RewindPM.Infrastructure.Write.Persistence.EventStoreDbContext>();
    return await eventStoreContext.Events.AnyAsync();
});

var app = builder.Build();

// データベースマイグレーションの自動適用
// - 保留中のマイグレーションがある場合: 常に適用（初回起動やスキーマ変更時）
// - 保留中のマイグレーションがない場合: AUTO_MIGRATE_DATABASE=true または開発環境でのみ再実行
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    // ReadModelデータベースの処理
    var readModelContext = services.GetRequiredService<RewindPM.Infrastructure.Read.Persistence.ReadModelDbContext>();
    var pendingReadModelMigrations = await readModelContext.Database.GetPendingMigrationsAsync();
    var hasPendingReadModelMigrations = pendingReadModelMigrations.Any();

    if (hasPendingReadModelMigrations)
    {
        Console.WriteLine($"[Startup] Applying ReadModel migrations...");
        await readModelContext.Database.MigrateAsync();
        Console.WriteLine($"[Startup] ReadModel migrations applied.");
    }
    else
    {
        var autoMigrate = builder.Configuration.GetValue<bool>("AUTO_MIGRATE_DATABASE", app.Environment.IsDevelopment());
        if (autoMigrate)
        {
            await readModelContext.Database.MigrateAsync();
        }
    }

    // EventStoreデータベースの処理
    var eventStoreContext = services.GetRequiredService<RewindPM.Infrastructure.Write.Persistence.EventStoreDbContext>();
    var pendingEventStoreMigrations = await eventStoreContext.Database.GetPendingMigrationsAsync();
    var hasPendingEventStoreMigrations = pendingEventStoreMigrations.Any();

    if (hasPendingEventStoreMigrations)
    {
        Console.WriteLine($"[Startup] Applying EventStore migrations...");
        await eventStoreContext.Database.MigrateAsync();
        Console.WriteLine($"[Startup] EventStore migrations applied.");
    }
    else
    {
        var autoMigrate = builder.Configuration.GetValue<bool>("AUTO_MIGRATE_DATABASE", app.Environment.IsDevelopment());
        if (autoMigrate)
        {
            await eventStoreContext.Database.MigrateAsync();
        }
    }

    // タイムゾーン変更の検出とReadModel再構築
    var readModelRebuildService = services.GetRequiredService<RewindPM.Infrastructure.Read.Services.IReadModelRebuildService>();
    var readModelWasCleared = await readModelRebuildService.CheckAndRebuildIfTimeZoneChangedAsync();

    // タイムゾーン変更でデータがクリアされた場合、DbContextの変更を確実に反映させる
    if (readModelWasCleared)
    {
        // 変更を確実に反映するため、DbContextをリフレッシュ
        readModelContext.ChangeTracker.Clear();
    }

    // ReadModelが空かどうかを確認（初回起動またはクリア後）
    var readModelIsEmpty = !await readModelContext.Projects.AnyAsync();
    Console.WriteLine($"[Startup] ReadModel empty: {readModelIsEmpty}, ReadModel cleared by timezone change: {readModelWasCleared}");

    // ReadModelが空で、EventStoreにイベントがある場合はプロジェクションをリプレイしてReadModelを再構築する
    if (readModelIsEmpty || readModelWasCleared)
    {
        var eventReplayService = services.GetRequiredService<RewindPM.Projection.Services.IEventReplayService>();
        var hasEvents = await eventReplayService.HasEventsAsync();
        Console.WriteLine($"[Startup] EventStore has events: {hasEvents}");

        if (hasEvents)
        {
            Console.WriteLine("[Startup] Replaying events from EventStore to rebuild ReadModel...");
            eventReplayService.RegisterAllEventHandlers();

            // EventStoreからイベントデータを取得してリプレイ
            var serializer = services.GetRequiredService<RewindPM.Infrastructure.Write.Serialization.DomainEventSerializer>();
            await eventReplayService.ReplayAllEventsAsync(async (ct) =>
            {
                var events = await eventStoreContext.Events
                    .OrderBy(e => e.OccurredAt)
                    .Select(e => new { e.EventType, e.EventData })
                    .ToListAsync(ct);

                return events.Select(e => (e.EventType, e.EventData)).ToList();
            });

            Console.WriteLine("[Startup] ReadModel rebuild from EventStore completed.");

            // リプレイ後、タイムゾーンメタデータが存在しない場合は初期化
            var storedTimeZone = await readModelRebuildService.GetStoredTimeZoneIdAsync();
            if (storedTimeZone == null)
            {
                var timeZoneService = services.GetRequiredService<RewindPM.Infrastructure.Read.Services.ITimeZoneService>();
                var currentTimeZone = timeZoneService.TimeZone.Id;
                
                // メタデータのみ追加（データはクリアしない）
                var metadataEntity = new RewindPM.Infrastructure.Read.Entities.SystemMetadataEntity
                {
                    Key = RewindPM.Infrastructure.Read.Entities.SystemMetadataEntity.TimeZoneMetadataKey,
                    Value = currentTimeZone
                };
                readModelContext.SystemMetadata.Add(metadataEntity);
                await readModelContext.SaveChangesAsync();
                
                Console.WriteLine($"[Startup] Initialized timezone metadata: {currentTimeZone}");
            }
        }

    }

    // 開発環境でサンプルデータを追加
    if (app.Environment.IsDevelopment())
    {
        var mediator = services.GetRequiredService<IMediator>();
        var projects = await mediator.Send(new GetAllProjectsQuery());
        Console.WriteLine($"[Startup] ReadModel project count: {projects.Count}");
        
        if (projects.Count == 0)
        {
            // ReadModelが空でもEventStoreに既存イベントがある場合、
            // シードでEventStoreへ書き込むのは避ける（タイムゾーン変更でReadModelのみ再構築したいケース）
            var hasEventStoreEvents = await eventStoreContext.Events.AnyAsync();
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

                // Projectionハンドラーは既にProjectionInitializerで登録済みのため、直接シード実行
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

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

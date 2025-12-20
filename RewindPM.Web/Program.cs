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
using RewindPM.Domain.Common;
using RewindPM.Domain.Events;
using RewindPM.Projection.Handlers;

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
builder.Services.AddProjection();

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

    // タイムゾーン変更の検出とReadModel再構築
    var timeZoneService = services.GetRequiredService<RewindPM.Infrastructure.Read.Services.ITimeZoneService>();

    // 現在保存されているタイムゾーンIDを取得
    var storedTimeZone = await readModelContext.SystemMetadata
        .Where(m => m.Key == RewindPM.Infrastructure.Read.Entities.SystemMetadataEntity.TimeZoneMetadataKey)
        .Select(m => m.Value)
        .FirstOrDefaultAsync();

    var configuredTimeZone = timeZoneService.TimeZone.Id;

    var readModelWasCleared = false;

    if (storedTimeZone != configuredTimeZone)
    {
        Console.WriteLine($"[Startup] TimeZone changed: {storedTimeZone ?? "none"} -> {configuredTimeZone}");
        Console.WriteLine($"[Startup] Rebuilding ReadModel database...");

        // トランザクション内でReadModelのクリアとメタデータ更新を実行
        using var transaction = await readModelContext.Database.BeginTransactionAsync();
        try
        {
            // ReadModelのデータをクリア(テーブル構造は維持)
            await readModelContext.Database.ExecuteSqlRawAsync("DELETE FROM TaskHistories");
            await readModelContext.Database.ExecuteSqlRawAsync("DELETE FROM ProjectHistories");
            await readModelContext.Database.ExecuteSqlRawAsync("DELETE FROM Tasks");
            await readModelContext.Database.ExecuteSqlRawAsync("DELETE FROM Projects");

            // タイムゾーンIDを更新
            var metadata = await readModelContext.SystemMetadata
                .FirstOrDefaultAsync(m => m.Key == RewindPM.Infrastructure.Read.Entities.SystemMetadataEntity.TimeZoneMetadataKey);

            if (metadata == null)
            {
                readModelContext.SystemMetadata.Add(new RewindPM.Infrastructure.Read.Entities.SystemMetadataEntity
                {
                    Key = RewindPM.Infrastructure.Read.Entities.SystemMetadataEntity.TimeZoneMetadataKey,
                    Value = configuredTimeZone
                });
            }
            else
            {
                metadata.Value = configuredTimeZone;
            }

            await readModelContext.SaveChangesAsync();
            await transaction.CommitAsync();

            Console.WriteLine($"[Startup] ReadModel cleared. Please re-create your data or import from EventStore.");
            readModelWasCleared = true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            Console.WriteLine($"[Startup ERROR] Failed to rebuild ReadModel: {ex.Message}");
            throw;
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

    // ReadModelをクリアした場合、EventStoreにイベントがあればプロジェクションをリプレイしてReadModelを再構築する
    if (readModelWasCleared)
    {
        var hasEvents = await eventStoreContext.Events.AnyAsync();
        if (hasEvents)
        {
            Console.WriteLine("[Startup] Replaying events from EventStore to rebuild ReadModel...");

            // Projectionハンドラーを登録（Program.csのスコープで即時利用するため）
            var eventPublisher = services.GetRequiredService<IEventPublisher>();
            eventPublisher.Subscribe<ProjectCreated>(
                new ScopedEventHandlerAdapter<ProjectCreated, ProjectCreatedEventHandler>(services));
            eventPublisher.Subscribe<ProjectUpdated>(
                new ScopedEventHandlerAdapter<ProjectUpdated, ProjectUpdatedEventHandler>(services));
            eventPublisher.Subscribe<ProjectDeleted>(
                new ScopedEventHandlerAdapter<ProjectDeleted, ProjectDeletedEventHandler>(services));
            eventPublisher.Subscribe<TaskCreated>(
                new ScopedEventHandlerAdapter<TaskCreated, TaskCreatedEventHandler>(services));
            eventPublisher.Subscribe<TaskUpdated>(
                new ScopedEventHandlerAdapter<TaskUpdated, TaskUpdatedEventHandler>(services));
            eventPublisher.Subscribe<TaskCompletelyUpdated>(
                new ScopedEventHandlerAdapter<TaskCompletelyUpdated, TaskCompletelyUpdatedEventHandler>(services));
            eventPublisher.Subscribe<TaskStatusChanged>(
                new ScopedEventHandlerAdapter<TaskStatusChanged, TaskStatusChangedEventHandler>(services));
            eventPublisher.Subscribe<TaskScheduledPeriodChanged>(
                new ScopedEventHandlerAdapter<TaskScheduledPeriodChanged, TaskScheduledPeriodChangedEventHandler>(services));
            eventPublisher.Subscribe<TaskActualPeriodChanged>(
                new ScopedEventHandlerAdapter<TaskActualPeriodChanged, TaskActualPeriodChangedEventHandler>(services));
            eventPublisher.Subscribe<TaskDeleted>(
                new ScopedEventHandlerAdapter<TaskDeleted, TaskDeletedEventHandler>(services));

            // シリアライザーを取得してEventStoreのイベントを時系列でリプレイ
            var serializer = services.GetRequiredService<RewindPM.Infrastructure.Write.Serialization.DomainEventSerializer>();
            var events = await eventStoreContext.Events
                .OrderBy(e => e.OccurredAt)
                .ToListAsync();

            foreach (var e in events)
            {
                try
                {
                    var domainEvent = serializer.Deserialize(e.EventType, e.EventData);
                    await eventPublisher.PublishAsync(domainEvent);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Startup WARN] Failed to replay event {e.EventId}: {ex.Message}");
                }
            }

            Console.WriteLine("[Startup] ReadModel rebuild from EventStore completed.");
        }
    }

    // 開発環境でサンプルデータを追加
    if (app.Environment.IsDevelopment())
    {
        var mediator = services.GetRequiredService<IMediator>();
        var projects = await mediator.Send(new GetAllProjectsQuery());
        if (projects.Count == 0)
        {
            // ReadModelが空でもEventStoreに既存イベントがある場合、
            // シードでEventStoreへ書き込むのは避ける（タイムゾーン変更でReadModelのみ再構築したいケース）
            var hasEventStoreEvents = await eventStoreContext.Events.AnyAsync();

            if (hasEventStoreEvents)
            {
                Console.WriteLine("[Startup] ReadModel empty but EventStore contains events. Skipping seed to avoid writing to EventStore.");
            }
            else
            {
                Console.WriteLine("[Startup] Seeding sample data...");

                // Projectionハンドラーを登録してからシードデータを実行
                // EventPublisherにすべてのプロジェクションハンドラーを登録
                var eventPublisher = services.GetRequiredService<IEventPublisher>();
                eventPublisher.Subscribe<ProjectCreated>(
                    new ScopedEventHandlerAdapter<ProjectCreated, ProjectCreatedEventHandler>(services));
                eventPublisher.Subscribe<ProjectUpdated>(
                    new ScopedEventHandlerAdapter<ProjectUpdated, ProjectUpdatedEventHandler>(services));
                eventPublisher.Subscribe<TaskCreated>(
                    new ScopedEventHandlerAdapter<TaskCreated, TaskCreatedEventHandler>(services));
                eventPublisher.Subscribe<TaskUpdated>(
                    new ScopedEventHandlerAdapter<TaskUpdated, TaskUpdatedEventHandler>(services));
                eventPublisher.Subscribe<TaskStatusChanged>(
                    new ScopedEventHandlerAdapter<TaskStatusChanged, TaskStatusChangedEventHandler>(services));
                eventPublisher.Subscribe<TaskScheduledPeriodChanged>(
                    new ScopedEventHandlerAdapter<TaskScheduledPeriodChanged, TaskScheduledPeriodChangedEventHandler>(services));
                eventPublisher.Subscribe<TaskActualPeriodChanged>(
                    new ScopedEventHandlerAdapter<TaskActualPeriodChanged, TaskActualPeriodChangedEventHandler>(services));

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

/// <summary>
/// スコープドハンドラーをシングルトンEventPublisherから呼び出すためのアダプター
/// 各イベント処理時に新しいスコープを作成してハンドラーを解決
/// </summary>
file class ScopedEventHandlerAdapter<TEvent, THandler> : IEventHandler<TEvent>
    where TEvent : class, IDomainEvent
    where THandler : IEventHandler<TEvent>
{
    private readonly IServiceProvider _serviceProvider;

    public ScopedEventHandlerAdapter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task HandleAsync(TEvent @event)
    {
        // 新しいスコープを作成してハンドラーを解決
        using var scope = _serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<THandler>();
        await handler.HandleAsync(@event);
    }
}

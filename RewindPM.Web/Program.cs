using Microsoft.EntityFrameworkCore;
using RewindPM.Web.Components;
using RewindPM.Infrastructure;
using RewindPM.Infrastructure.Read;
using RewindPM.Application.Write;
using RewindPM.Application.Read;
using RewindPM.Projection;

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

// Infrastructure.Read層の登録（ReadModelRepository, ReadModelDbContext）
builder.Services.AddInfrastructureRead(readModelConnectionString);

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

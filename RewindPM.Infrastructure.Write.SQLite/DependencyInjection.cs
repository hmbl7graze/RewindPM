using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RewindPM.Domain.Common;
using RewindPM.Infrastructure.Write.EventPublishing;
using RewindPM.Infrastructure.Write.SQLite.EventStore;
using RewindPM.Infrastructure.Write.SQLite.Persistence;

namespace RewindPM.Infrastructure.Write.SQLite;

/// <summary>
/// Infrastructure.Write.SQLite層のサービスをDIコンテナに登録するための拡張メソッド
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// SQLite EventStoreの実装をDIコンテナに登録する
    /// </summary>
    /// <param name="services">サービスコレクション</param>
    /// <param name="connectionString">EventStoreデータベースの接続文字列</param>
    /// <returns>サービスコレクション</returns>
    public static IServiceCollection AddInfrastructureWriteSQLite(
        this IServiceCollection services,
        string connectionString)
    {
        // EventStoreDbContextの登録
        services.AddDbContext<EventStoreDbContext>(options =>
            options.UseSqlite(connectionString));

        // SqliteEventStoreの登録（内部実装、スコープド：DbContextを使用するため）
        services.AddScoped<SqliteEventStore>();

        // IEventStoreの実装としてEventPublishingEventStoreDecoratorを登録
        // SqliteEventStoreをラップしてイベント発行機能を追加
        services.AddScoped<IEventStore>(sp =>
        {
            var innerStore = sp.GetRequiredService<SqliteEventStore>();
            var eventPublisher = sp.GetRequiredService<IEventPublisher>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<EventPublishingEventStoreDecorator>>();
            return new EventPublishingEventStoreDecorator(innerStore, eventPublisher, logger);
        });

        // IEventStoreReaderの実装をIEventStoreから取得できるように登録
        // SqliteEventStoreはIEventStoreを実装しており、IEventStoreはIEventStoreReaderを継承しているため、
        // IEventStoreReaderとしても使用可能
        services.AddScoped<IEventStoreReader>(sp => sp.GetRequiredService<IEventStore>());

        return services;
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RewindPM.Application.Write.Repositories;
using RewindPM.Domain.Common;
using RewindPM.Infrastructure.Write.EventPublishing;
using RewindPM.Infrastructure.Write.EventStore;
using RewindPM.Infrastructure.Write.Persistence;
using RewindPM.Infrastructure.Write.Repositories;
using RewindPM.Infrastructure.Write.Serialization;

namespace RewindPM.Infrastructure;

/// <summary>
/// Infrastructure層のサービスをDIコンテナに登録するための拡張メソッド
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Infrastructure層のサービスをDIコンテナに登録する
    /// </summary>
    /// <param name="services">サービスコレクション</param>
    /// <param name="connectionString">EventStoreデータベースの接続文字列</param>
    /// <returns>サービスコレクション</returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        // EventStoreDbContextの登録
        services.AddDbContext<EventStoreDbContext>(options =>
            options.UseSqlite(connectionString));

        // DomainEventSerializerの登録（シングルトン：ステートレスなため）
        services.AddSingleton<DomainEventSerializer>();

        // EventPublisherの登録（シングルトン：ハンドラー管理のため）
        services.AddSingleton<IEventPublisher, EventPublisher>();

        // 時刻プロバイダーの登録（シングルトン：ステートレスなため）
        services.AddSingleton<IDateTimeProvider, RewindPM.Infrastructure.Write.Services.SystemDateTimeProvider>();

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

        // IAggregateRepositoryの実装としてAggregateRepositoryを登録（スコープド：IEventStoreを使用するため）
        services.AddScoped<IAggregateRepository, AggregateRepository>();

        // IEventStoreReaderの実装をIEventStoreから取得できるように登録
        // SqliteEventStoreはIEventStoreを実装しており、IEventStoreはIEventStoreReaderを継承しているため、
        // IEventStoreReaderとしても使用可能
        services.AddScoped<IEventStoreReader>(sp => sp.GetRequiredService<IEventStore>());

        return services;
    }
}

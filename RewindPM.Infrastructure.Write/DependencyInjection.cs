using Microsoft.Extensions.DependencyInjection;
using RewindPM.Application.Write.Repositories;
using RewindPM.Domain.Common;
using RewindPM.Infrastructure.Write.EventPublishing;
using RewindPM.Infrastructure.Write.Repositories;
using RewindPM.Infrastructure.Write.Serialization;
using RewindPM.Infrastructure.Write.Services;

namespace RewindPM.Infrastructure.Write;

/// <summary>
/// Infrastructure.Write層のサービスをDIコンテナに登録するための拡張メソッド
/// DB非依存の共通サービスを登録
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Infrastructure.Write層の共通サービスをDIコンテナに登録する
    /// </summary>
    /// <param name="services">サービスコレクション</param>
    /// <returns>サービスコレクション</returns>
    public static IServiceCollection AddInfrastructureWrite(
        this IServiceCollection services)
    {
        // DomainEventSerializerの登録（シングルトン：ステートレスなため）
        services.AddSingleton<DomainEventSerializer>();

        // EventPublisherの登録（シングルトン：ハンドラー管理のため）
        services.AddSingleton<IEventPublisher, EventPublisher>();

        // 時刻プロバイダーの登録（シングルトン：ステートレスなため）
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();

        // IAggregateRepositoryの実装としてAggregateRepositoryを登録（スコープド：IEventStoreを使用するため）
        services.AddScoped<IAggregateRepository, AggregateRepository>();

        return services;
    }
}

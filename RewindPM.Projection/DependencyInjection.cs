using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RewindPM.Domain.Common;
using RewindPM.Projection.Handlers;
using RewindPM.Projection.Services;

namespace RewindPM.Projection;

/// <summary>
/// Projection層のサービスをDIコンテナに登録するための拡張メソッド
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Projection層のサービスをDIコンテナに登録する
    /// EventPublisherへのハンドラー登録はProjectionInitializerによってアプリケーション起動時に行われる
    /// </summary>
    /// <param name="services">サービスコレクション</param>
    /// <param name="hasEventsAsyncFunc">EventStoreにイベントが存在するかチェックする関数（必須）</param>
    /// <returns>サービスコレクション</returns>
    public static IServiceCollection AddProjection(
        this IServiceCollection services,
        Func<IServiceProvider, Task<bool>> hasEventsAsyncFunc)
    {
        // プロジェクションサービスを登録
        services.AddScoped<TaskSnapshotService>();

        // イベントリプレイサービスを登録
        services.AddScoped<IEventReplayService>(sp =>
        {
            var eventPublisher = sp.GetRequiredService<IEventPublisher>();
            var logger = sp.GetRequiredService<ILogger<EventReplayService>>();
            return new EventReplayService(eventPublisher, sp, logger, hasEventsAsyncFunc);
        });

        // プロジェクションハンドラーをスコープドで登録
        services.AddScoped<ProjectCreatedEventHandler>();
        services.AddScoped<ProjectUpdatedEventHandler>();
        services.AddScoped<ProjectDeletedEventHandler>();
        services.AddScoped<TaskCreatedEventHandler>();
        services.AddScoped<TaskUpdatedEventHandler>();
        services.AddScoped<TaskCompletelyUpdatedEventHandler>();
        services.AddScoped<TaskStatusChangedEventHandler>();
        services.AddScoped<TaskScheduledPeriodChangedEventHandler>();
        services.AddScoped<TaskActualPeriodChangedEventHandler>();
        services.AddScoped<TaskDeletedEventHandler>();

        // ProjectionInitializerをHostedServiceとして登録
        // アプリケーション起動時にEventPublisherにハンドラーを登録する
        services.AddHostedService<ProjectionInitializer>();

        return services;
    }
}

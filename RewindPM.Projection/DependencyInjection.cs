using Microsoft.Extensions.DependencyInjection;
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
    /// <returns>サービスコレクション</returns>
    public static IServiceCollection AddProjection(this IServiceCollection services)
    {
        // プロジェクションサービスを登録
        services.AddScoped<TaskSnapshotService>();

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

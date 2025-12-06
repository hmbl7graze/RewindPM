using Microsoft.Extensions.DependencyInjection;
using RewindPM.Domain.Common;
using RewindPM.Domain.Events;
using RewindPM.Projection.Handlers;

namespace RewindPM.Projection;

/// <summary>
/// Projection層のサービスをDIコンテナに登録するための拡張メソッド
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Projection層のサービスをDIコンテナに登録し、EventPublisherにハンドラーを登録する
    /// </summary>
    /// <param name="services">サービスコレクション</param>
    /// <param name="eventPublisher">イベント発行者（既にDIコンテナに登録済みのインスタンス）</param>
    /// <returns>サービスコレクション</returns>
    public static IServiceCollection AddProjection(
        this IServiceCollection services,
        IEventPublisher eventPublisher)
    {
        ArgumentNullException.ThrowIfNull(eventPublisher);

        // プロジェクションハンドラーをスコープドで登録
        services.AddScoped<ProjectCreatedEventHandler>();
        services.AddScoped<ProjectUpdatedEventHandler>();
        services.AddScoped<TaskCreatedEventHandler>();
        services.AddScoped<TaskUpdatedEventHandler>();
        services.AddScoped<TaskStatusChangedEventHandler>();

        // EventPublisherにハンドラーを登録
        // スコープドハンドラーをシングルトンEventPublisherから呼び出すためのアダプターを使用
        var serviceProvider = services.BuildServiceProvider();

        eventPublisher.Subscribe<ProjectCreated>(
            new ScopedEventHandlerAdapter<ProjectCreated, ProjectCreatedEventHandler>(serviceProvider));

        eventPublisher.Subscribe<ProjectUpdated>(
            new ScopedEventHandlerAdapter<ProjectUpdated, ProjectUpdatedEventHandler>(serviceProvider));

        eventPublisher.Subscribe<TaskCreated>(
            new ScopedEventHandlerAdapter<TaskCreated, TaskCreatedEventHandler>(serviceProvider));

        eventPublisher.Subscribe<TaskUpdated>(
            new ScopedEventHandlerAdapter<TaskUpdated, TaskUpdatedEventHandler>(serviceProvider));

        eventPublisher.Subscribe<TaskStatusChanged>(
            new ScopedEventHandlerAdapter<TaskStatusChanged, TaskStatusChangedEventHandler>(serviceProvider));

        return services;
    }

    /// <summary>
    /// スコープドハンドラーをシングルトンEventPublisherから呼び出すためのアダプター
    /// 各イベント処理時に新しいスコープを作成してハンドラーを解決
    /// </summary>
    private class ScopedEventHandlerAdapter<TEvent, THandler> : IEventHandler<TEvent>
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
}

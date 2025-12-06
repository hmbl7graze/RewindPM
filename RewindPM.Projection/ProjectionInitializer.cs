using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RewindPM.Domain.Common;
using RewindPM.Domain.Events;
using RewindPM.Projection.Handlers;

namespace RewindPM.Projection;

/// <summary>
/// アプリケーション起動時にEventPublisherにプロジェクションハンドラーを登録するホストサービス
/// </summary>
public class ProjectionInitializer : IHostedService
{
    private readonly IEventPublisher _eventPublisher;
    private readonly IServiceProvider _serviceProvider;

    public ProjectionInitializer(
        IEventPublisher eventPublisher,
        IServiceProvider serviceProvider)
    {
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // EventPublisherにすべてのプロジェクションハンドラーを登録
        _eventPublisher.Subscribe<ProjectCreated>(
            new ScopedEventHandlerAdapter<ProjectCreated, ProjectCreatedEventHandler>(_serviceProvider));

        _eventPublisher.Subscribe<ProjectUpdated>(
            new ScopedEventHandlerAdapter<ProjectUpdated, ProjectUpdatedEventHandler>(_serviceProvider));

        _eventPublisher.Subscribe<TaskCreated>(
            new ScopedEventHandlerAdapter<TaskCreated, TaskCreatedEventHandler>(_serviceProvider));

        _eventPublisher.Subscribe<TaskUpdated>(
            new ScopedEventHandlerAdapter<TaskUpdated, TaskUpdatedEventHandler>(_serviceProvider));

        _eventPublisher.Subscribe<TaskStatusChanged>(
            new ScopedEventHandlerAdapter<TaskStatusChanged, TaskStatusChangedEventHandler>(_serviceProvider));

        _eventPublisher.Subscribe<TaskScheduledPeriodChanged>(
            new ScopedEventHandlerAdapter<TaskScheduledPeriodChanged, TaskScheduledPeriodChangedEventHandler>(_serviceProvider));

        _eventPublisher.Subscribe<TaskActualPeriodChanged>(
            new ScopedEventHandlerAdapter<TaskActualPeriodChanged, TaskActualPeriodChangedEventHandler>(_serviceProvider));

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
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

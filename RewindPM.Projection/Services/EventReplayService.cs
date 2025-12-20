using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RewindPM.Domain.Common;
using RewindPM.Domain.Events;
using RewindPM.Projection.Handlers;

namespace RewindPM.Projection.Services;

/// <summary>
/// イベントストアからイベントをリプレイしてReadModelを再構築するサービス
/// </summary>
public class EventReplayService : IEventReplayService
{
    private readonly IEventPublisher _eventPublisher;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventReplayService> _logger;
    private readonly Func<IServiceProvider, Task<bool>> _hasEventsAsyncFunc;
    private bool _handlersRegistered = false;

    /// <summary>
    /// ReadModelの整合性に重大な影響を与える重要イベントのセット
    /// これらのイベントの処理に失敗した場合、リプレイ処理を中断する
    /// </summary>
    private static readonly HashSet<string> _criticalEvents = new()
    {
        "ProjectCreated",
        "TaskCreated"
    };

    public EventReplayService(
        IEventPublisher eventPublisher,
        IServiceProvider serviceProvider,
        ILogger<EventReplayService> logger,
        Func<IServiceProvider, Task<bool>> hasEventsAsyncFunc)
    {
        _eventPublisher = eventPublisher;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _hasEventsAsyncFunc = hasEventsAsyncFunc;
    }

    /// <inheritdoc/>
    public async Task<bool> HasEventsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return await _hasEventsAsyncFunc(_serviceProvider);
    }

    /// <inheritdoc/>
    public void RegisterAllEventHandlers()
    {
        // 重複登録を防ぐため、既に登録済みの場合はスキップ
        if (_handlersRegistered)
        {
            _logger.LogDebug("Event handlers already registered. Skipping duplicate registration.");
            return;
        }

        _logger.LogInformation("Registering all event handlers for replay...");

        _eventPublisher.Subscribe<ProjectCreated>(
            new ScopedEventHandlerAdapter<ProjectCreated, ProjectCreatedEventHandler>(_serviceProvider));
        _eventPublisher.Subscribe<ProjectUpdated>(
            new ScopedEventHandlerAdapter<ProjectUpdated, ProjectUpdatedEventHandler>(_serviceProvider));
        _eventPublisher.Subscribe<ProjectDeleted>(
            new ScopedEventHandlerAdapter<ProjectDeleted, ProjectDeletedEventHandler>(_serviceProvider));
        _eventPublisher.Subscribe<TaskCreated>(
            new ScopedEventHandlerAdapter<TaskCreated, TaskCreatedEventHandler>(_serviceProvider));
        _eventPublisher.Subscribe<TaskUpdated>(
            new ScopedEventHandlerAdapter<TaskUpdated, TaskUpdatedEventHandler>(_serviceProvider));
        _eventPublisher.Subscribe<TaskCompletelyUpdated>(
            new ScopedEventHandlerAdapter<TaskCompletelyUpdated, TaskCompletelyUpdatedEventHandler>(_serviceProvider));
        _eventPublisher.Subscribe<TaskStatusChanged>(
            new ScopedEventHandlerAdapter<TaskStatusChanged, TaskStatusChangedEventHandler>(_serviceProvider));
        _eventPublisher.Subscribe<TaskScheduledPeriodChanged>(
            new ScopedEventHandlerAdapter<TaskScheduledPeriodChanged, TaskScheduledPeriodChangedEventHandler>(_serviceProvider));
        _eventPublisher.Subscribe<TaskActualPeriodChanged>(
            new ScopedEventHandlerAdapter<TaskActualPeriodChanged, TaskActualPeriodChangedEventHandler>(_serviceProvider));
        _eventPublisher.Subscribe<TaskDeleted>(
            new ScopedEventHandlerAdapter<TaskDeleted, TaskDeletedEventHandler>(_serviceProvider));

        _handlersRegistered = true;
        _logger.LogInformation("All event handlers registered.");
    }

    /// <inheritdoc/>
    public async Task ReplayAllEventsAsync(
        Func<CancellationToken, Task<List<IDomainEvent>>> getEventsAsync,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting event replay from EventStore...");

        var events = await getEventsAsync(cancellationToken);

        _logger.LogInformation("Replaying {EventCount} events...", events.Count);

        foreach (var domainEvent in events)
        {
            try
            {
                await _eventPublisher.PublishAsync(domainEvent);
            }
            catch (Exception ex)
            {
                var eventType = domainEvent.GetType().Name;

                // 重要イベントの失敗は ReadModel の整合性に重大な影響を与えるため、処理を中断する
                if (_criticalEvents.Contains(eventType))
                {
                    _logger.LogError(
                        ex,
                        "Failed to replay critical event of type {EventType}. AggregateId: {AggregateId}",
                        eventType,
                        domainEvent.AggregateId);

                    // 重要イベントの欠落を無視すると ReadModel が恒久的に不完全になるため、例外を再スローしてリプレイ処理を中断する
                    throw;
                }

                _logger.LogWarning(
                    ex,
                    "Failed to replay event of type {EventType}. AggregateId: {AggregateId}",
                    eventType,
                    domainEvent.AggregateId);
            }
        }

        _logger.LogInformation("Event replay completed.");
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
            var logger = scope.ServiceProvider.GetService<ILogger<THandler>>();

            try
            {
                var handler = scope.ServiceProvider.GetRequiredService<THandler>();
                await handler.HandleAsync(@event);
            }
            catch (Exception ex)
            {
                // どのイベントをどのハンドラーで処理中に失敗したかをログ出力
                logger?.LogError(
                    ex,
                    "Error while handling event of type {EventType} with handler {HandlerType}.",
                    typeof(TEvent).FullName,
                    typeof(THandler).FullName);

                throw;
            }
        }
    }
}

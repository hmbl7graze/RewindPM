using System.Text.Json;
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

    /// <summary>
    /// EventStoreとの整合性を保つため、Write側と同じシリアライズオプションを使用
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        PropertyNameCaseInsensitive = true
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
        return await _hasEventsAsyncFunc(_serviceProvider);
    }

    /// <inheritdoc/>
    public void RegisterAllEventHandlers()
    {
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

        _logger.LogInformation("All event handlers registered.");
    }

    /// <inheritdoc/>
    public async Task ReplayAllEventsAsync(
        Func<CancellationToken, Task<List<(string EventType, string EventData)>>> getEventsAsync,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting event replay from EventStore...");

        var events = await getEventsAsync(cancellationToken);

        _logger.LogInformation("Replaying {EventCount} events...", events.Count);

        foreach (var (eventType, eventData) in events)
        {
            try
            {
                var domainEvent = DeserializeEvent(eventType, eventData);
                await _eventPublisher.PublishAsync(domainEvent);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to replay event of type {EventType}", eventType);
            }
        }

        _logger.LogInformation("Event replay completed.");
    }

    /// <summary>
    /// イベントデータを型情報に基づいてデシリアライズする
    /// </summary>
    private IDomainEvent DeserializeEvent(string eventType, string eventData)
    {
        var type = Type.GetType($"RewindPM.Domain.Events.{eventType}, RewindPM.Domain");
        if (type == null)
        {
            throw new InvalidOperationException($"Unknown event type: {eventType}");
        }

        var domainEvent = JsonSerializer.Deserialize(eventData, type, JsonOptions) as IDomainEvent;
        if (domainEvent == null)
        {
            throw new InvalidOperationException($"Failed to deserialize event: {eventType}");
        }

        return domainEvent;
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

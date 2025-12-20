using Microsoft.Extensions.Logging;
using RewindPM.Domain.Common;

namespace RewindPM.Infrastructure.Write.EventPublishing;

/// <summary>
/// EventStoreのデコレータ
/// イベント保存後にイベント発行を行う
/// </summary>
public class EventPublishingEventStoreDecorator : IEventStore
{
    private readonly IEventStore _innerEventStore;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<EventPublishingEventStoreDecorator> _logger;

    public EventPublishingEventStoreDecorator(
        IEventStore innerEventStore,
        IEventPublisher eventPublisher,
        ILogger<EventPublishingEventStoreDecorator> logger)
    {
        _innerEventStore = innerEventStore ?? throw new ArgumentNullException(nameof(innerEventStore));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// イベントを保存し、保存成功後にイベントを発行する
    /// </summary>
    public async Task SaveEventsAsync(Guid aggregateId, IEnumerable<IDomainEvent> events, int expectedVersion)
    {
        var eventList = events.ToList();

        // 内部のEventStoreに委譲（イベント保存）
        await _innerEventStore.SaveEventsAsync(aggregateId, eventList, expectedVersion);

        // イベント保存が成功したら、イベントを発行
        _logger.LogDebug("Publishing {EventCount} event(s) for aggregate {AggregateId}",
            eventList.Count, aggregateId);

        foreach (var @event in eventList)
        {
            await _eventPublisher.PublishAsync(@event);
        }
    }

    /// <summary>
    /// 指定されたAggregateの全イベントを取得する（委譲）
    /// </summary>
    public Task<List<IDomainEvent>> GetEventsAsync(Guid aggregateId)
    {
        return _innerEventStore.GetEventsAsync(aggregateId);
    }

    /// <summary>
    /// 指定された時点までのイベントを取得する（委譲）
    /// </summary>
    public Task<List<IDomainEvent>> GetEventsUntilAsync(Guid aggregateId, DateTimeOffset pointInTime)
    {
        return _innerEventStore.GetEventsUntilAsync(aggregateId, pointInTime);
    }

    /// <summary>
    /// 指定されたイベント種別のイベントを期間指定で取得する（委譲）
    /// </summary>
    public Task<List<IDomainEvent>> GetEventsByTypeAsync(string eventType, DateTimeOffset? from = null, DateTimeOffset? to = null)
    {
        return _innerEventStore.GetEventsByTypeAsync(eventType, from, to);
    }

    /// <summary>
    /// 指定されたプロジェクトに関連するタスクのIDリストを取得する（委譲）
    /// </summary>
    public Task<List<Guid>> GetTaskIdsByProjectIdAsync(Guid projectId)
    {
        return _innerEventStore.GetTaskIdsByProjectIdAsync(projectId);
    }

    /// <summary>
    /// EventStoreにイベントが存在するかチェックする（委譲、IEventStoreReaderの実装）
    /// </summary>
    public Task<bool> HasEventsAsync(CancellationToken cancellationToken = default)
    {
        return _innerEventStore.HasEventsAsync(cancellationToken);
    }

    /// <summary>
    /// EventStoreから全イベントを時系列順に取得する（委譲、IEventStoreReaderの実装）
    /// </summary>
    public Task<List<(string EventType, string EventData)>> GetAllEventsAsync(CancellationToken cancellationToken = default)
    {
        return _innerEventStore.GetAllEventsAsync(cancellationToken);
    }
}

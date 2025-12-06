using Microsoft.EntityFrameworkCore;
using RewindPM.Domain.Common;
using RewindPM.Infrastructure.Write.Entities;
using RewindPM.Infrastructure.Write.Persistence;
using RewindPM.Infrastructure.Write.Serialization;

namespace RewindPM.Infrastructure.Write.EventStore;

/// <summary>
/// SQLiteを使用したイベントストアの実装
/// イベントソーシングのためのイベント永続化を担当
/// </summary>
public class SqliteEventStore : IEventStore
{
    private readonly EventStoreDbContext _context;
    private readonly DomainEventSerializer _serializer;

    public SqliteEventStore(EventStoreDbContext context, DomainEventSerializer serializer)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
    }

    /// <summary>
    /// イベントを保存する
    /// 楽観的同時実行制御により、expectedVersionと実際のバージョンが一致することを確認
    /// </summary>
    public async Task SaveEventsAsync(Guid aggregateId, IEnumerable<IDomainEvent> events, int expectedVersion)
    {
        ArgumentNullException.ThrowIfNull(events);

        var eventList = events.ToList();
        if (!eventList.Any())
        {
            return; // イベントがない場合は何もしない
        }

        // トランザクション開始
        await using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // 現在のバージョンを取得
            var currentVersion = await _context.Events
                .Where(e => e.AggregateId == aggregateId)
                .MaxAsync(e => (int?)e.Version) ?? -1;

            // 楽観的同時実行制御チェック
            if (currentVersion != expectedVersion)
            {
                throw new ConcurrencyException(aggregateId, expectedVersion, currentVersion);
            }

            // イベントをエンティティに変換して保存
            var version = expectedVersion;
            foreach (var domainEvent in eventList)
            {
                version++;

                var eventEntity = new EventEntity
                {
                    EventId = domainEvent.EventId,
                    AggregateId = aggregateId,
                    EventType = domainEvent.EventType,
                    EventData = _serializer.Serialize(domainEvent),
                    OccurredAt = domainEvent.OccurredAt,
                    Version = version,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Events.Add(eventEntity);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// 指定されたAggregateの全イベントを取得する
    /// </summary>
    public async Task<List<IDomainEvent>> GetEventsAsync(Guid aggregateId)
    {
        var eventEntities = await _context.Events
            .Where(e => e.AggregateId == aggregateId)
            .OrderBy(e => e.Version)
            .ToListAsync();

        return eventEntities
            .Select(e => _serializer.Deserialize(e.EventType, e.EventData))
            .ToList();
    }

    /// <summary>
    /// 指定された時点までのイベントを取得する（タイムトラベル用）
    /// </summary>
    public async Task<List<IDomainEvent>> GetEventsUntilAsync(Guid aggregateId, DateTime pointInTime)
    {
        var eventEntities = await _context.Events
            .Where(e => e.AggregateId == aggregateId && e.OccurredAt <= pointInTime)
            .OrderBy(e => e.Version)
            .ToListAsync();

        return eventEntities
            .Select(e => _serializer.Deserialize(e.EventType, e.EventData))
            .ToList();
    }

    /// <summary>
    /// 指定されたイベント種別のイベントを期間指定で取得する
    /// </summary>
    public async Task<List<IDomainEvent>> GetEventsByTypeAsync(string eventType, DateTime? from = null, DateTime? to = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);

        var query = _context.Events.Where(e => e.EventType == eventType);

        if (from.HasValue)
        {
            query = query.Where(e => e.OccurredAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(e => e.OccurredAt <= to.Value);
        }

        var eventEntities = await query
            .OrderBy(e => e.OccurredAt)
            .ToListAsync();

        return eventEntities
            .Select(e => _serializer.Deserialize(e.EventType, e.EventData))
            .ToList();
    }
}

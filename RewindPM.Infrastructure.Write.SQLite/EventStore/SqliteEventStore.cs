using Microsoft.EntityFrameworkCore;
using RewindPM.Domain.Common;
using RewindPM.Infrastructure.Write.Serialization;
using RewindPM.Infrastructure.Write.SQLite.Entities;
using RewindPM.Infrastructure.Write.SQLite.Persistence;

namespace RewindPM.Infrastructure.Write.SQLite.EventStore;

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
                    CreatedAt = DateTimeOffset.UtcNow
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
    public async Task<List<IDomainEvent>> GetEventsUntilAsync(Guid aggregateId, DateTimeOffset pointInTime)
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
    public async Task<List<IDomainEvent>> GetEventsByTypeAsync(string eventType, DateTimeOffset? from = null, DateTimeOffset? to = null)
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

    /// <summary>
    /// 指定されたプロジェクトに関連するタスクのIDリストを取得する
    /// </summary>
    public async Task<List<Guid>> GetTaskIdsByProjectIdAsync(Guid projectId)
    {
        // TaskCreatedイベントを全て取得してデシリアライズし、該当プロジェクトのタスクIDを抽出
        var taskCreatedEvents = await _context.Events
            .Where(e => e.EventType == "TaskCreated")
            .ToListAsync();

        var createdTasks = taskCreatedEvents
            .Select(e => _serializer.Deserialize(e.EventType, e.EventData))
            .OfType<Domain.Events.TaskCreated>()
            .Where(e => e.ProjectId == projectId)
            .Select(e => e.AggregateId)
            .ToList();

        // TaskDeletedイベントで削除されたタスクIDを取得
        var deletedTasks = await _context.Events
            .Where(e => e.EventType == "TaskDeleted" && createdTasks.Contains(e.AggregateId))
            .Select(e => e.AggregateId)
            .ToListAsync();

        // 削除されていないタスクIDを返す
        return createdTasks.Except(deletedTasks).ToList();
    }

    /// <summary>
    /// EventStoreにイベントが存在するかチェックする（IEventStoreReaderの実装）
    /// </summary>
    public async Task<bool> HasEventsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Events.AnyAsync(cancellationToken);
    }

    /// <summary>
    /// EventStoreから全イベントを時系列順に取得する（IEventStoreReaderの実装）
    /// リプレイ処理で使用される。デシリアライズ済みのドメインイベントとして返す
    /// </summary>
    public async Task<List<IDomainEvent>> GetAllEventsAsync(CancellationToken cancellationToken = default)
    {
        var events = await _context.Events
            .OrderBy(e => e.OccurredAt)
            .Select(e => new { e.EventType, e.EventData })
            .ToListAsync(cancellationToken);

        var domainEvents = new List<IDomainEvent>();
        foreach (var e in events)
        {
            var domainEvent = _serializer.Deserialize(e.EventType, e.EventData);
            domainEvents.Add(domainEvent);
        }

        return domainEvents;
    }
}

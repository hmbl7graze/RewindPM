namespace RewindPM.Domain.Common;

/// <summary>
/// ドメインイベントの基本インターフェース
/// 全てのドメインイベントはこのインターフェースを実装する
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// イベントの一意識別子
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// このイベントが発生したAggregateのID
    /// </summary>
    Guid AggregateId { get; }

    /// <summary>
    /// イベントが発生した日時（UTC）
    /// </summary>
    DateTimeOffset OccurredAt { get; }

    /// <summary>
    /// イベントの型名（イベントストアでの識別用）
    /// </summary>
    string EventType { get; }
}

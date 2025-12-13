namespace RewindPM.Domain.Common;

/// <summary>
/// ドメインイベントの抽象基底クラス
/// イミュータブルなrecordとして実装し、イベントソーシングの原則に従う
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    /// <summary>
    /// イベントの一意識別子（自動生成）
    /// </summary>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// このイベントが発生したAggregateのID
    /// </summary>
    public required Guid AggregateId { get; init; }

    /// <summary>
    /// イベントが発生した日時（UTC、自動設定）
    /// </summary>
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// イベントの型名（リフレクションで取得）
    /// イベントストアでの識別に使用
    /// </summary>
    public string EventType => GetType().Name;
}

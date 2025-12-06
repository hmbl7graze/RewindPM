using RewindPM.Domain.Common;
using RewindPM.Domain.ValueObjects;

namespace RewindPM.Domain.Events;

/// <summary>
/// タスクが作成された時に発生するドメインイベント
/// </summary>
public record TaskCreated : DomainEvent
{
    /// <summary>
    /// 所属するプロジェクトのID
    /// </summary>
    public required Guid ProjectId { get; init; }

    /// <summary>
    /// タスクのタイトル
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// タスクの説明
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// 予定期間と工数
    /// </summary>
    public required ScheduledPeriod ScheduledPeriod { get; init; }

    /// <summary>
    /// タスクを作成したユーザーID
    /// </summary>
    public required string CreatedBy { get; init; }
}

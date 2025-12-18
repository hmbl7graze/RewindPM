using RewindPM.Domain.Common;
using RewindPM.Domain.ValueObjects;
using TaskStatus = RewindPM.Domain.ValueObjects.TaskStatus;

namespace RewindPM.Domain.Events;

/// <summary>
/// タスクの全プロパティが一括更新された時に発生するドメインイベント
/// </summary>
public record TaskCompletelyUpdated : DomainEvent
{
    /// <summary>
    /// 更新後のタイトル
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// 更新後の説明
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// 更新後のステータス
    /// </summary>
    public required TaskStatus Status { get; init; }

    /// <summary>
    /// 更新後の予定期間
    /// </summary>
    public required ScheduledPeriod ScheduledPeriod { get; init; }

    /// <summary>
    /// 更新後の実績期間
    /// </summary>
    public required ActualPeriod ActualPeriod { get; init; }

    /// <summary>
    /// タスクを更新したユーザーID
    /// </summary>
    public required string UpdatedBy { get; init; }
}

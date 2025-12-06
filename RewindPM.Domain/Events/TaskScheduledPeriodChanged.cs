using RewindPM.Domain.Common;
using RewindPM.Domain.ValueObjects;

namespace RewindPM.Domain.Events;

/// <summary>
/// タスクの予定期間が変更された時に発生するドメインイベント
/// </summary>
public record TaskScheduledPeriodChanged : DomainEvent
{
    /// <summary>
    /// 新しい予定期間
    /// </summary>
    public required ScheduledPeriod ScheduledPeriod { get; init; }

    /// <summary>
    /// スケジュールを変更したユーザーID
    /// </summary>
    public required string ChangedBy { get; init; }
}

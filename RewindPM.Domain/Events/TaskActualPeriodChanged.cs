using RewindPM.Domain.Common;
using RewindPM.Domain.ValueObjects;

namespace RewindPM.Domain.Events;

/// <summary>
/// タスクの実績期間が変更された時に発生するドメインイベント
/// </summary>
public record TaskActualPeriodChanged : DomainEvent
{
    /// <summary>
    /// 新しい実績期間
    /// </summary>
    public required ActualPeriod ActualPeriod { get; init; }

    /// <summary>
    /// 実績を変更したユーザーID
    /// </summary>
    public required string ChangedBy { get; init; }
}

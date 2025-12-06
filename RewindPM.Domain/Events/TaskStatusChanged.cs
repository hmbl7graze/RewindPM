using RewindPM.Domain.Common;
using RewindPM.Domain.ValueObjects;
using TaskStatus = RewindPM.Domain.ValueObjects.TaskStatus;

namespace RewindPM.Domain.Events;

/// <summary>
/// タスクのステータスが変更された時に発生するドメインイベント
/// </summary>
public record TaskStatusChanged : DomainEvent
{
    /// <summary>
    /// 変更前のステータス
    /// </summary>
    public required TaskStatus OldStatus { get; init; }

    /// <summary>
    /// 変更後のステータス
    /// </summary>
    public required TaskStatus NewStatus { get; init; }

    /// <summary>
    /// ステータスを変更したユーザーID
    /// </summary>
    public required string ChangedBy { get; init; }
}

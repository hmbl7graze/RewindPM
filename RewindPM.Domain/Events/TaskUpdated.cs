using RewindPM.Domain.Common;

namespace RewindPM.Domain.Events;

/// <summary>
/// タスクのタイトルまたは説明が更新された時に発生するドメインイベント
/// </summary>
public record TaskUpdated : DomainEvent
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
    /// タスクを更新したユーザーID
    /// </summary>
    public required string UpdatedBy { get; init; }
}

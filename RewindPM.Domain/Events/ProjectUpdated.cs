using RewindPM.Domain.Common;

namespace RewindPM.Domain.Events;

/// <summary>
/// プロジェクトが更新された時に発生するドメインイベント
/// </summary>
public record ProjectUpdated : DomainEvent
{
    /// <summary>
    /// 更新後のプロジェクトのタイトル
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// 更新後のプロジェクトの説明
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// プロジェクトを更新したユーザーID
    /// </summary>
    public required string UpdatedBy { get; init; }
}

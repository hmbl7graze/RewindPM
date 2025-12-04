using RewindPM.Domain.Common;

namespace RewindPM.Domain.Events;

/// <summary>
/// プロジェクトが作成された時に発生するドメインイベント
/// </summary>
public record ProjectCreated : DomainEvent
{
    /// <summary>
    /// プロジェクトのタイトル
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// プロジェクトの説明
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// プロジェクトを作成したユーザーID
    /// </summary>
    public required string CreatedBy { get; init; }
}

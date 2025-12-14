using RewindPM.Domain.Common;

namespace RewindPM.Domain.Events;

/// <summary>
/// タスクが削除されたことを表すドメインイベント
/// </summary>
public record TaskDeleted : DomainEvent
{
    /// <summary>
    /// 削除されたタスクが所属していたプロジェクトのID
    /// </summary>
    public required Guid ProjectId { get; init; }

    /// <summary>
    /// 削除を実行したユーザーID
    /// </summary>
    public required string DeletedBy { get; init; }

    /// <summary>
    /// 削除理由（オプション）
    /// </summary>
    public string Reason { get; init; } = string.Empty;
}

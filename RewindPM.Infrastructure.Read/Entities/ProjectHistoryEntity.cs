namespace RewindPM.Infrastructure.Read.Entities;

/// <summary>
/// プロジェクトの日次スナップショットを保持するエンティティ
/// 過去の任意の時点の状態を確認するために使用
/// </summary>
public class ProjectHistoryEntity
{
    /// <summary>
    /// スナップショットID（主キー）
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// プロジェクトID
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// スナップショットの基準日（ローカルタイムゾーンの日付の00:00:00 UTC）
    /// </summary>
    public DateTimeOffset SnapshotDate { get; set; }

    /// <summary>
    /// プロジェクト名（この日の最終状態）
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// プロジェクトの説明（この日の最終状態）
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 作成日時（UTC）
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// 更新日時（UTC）
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <summary>
    /// 作成者
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// 更新者
    /// </summary>
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// スナップショット作成日時（UTC）
    /// </summary>
    public DateTimeOffset SnapshotCreatedAt { get; set; }
}

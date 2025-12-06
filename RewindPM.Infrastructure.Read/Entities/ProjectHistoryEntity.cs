namespace RewindPM.Infrastructure.Read.Entities;

/// <summary>
/// プロジェクトの過去状態を保持するエンティティ
/// タイムトラベル機能のために日単位のスナップショットを保存
/// </summary>
public class ProjectHistoryEntity
{
    /// <summary>
    /// 履歴レコードID（主キー）
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 元のプロジェクトID
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// スナップショット日付（日単位、UTC）
    /// この日の最後の状態を保存
    /// </summary>
    public DateTime SnapshotDate { get; set; }

    /// <summary>
    /// プロジェクト名
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// プロジェクトの説明
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 作成日時（UTC）
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// 更新日時（UTC）
    /// この時点での最終更新日時
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// 作成者
    /// </summary>
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// 更新者
    /// この時点での最終更新者
    /// </summary>
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// スナップショット作成日時（UTC）
    /// このレコードがいつ作成されたか
    /// </summary>
    public DateTime SnapshotCreatedAt { get; set; }
}

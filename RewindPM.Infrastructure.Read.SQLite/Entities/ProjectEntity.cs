namespace RewindPM.Infrastructure.Read.SQLite.Entities;

/// <summary>
/// プロジェクトの現在状態を保持するエンティティ
/// ReadModelデータベースに保存される
/// </summary>
public class ProjectEntity
{
    /// <summary>
    /// プロジェクトID（主キー）
    /// </summary>
    public Guid Id { get; set; }

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
    /// 削除フラグ（論理削除）
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>
    /// 削除日時（UTC）
    /// </summary>
    public DateTimeOffset? DeletedAt { get; set; }

    /// <summary>
    /// 削除者
    /// </summary>
    public string? DeletedBy { get; set; }
}

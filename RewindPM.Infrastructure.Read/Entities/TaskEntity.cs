using TaskStatus = RewindPM.Domain.ValueObjects.TaskStatus;

namespace RewindPM.Infrastructure.Read.Entities;

/// <summary>
/// タスクの現在状態を保持するエンティティ
/// ReadModelデータベースに保存される
/// </summary>
public class TaskEntity
{
    /// <summary>
    /// タスクID（主キー）
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 所属するプロジェクトのID
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// タスク名
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// タスクの説明
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// タスクのステータス
    /// </summary>
    public TaskStatus Status { get; set; }

    // 予定期間と工数
    /// <summary>
    /// 予定開始日
    /// </summary>
    public DateTimeOffset? ScheduledStartDate { get; set; }

    /// <summary>
    /// 予定終了日
    /// </summary>
    public DateTimeOffset? ScheduledEndDate { get; set; }

    /// <summary>
    /// 予定工数（時間）
    /// </summary>
    public int? EstimatedHours { get; set; }

    // 実績期間と工数
    /// <summary>
    /// 実績開始日
    /// </summary>
    public DateTimeOffset? ActualStartDate { get; set; }

    /// <summary>
    /// 実績終了日
    /// </summary>
    public DateTimeOffset? ActualEndDate { get; set; }

    /// <summary>
    /// 実績工数（時間）
    /// </summary>
    public int? ActualHours { get; set; }

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

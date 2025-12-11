using TaskStatus = RewindPM.Domain.ValueObjects.TaskStatus;

namespace RewindPM.Infrastructure.Read.Entities;

/// <summary>
/// タスクの過去状態を保持するエンティティ
/// タイムトラベル機能のために日単位のスナップショットを保存
/// </summary>
public class TaskHistoryEntity
{
    /// <summary>
    /// 履歴レコードID（主キー）
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// 元のタスクID
    /// </summary>
    public Guid TaskId { get; set; }

    /// <summary>
    /// 所属するプロジェクトのID
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// スナップショット日付（日単位、UTC）
    /// この日の最後の状態を保存
    /// </summary>
    public DateTime SnapshotDate { get; set; }

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
    public DateTime? ScheduledStartDate { get; set; }

    /// <summary>
    /// 予定終了日
    /// </summary>
    public DateTime? ScheduledEndDate { get; set; }

    /// <summary>
    /// 予定工数（時間）
    /// </summary>
    public int? EstimatedHours { get; set; }

    // 実績期間と工数
    /// <summary>
    /// 実績開始日
    /// </summary>
    public DateTime? ActualStartDate { get; set; }

    /// <summary>
    /// 実績終了日
    /// </summary>
    public DateTime? ActualEndDate { get; set; }

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
    /// この時点での最終更新日時
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }

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
    public DateTimeOffset SnapshotCreatedAt { get; set; }
}

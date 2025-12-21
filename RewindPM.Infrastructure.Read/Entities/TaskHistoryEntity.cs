using RewindPM.Infrastructure.Read.Contracts;
using TaskStatus = RewindPM.Domain.ValueObjects.TaskStatus;

namespace RewindPM.Infrastructure.Read.Entities;

/// <summary>
/// タスクの日次スナップショットを保持するエンティティ
/// 過去の任意の時点の状態を確認するために使用
/// </summary>
public class TaskHistoryEntity : ITaskStatisticsData
{
    /// <summary>
    /// スナップショットID（主キー）
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// タスクID
    /// </summary>
    public Guid TaskId { get; set; }

    /// <summary>
    /// 所属するプロジェクトのID
    /// </summary>
    public Guid ProjectId { get; set; }

    /// <summary>
    /// スナップショットの基準日（ローカルタイムゾーンの日付の00:00:00 UTC）
    /// </summary>
    public DateTimeOffset SnapshotDate { get; set; }

    /// <summary>
    /// タスク名（この日の最終状態）
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// タスクの説明（この日の最終状態）
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// タスクのステータス（この日の最終状態）
    /// </summary>
    public TaskStatus Status { get; set; }

    // 予定期間と工数（この日の最終状態）
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

    // 実績期間と工数（この日の最終状態）
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
    /// スナップショット作成日時（UTC）
    /// </summary>
    public DateTimeOffset SnapshotCreatedAt { get; set; }
}

using TaskStatus = RewindPM.Domain.ValueObjects.TaskStatus;

namespace RewindPM.Infrastructure.Read.Contracts;

/// <summary>
/// タスクの統計計算に必要なデータを提供するインターフェース
/// TaskEntityとTaskHistoryEntityで共通利用される
/// </summary>
public interface ITaskStatisticsData
{
    /// <summary>
    /// タスクのステータス
    /// </summary>
    TaskStatus Status { get; }

    /// <summary>
    /// 予定工数（時間）
    /// </summary>
    int? EstimatedHours { get; }

    /// <summary>
    /// 実績工数（時間）
    /// </summary>
    int? ActualHours { get; }

    /// <summary>
    /// 予定開始日
    /// </summary>
    DateTimeOffset? ScheduledStartDate { get; }

    /// <summary>
    /// 予定終了日
    /// </summary>
    DateTimeOffset? ScheduledEndDate { get; }

    /// <summary>
    /// 実績開始日
    /// </summary>
    DateTimeOffset? ActualStartDate { get; }

    /// <summary>
    /// 実績終了日
    /// </summary>
    DateTimeOffset? ActualEndDate { get; }
}

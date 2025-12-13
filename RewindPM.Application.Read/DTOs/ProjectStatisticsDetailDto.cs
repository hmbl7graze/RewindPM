namespace RewindPM.Application.Read.DTOs;

/// <summary>
/// プロジェクト詳細画面用の統計情報
/// </summary>
public record ProjectStatisticsDetailDto
{
    // タスク状況
    public required int TotalTasks { get; init; }
    public required int CompletedTasks { get; init; }
    public required int InProgressTasks { get; init; }
    public required int InReviewTasks { get; init; }
    public required int TodoTasks { get; init; }

    // 工数統計
    public required int TotalEstimatedHours { get; init; }
    public required int TotalActualHours { get; init; }
    public required int RemainingEstimatedHours { get; init; }

    // スケジュール統計
    public required int OnTimeTasks { get; init; }
    public required int DelayedTasks { get; init; }
    public required double AverageDelayDays { get; init; }

    // 見積もり精度統計（作業期間ベース）
    /// <summary>
    /// 作業期間の見積もりが正確だったタスク数（誤差±10%以内または±1日以内）
    /// </summary>
    public required int AccurateEstimateTasks { get; init; }
    /// <summary>
    /// 作業期間の見積もりが実績より多かった（過大見積もり）タスク数
    /// </summary>
    public required int OverEstimateTasks { get; init; }
    /// <summary>
    /// 作業期間の見積もりが実績より少なかった（過小見積もり）タスク数
    /// </summary>
    public required int UnderEstimateTasks { get; init; }
    /// <summary>
    /// 作業期間見積もり誤差の平均（日数）
    /// </summary>
    public required double AverageEstimateErrorDays { get; init; }

    // 統計の基準日（リワインド対応）
    public required DateTimeOffset AsOfDate { get; init; }

    // 計算プロパティ
    /// <summary>
    /// 完了率（0-100）
    /// </summary>
    public double CompletionRate => TotalTasks > 0
        ? Math.Round((double)CompletedTasks / TotalTasks * 100, 1)
        : 0;

    /// <summary>
    /// 工数オーバーラン（時間）
    /// </summary>
    public int HoursOverrun => TotalActualHours - TotalEstimatedHours;

    /// <summary>
    /// 工数消費率（%）
    /// 実績工数 / 予定工数 * 100
    /// 100%を超える場合は予定より多くの工数を消費していることを示す
    /// </summary>
    public double HoursConsumptionRate => TotalEstimatedHours > 0
        ? Math.Round((double)TotalActualHours / TotalEstimatedHours * 100, 1)
        : 0;

    /// <summary>
    /// スケジュール遵守率（%）
    /// </summary>
    public double OnTimeRate
    {
        get
        {
            var completedCount = CompletedTasks;
            return completedCount > 0
                ? Math.Round((double)OnTimeTasks / completedCount * 100, 1)
                : 0;
        }
    }

    /// <summary>
    /// 見積もり精度率（%）
    /// 作業期間の見積もりが正確だったタスクの割合
    /// </summary>
    public double EstimateAccuracyRate
    {
        get
        {
            var completedCount = CompletedTasks;
            return completedCount > 0
                ? Math.Round((double)AccurateEstimateTasks / completedCount * 100, 1)
                : 0;
        }
    }
}

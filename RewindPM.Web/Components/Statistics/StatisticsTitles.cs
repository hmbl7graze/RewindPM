namespace RewindPM.Web.Components.Statistics;

/// <summary>
/// 統計情報のツールチップ説明文
/// 将来的にリソースファイル(.resx)への移行を想定
/// </summary>
public static class StatisticsTitles
{
    // タスク統計
    public const string CompletionRate =
        "完了率 = (完了済みのタスク数 / 全タスク数) × 100";

    public const string TotalTasks =
        "プロジェクトに登録されているタスクの総数";

    public const string CompletedTasks =
        "完了済みのタスク数\n（ステータスが「完了」のタスク）";

    public const string InProgressTasks =
        "現在進行中のタスク数\n（ステータスが「進行中」のタスク）";

    public const string InReviewTasks =
        "レビュー待ちのタスク数\n（ステータスが「レビュー中」のタスク）";

    public const string TodoTasks =
        "未着手のタスク数\n（ステータスが「未着手」のタスク）";

    // 工数統計
    public const string HoursConsumptionRate =
        "工数消費率 = (実績工数 / 予定工数) × 100\n100%超過は予定より多く消費";

    public const string TotalEstimatedHours =
        "全タスクの予定工数の合計";

    public const string TotalActualHours =
        "全タスクの実績工数の合計";

    public const string HoursOverrun =
        "工数差分 = 実績工数 - 予定工数\nプラス=超過、マイナス=節約";

    public const string RemainingEstimatedHours =
        "残予定工数 = 未完了タスクの(予定工数 - 実績工数)合計\nマイナスは0に丸める";

    // スケジュール統計
    public const string OnTimeRate =
        "期限内完了率 = (期限内完了数 / 完了済みタスク数) × 100";

    public const string OnTimeTasks =
        "予定終了日よりも実績終了日が早いタスクの数";

    public const string DelayedTasks =
        "予定終了日よりも実績終了日が遅いタスクの数";

    public const string AverageDelayDays =
        "平均遅延 = 遅延したタスクの(実績終了日 - 予定終了日)の平均";

    // 見積もり精度統計
    public const string EstimateAccuracyRate =
        "見積もり精度率 = (正確な見積り数 / 完了済みタスク数) × 100\n誤差±10%以内または±1日以内であれば正確とする";

    public const string AccurateEstimateTasks =
        "見積もりが正確だったタスクの数（誤差±10%以内または±1日以内）";

    public const string OverEstimateTasks =
        "実績より予定が多かったタスクの数（過大見積もり）";

    public const string UnderEstimateTasks =
        "実績より予定が少なかったタスクの数（過小見積もり）";

    public const string AverageEstimateErrorDays =
        "平均誤差 = 作業期間見積もりの誤差平均\n単位は日";
}

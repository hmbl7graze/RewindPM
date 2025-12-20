using TaskStatus = RewindPM.Domain.ValueObjects.TaskStatus;

namespace RewindPM.Infrastructure.Read.SQLite.Contracts;

/// <summary>
/// タスクの統計計算に必要なデータを提供するインターフェース。
/// 主に読み取りモデル（例: TaskEntity, TaskHistoryEntity）で実装され、
/// 統計集計ロジックに対してタスク単位の必要最小限の情報を提供する責務を持つ。
/// </summary>
/// <remarks>
/// <para>
/// このインターフェースを実装するクラスは「統計計算そのもの」は行わず、
/// 集計サービス等が統計値を算出するために必要な元データを正しく提供することに責務を限定する。
/// </para>
/// <para>
/// 各プロパティがnullableであるのは、値がまだ確定していない／未入力である場合を表現するためであり、
/// 「0」や「既定値」を意味するものではない。
/// 例えば、予定工数が未見積の場合や、実績工数がまだ記録されていない場合などはnullとする。
/// </para>
/// <para>
/// 統計計算においては、nullの値は「集計対象外」として扱うことを前提とする。
/// つまり、合計・平均・最大／最小などの計算時にnullを0や既定日付として置き換えてはならない。
/// 0や特定の日付を統計に含めたい場合は、実装クラス側で0やその日付を明示的に設定すること。
/// </para>
/// </remarks>
public interface ITaskStatisticsData
{
    /// <summary>
    /// タスクのステータス。
    /// </summary>
    /// <remarks>
    /// 統計計算では、ステータスごとの件数集計や、ステータス遷移別のリードタイム分析などに利用される。
    /// この値は必須でありnullにはならない。
    /// </remarks>
    TaskStatus Status { get; }

    /// <summary>
    /// 予定工数（時間）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// nullの場合は「未見積（予定工数が設定されていない）」ことを表す。
    /// 予定工数が0であることを明示したい場合は0を設定すること。
    /// </para>
    /// <para>
    /// 統計計算（予定工数の合計・平均など）では、nullのレコードは集計から除外する。
    /// すなわち、nullを0として合計や平均に含めてはならない。
    /// </para>
    /// </remarks>
    int? EstimatedHours { get; }

    /// <summary>
    /// 実績工数（時間）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// nullの場合は「まだ作業が行われておらず、実績工数が記録されていない」ことを表す。
    /// 実績が0時間であることを明示したい場合は0を設定すること。
    /// </para>
    /// <para>
    /// 統計計算（実績工数の合計・平均、予実差分の算出など）では、nullのレコードは
    /// 実績工数の集計対象から除外する前提とする。
    /// </para>
    /// </remarks>
    int? ActualHours { get; }

    /// <summary>
    /// 予定開始日。
    /// </summary>
    /// <remarks>
    /// <para>
    /// nullの場合は「予定開始日が未設定」であることを表す。
    /// </para>
    /// <para>
    /// 統計計算（予定期間の分析、予定開始遅延の算出など）では、
    /// 予定開始日がnullのレコードは該当する計算から除外する。
    /// </para>
    /// </remarks>
    DateTimeOffset? ScheduledStartDate { get; }

    /// <summary>
    /// 予定終了日。
    /// </summary>
    /// <remarks>
    /// <para>
    /// nullの場合は「予定終了日が未設定」であることを表す。
    /// </para>
    /// <para>
    /// 統計計算（予定工期、予定完了日ベースの予実比較など）では、
    /// 予定終了日がnullのレコードは該当する計算から除外する。
    /// </para>
    /// </remarks>
    DateTimeOffset? ScheduledEndDate { get; }

    /// <summary>
    /// 実績開始日。
    /// </summary>
    /// <remarks>
    /// <para>
    /// nullの場合は「まだ作業が開始されていない」または「開始日が記録されていない」ことを表す。
    /// </para>
    /// <para>
    /// 統計計算（リードタイム、着手遅延の分析など）では、
    /// 実績開始日がnullのレコードは開始日を必要とする計算から除外する。
    /// </para>
    /// </remarks>
    DateTimeOffset? ActualStartDate { get; }

    /// <summary>
    /// 実績終了日。
    /// </summary>
    /// <remarks>
    /// <para>
    /// nullの場合は「まだ作業が完了していない」または「完了日が記録されていない」ことを表す。
    /// </para>
    /// <para>
    /// 統計計算（実績工期、リードタイム、完了率の算出など）では、
    /// 実績終了日がnullのレコードは終了日を必要とする計算から除外する。
    /// </para>
    /// </remarks>
    DateTimeOffset? ActualEndDate { get; }
}

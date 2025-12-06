namespace RewindPM.Domain.ValueObjects;

/// <summary>
/// タスクの予定期間と見積工数を表すValue Object
/// イミュータブルで、バリデーションロジックを内包する
/// </summary>
public record ScheduledPeriod
{
    /// <summary>
    /// 予定開始日
    /// </summary>
    public DateTime StartDate { get; init; }

    /// <summary>
    /// 予定終了日
    /// </summary>
    public DateTime EndDate { get; init; }

    /// <summary>
    /// 見積工数（時間）
    /// </summary>
    public int EstimatedHours { get; init; }

    /// <summary>
    /// ScheduledPeriodのコンストラクタ
    /// バリデーションを行い、不正な値の場合は例外をスローする
    /// </summary>
    /// <param name="startDate">予定開始日</param>
    /// <param name="endDate">予定終了日</param>
    /// <param name="estimatedHours">見積工数（時間）</param>
    /// <exception cref="ArgumentException">バリデーションエラー</exception>
    public ScheduledPeriod(DateTime startDate, DateTime endDate, int estimatedHours)
    {
        if (endDate <= startDate)
        {
            throw new ArgumentException("予定終了日は予定開始日より後でなければなりません");
        }

        if (estimatedHours <= 0)
        {
            throw new ArgumentException("見積工数は正の数でなければなりません");
        }

        StartDate = startDate;
        EndDate = endDate;
        EstimatedHours = estimatedHours;
    }

    /// <summary>
    /// 予定期間の日数を計算する
    /// </summary>
    public int DurationInDays => (EndDate - StartDate).Days;
}

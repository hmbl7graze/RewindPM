namespace RewindPM.Domain.ValueObjects;

/// <summary>
/// タスクの予定期間と見積工数を表すValue Object
/// イミュータブルで、バリデーションロジックを内包する
/// 予定は未設定の可能性があるため、nullable型を使用
/// </summary>
public record ScheduledPeriod
{
    /// <summary>
    /// 予定開始日（未設定の場合はnull）
    /// </summary>
    public DateTimeOffset? StartDate { get; init; }

    /// <summary>
    /// 予定終了日（未設定の場合はnull）
    /// </summary>
    public DateTimeOffset? EndDate { get; init; }

    /// <summary>
    /// 見積工数（時間）（未設定の場合はnull）
    /// </summary>
    public int? EstimatedHours { get; init; }

    /// <summary>
    /// ScheduledPeriodのコンストラクタ
    /// バリデーションを行い、不正な値の場合は例外をスローする
    /// </summary>
    /// <param name="startDate">予定開始日（nullの場合は未設定）</param>
    /// <param name="endDate">予定終了日（nullの場合は未設定）</param>
    /// <param name="estimatedHours">見積工数（時間、nullの場合は未設定）</param>
    /// <exception cref="ArgumentException">バリデーションエラー</exception>
    public ScheduledPeriod(DateTimeOffset? startDate = null, DateTimeOffset? endDate = null, int? estimatedHours = null)
    {
        // 両方の日付が設定されている場合、終了日は開始日より後でなければならない
        if (startDate.HasValue && endDate.HasValue && endDate.Value <= startDate.Value)
        {
            throw new ArgumentException("予定終了日は予定開始日より後でなければなりません");
        }

        // 工数が設定されている場合、正の数でなければならない
        if (estimatedHours.HasValue && estimatedHours.Value <= 0)
        {
            throw new ArgumentException("見積工数は正の数でなければなりません");
        }

        StartDate = startDate;
        EndDate = endDate;
        EstimatedHours = estimatedHours;
    }

    /// <summary>
    /// 予定期間の日数を計算する（両方の日付が設定されている場合のみ）
    /// </summary>
    public int? DurationInDays => StartDate.HasValue && EndDate.HasValue 
        ? (EndDate.Value - StartDate.Value).Days 
        : null;
}

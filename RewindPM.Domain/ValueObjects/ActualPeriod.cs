namespace RewindPM.Domain.ValueObjects;

/// <summary>
/// タスクの実績期間と実績工数を表すValue Object
/// イミュータブルで、バリデーションロジックを内包する
/// 実績は未設定の可能性があるため、nullable型を使用
/// </summary>
public record ActualPeriod
{
    /// <summary>
    /// 実績開始日（未開始の場合はnull）
    /// </summary>
    public DateTime? StartDate { get; init; }

    /// <summary>
    /// 実績終了日（未完了の場合はnull）
    /// </summary>
    public DateTime? EndDate { get; init; }

    /// <summary>
    /// 実績工数（時間）（未設定の場合はnull）
    /// </summary>
    public int? ActualHours { get; init; }

    /// <summary>
    /// ActualPeriodのコンストラクタ
    /// バリデーションを行い、不正な値の場合は例外をスローする
    /// </summary>
    /// <param name="startDate">実績開始日（nullの場合は未開始）</param>
    /// <param name="endDate">実績終了日（nullの場合は未完了）</param>
    /// <param name="actualHours">実績工数（時間、nullの場合は未設定）</param>
    /// <exception cref="ArgumentException">バリデーションエラー</exception>
    public ActualPeriod(DateTime? startDate = null, DateTime? endDate = null, int? actualHours = null)
    {
        // 両方の日付が設定されている場合、終了日は開始日より後でなければならない
        if (startDate.HasValue && endDate.HasValue && endDate.Value <= startDate.Value)
        {
            throw new ArgumentException("実績終了日は実績開始日より後でなければなりません");
        }

        // 工数が設定されている場合、正の数でなければならない
        if (actualHours.HasValue && actualHours.Value <= 0)
        {
            throw new ArgumentException("実績工数は正の数でなければなりません");
        }

        StartDate = startDate;
        EndDate = endDate;
        ActualHours = actualHours;
    }

    /// <summary>
    /// 実績期間の日数を計算する
    /// 開始日または終了日が未設定の場合はnullを返す
    /// </summary>
    public int? DurationInDays =>
        StartDate.HasValue && EndDate.HasValue
            ? (EndDate.Value - StartDate.Value).Days
            : null;

    /// <summary>
    /// タスクが開始されているかを判定する
    /// </summary>
    public bool IsStarted => StartDate.HasValue;

    /// <summary>
    /// タスクが完了しているかを判定する
    /// </summary>
    public bool IsCompleted => EndDate.HasValue;
}

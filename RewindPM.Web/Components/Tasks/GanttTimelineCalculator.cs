using RewindPM.Application.Read.DTOs;

namespace RewindPM.Web.Components.Tasks;

/// <summary>
/// ガントチャートのタイムライン計算を行うクラス
/// </summary>
public class GanttTimelineCalculator
{
    private DateTime? _timelineStart;
    private DateTime? _timelineEnd;
    private int _totalDays;

    /// <summary>
    /// タイムラインの開始日
    /// </summary>
    public DateTime? TimelineStart => _timelineStart;

    /// <summary>
    /// タイムラインの終了日
    /// </summary>
    public DateTime? TimelineEnd => _timelineEnd;

    /// <summary>
    /// 全体の日数
    /// </summary>
    public int TotalDays => _totalDays;

    /// <summary>
    /// タイムラインを計算
    /// </summary>
    /// <param name="tasks">タスクリスト</param>
    /// <returns>タイムライン範囲が変更されたかどうか</returns>
    public bool CalculateTimeline(List<TaskDto> tasks)
    {
        var previousStart = _timelineStart;
        var previousEnd = _timelineEnd;
        var previousDays = _totalDays;

        if (tasks == null || !tasks.Any())
        {
            _timelineStart = null;
            _timelineEnd = null;
            _totalDays = 0;
            return HasChanged(previousStart, previousEnd, previousDays);
        }

        var allDates = GetAllDatesFromTasks(tasks);

        if (!allDates.Any())
        {
            _timelineStart = null;
            _timelineEnd = null;
            _totalDays = 0;
            return HasChanged(previousStart, previousEnd, previousDays);
        }

        _timelineStart = allDates.Min().Date;
        _timelineEnd = allDates.Max().Date;
        _totalDays = (int)(_timelineEnd.Value - _timelineStart.Value).TotalDays + 1;

        return HasChanged(previousStart, previousEnd, previousDays);
    }

    /// <summary>
    /// タスクから全ての日付を抽出
    /// </summary>
    private static List<DateTimeOffset> GetAllDatesFromTasks(List<TaskDto> tasks)
    {
        var allDates = new List<DateTimeOffset>();

        foreach (var task in tasks)
        {
            if (task.ScheduledStartDate.HasValue)
                allDates.Add(task.ScheduledStartDate.Value);
            if (task.ScheduledEndDate.HasValue)
                allDates.Add(task.ScheduledEndDate.Value);
            if (task.ActualStartDate.HasValue)
                allDates.Add(task.ActualStartDate.Value);
            if (task.ActualEndDate.HasValue)
                allDates.Add(task.ActualEndDate.Value);
        }

        return allDates;
    }

    /// <summary>
    /// タイムラインが変更されたかどうかを判定
    /// </summary>
    private bool HasChanged(DateTime? previousStart, DateTime? previousEnd, int previousDays)
    {
        if (previousStart == null || previousEnd == null)
        {
            return true;
        }

        return _timelineStart != previousStart
            || _timelineEnd != previousEnd
            || _totalDays != previousDays;
    }

    /// <summary>
    /// グリッドカラムのスタイルを取得
    /// </summary>
    /// <param name="startDate">開始日</param>
    /// <param name="endDate">終了日</param>
    public string GetGridColumn(DateTimeOffset startDate, DateTimeOffset endDate)
    {
        if (_timelineStart == null) return "1 / 2";

        var startDay = (int)(startDate.Date - _timelineStart.Value).TotalDays + 1;
        var endDay = (int)(endDate.Date - _timelineStart.Value).TotalDays + 2;

        startDay = Math.Max(1, Math.Min(startDay, _totalDays));
        endDay = Math.Max(startDay + 1, Math.Min(endDay, _totalDays + 1));

        return $"{startDay} / {endDay}";
    }

    /// <summary>
    /// 月グループを取得
    /// </summary>
    public List<MonthGroup> GetMonthGroups()
    {
        var groups = new List<MonthGroup>();
        if (_timelineStart == null || _totalDays == 0) return groups;

        var processedDays = 0;
        while (processedDays < _totalDays)
        {
            var currentDate = _timelineStart.Value.AddDays(processedDays);
            var currentMonth = currentDate.Month;
            var currentYear = currentDate.Year;
            var startColumn = processedDays + 1;

            var daysInThisMonth = CountDaysInMonth(processedDays, currentMonth, currentYear);

            var endColumn = startColumn + daysInThisMonth;
            groups.Add(new MonthGroup
            {
                Year = currentYear,
                Month = currentMonth,
                StartColumn = startColumn,
                EndColumn = endColumn,
                DayCount = daysInThisMonth
            });

            processedDays += daysInThisMonth;
        }

        return groups;
    }

    /// <summary>
    /// 指定月の日数をカウント
    /// </summary>
    private int CountDaysInMonth(int processedDays, int currentMonth, int currentYear)
    {
        var daysInThisMonth = 0;
        for (int i = processedDays; i < _totalDays; i++)
        {
            var checkDate = _timelineStart!.Value.AddDays(i);
            if (checkDate.Month == currentMonth && checkDate.Year == currentYear)
            {
                daysInThisMonth++;
            }
            else
            {
                break;
            }
        }
        return daysInThisMonth;
    }

    /// <summary>
    /// 月グループを表すクラス
    /// </summary>
    public class MonthGroup
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int StartColumn { get; set; }
        public int EndColumn { get; set; }
        public int DayCount { get; set; }
    }
}

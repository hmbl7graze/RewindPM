using RewindPM.Application.Read.DTOs;
using RewindPM.Web.Components.Shared;

namespace RewindPM.Web.Components.Tasks;

/// <summary>
/// ガントチャートのバーアニメーション機能を管理するクラス
/// </summary>
public class GanttBarAnimationManager
{
    private Dictionary<string, BarState> _previousBarStates = new();
    private Dictionary<string, BarState> _displayBarStates = new();
    private HashSet<string> _barsToAnimate = new();
    private readonly HashSet<string> _fadingOutBars = new();

    /// <summary>
    /// 表示するバーの状態
    /// </summary>
    public Dictionary<string, BarState> DisplayBarStates => _displayBarStates;

    /// <summary>
    /// アニメーション対象のバー
    /// </summary>
    public HashSet<string> BarsToAnimate => _barsToAnimate;

    /// <summary>
    /// フェードアウト中のバー
    /// </summary>
    public HashSet<string> FadingOutBars => _fadingOutBars;

    /// <summary>
    /// 初期ロードかどうかを判定
    /// </summary>
    public bool IsInitialLoad() => _previousBarStates.Count == 0;

    /// <summary>
    /// バーの状態を初期化
    /// </summary>
    /// <param name="tasks">タスクリスト</param>
    public void InitializeBarStates(List<TaskDto> tasks)
    {
        if (tasks == null) return;

        _previousBarStates.Clear();

        foreach (var task in tasks)
        {
            if (task.ScheduledStartDate.HasValue && task.ScheduledEndDate.HasValue)
            {
                var key = GetBarKey(task.Id, "scheduled");
                _previousBarStates[key] = new BarState(task.ScheduledStartDate.Value, task.ScheduledEndDate.Value);
            }

            if (task.ActualStartDate.HasValue && task.ActualEndDate.HasValue)
            {
                var key = GetBarKey(task.Id, "actual");
                _previousBarStates[key] = new BarState(task.ActualStartDate.Value, task.ActualEndDate.Value);
            }
        }
    }

    /// <summary>
    /// 表示するバーの状態を初期化
    /// </summary>
    public void InitializeDisplayBarStates()
    {
        _displayBarStates = _previousBarStates.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// 現在のバー状態を取得
    /// </summary>
    /// <param name="tasks">タスクリスト</param>
    public Dictionary<string, BarState> GetCurrentBarStates(List<TaskDto> tasks)
    {
        var currentBarStates = new Dictionary<string, BarState>();
        if (tasks == null) return currentBarStates;

        foreach (var task in tasks)
        {
            if (task.ScheduledStartDate.HasValue && task.ScheduledEndDate.HasValue)
            {
                var key = GetBarKey(task.Id, "scheduled");
                currentBarStates[key] = new BarState(task.ScheduledStartDate.Value, task.ScheduledEndDate.Value);
            }

            if (task.ActualStartDate.HasValue && task.ActualEndDate.HasValue)
            {
                var key = GetBarKey(task.Id, "actual");
                currentBarStates[key] = new BarState(task.ActualStartDate.Value, task.ActualEndDate.Value);
            }
        }

        return currentBarStates;
    }

    /// <summary>
    /// 削除されたバーを取得
    /// </summary>
    /// <param name="currentBarStates">現在のバー状態</param>
    public List<string> GetRemovedBars(Dictionary<string, BarState> currentBarStates)
    {
        return _previousBarStates.Keys
            .Where(key => !currentBarStates.ContainsKey(key))
            .ToList();
    }

    /// <summary>
    /// バー削除のアニメーションを準備
    /// </summary>
    /// <param name="removedBars">削除されたバーのキーリスト</param>
    public void PrepareBarRemovalAnimation(List<string> removedBars)
    {
        // フェードアウト対象をマーク
        foreach (var barKey in removedBars)
        {
            _fadingOutBars.Add(barKey);
        }

        // 表示バーは前回の状態を維持(削除されるバーも表示し続ける)
        _displayBarStates = _previousBarStates.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// バー削除のアニメーション完了処理
    /// </summary>
    public void CompleteBarRemovalAnimation()
    {
        _fadingOutBars.Clear();
        UpdateDisplayBarStates();
    }

    /// <summary>
    /// 表示するバーの状態を更新
    /// </summary>
    public void UpdateDisplayBarStates()
    {
        _displayBarStates = _previousBarStates.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    /// <summary>
    /// 変更を検出して前回の状態を更新
    /// </summary>
    /// <param name="tasks">タスクリスト</param>
    public void DetectChangesAndUpdate(List<TaskDto> tasks)
    {
        if (tasks == null) return;

        var newBarsToAnimate = new HashSet<string>();
        var currentBarStates = new Dictionary<string, BarState>();

        foreach (var task in tasks)
        {
            if (task.ScheduledStartDate.HasValue && task.ScheduledEndDate.HasValue)
            {
                var key = GetBarKey(task.Id, "scheduled");
                var currentState = new BarState(task.ScheduledStartDate.Value, task.ScheduledEndDate.Value);
                currentBarStates[key] = currentState;

                if (_previousBarStates.TryGetValue(key, out var previousState))
                {
                    if (!previousState.Equals(currentState))
                    {
                        newBarsToAnimate.Add(key);
                    }
                }
                else
                {
                    newBarsToAnimate.Add(key);
                }
            }

            if (task.ActualStartDate.HasValue && task.ActualEndDate.HasValue)
            {
                var key = GetBarKey(task.Id, "actual");
                var currentState = new BarState(task.ActualStartDate.Value, task.ActualEndDate.Value);
                currentBarStates[key] = currentState;

                if (_previousBarStates.TryGetValue(key, out var previousState))
                {
                    if (!previousState.Equals(currentState))
                    {
                        newBarsToAnimate.Add(key);
                    }
                }
                else
                {
                    newBarsToAnimate.Add(key);
                }
            }
        }

        _previousBarStates = currentBarStates;
        _barsToAnimate = newBarsToAnimate;
    }

    /// <summary>
    /// 前回のバー状態を更新
    /// </summary>
    /// <param name="currentBarStates">現在のバー状態</param>
    public void UpdatePreviousBarStates(Dictionary<string, BarState> currentBarStates)
    {
        _previousBarStates = currentBarStates;
    }

    /// <summary>
    /// バーのキーを生成
    /// </summary>
    /// <param name="taskId">タスクID</param>
    /// <param name="barType">バータイプ（scheduled または actual）</param>
    public static string GetBarKey(Guid taskId, string barType)
    {
        return $"{taskId}_{barType}";
    }

    /// <summary>
    /// バーを表示すべきかどうかを判定
    /// </summary>
    /// <param name="taskId">タスクID</param>
    /// <param name="barType">バータイプ（scheduled または actual）</param>
    public bool ShouldDisplayBar(Guid taskId, string barType)
    {
        var key = GetBarKey(taskId, barType);
        return _displayBarStates.ContainsKey(key);
    }

    /// <summary>
    /// 表示するバーの状態を取得
    /// </summary>
    /// <param name="taskId">タスクID</param>
    /// <param name="barType">バータイプ（scheduled または actual）</param>
    public BarState? GetDisplayBarState(Guid taskId, string barType)
    {
        var key = GetBarKey(taskId, barType);
        return _displayBarStates.TryGetValue(key, out var state) ? state : null;
    }

    /// <summary>
    /// バーのアニメーションクラスを取得
    /// </summary>
    /// <param name="taskId">タスクID</param>
    /// <param name="barType">バータイプ（scheduled または actual）</param>
    public string GetBarAnimationClass(Guid taskId, string barType)
    {
        var key = GetBarKey(taskId, barType);
        if (_fadingOutBars.Contains(key))
        {
            return "fading-out";
        }
        if (_barsToAnimate.Contains(key))
        {
            return "animating";
        }
        return "";
    }

    /// <summary>
    /// バーの状態を表すレコード
    /// </summary>
    public record BarState(DateTimeOffset StartDate, DateTimeOffset EndDate)
    {
        public virtual bool Equals(BarState? other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other is null) return false;
            return StartDate.UtcDateTime == other.StartDate.UtcDateTime
                && EndDate.UtcDateTime == other.EndDate.UtcDateTime;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(StartDate.UtcDateTime, EndDate.UtcDateTime);
        }
    }
}

namespace RewindPM.Web.Components.Tasks;

/// <summary>
/// ガントチャートのズーム機能を管理するクラス
/// </summary>
public class GanttZoomManager
{
    private double _horizontalZoomScale = 1.0;
    private double _verticalZoomScale = 1.0;
    private double _baseColumnWidth = GanttConstants.CellWidth.DefaultBase;
    private double _baseRowHeight = GanttConstants.RowHeight.DefaultBase;
    private int _horizontalZoomLevelIndex = 0;
    private int _verticalZoomLevelIndex = 0;
    private bool _isBaseColumnWidthCalculated = false;
    private bool _isBaseRowHeightCalculated = false;

    /// <summary>
    /// 横方向ズームスケール
    /// </summary>
    public double HorizontalZoomScale => _horizontalZoomScale;

    /// <summary>
    /// 縦方向ズームスケール
    /// </summary>
    public double VerticalZoomScale => _verticalZoomScale;

    /// <summary>
    /// 基準セル幅
    /// </summary>
    public double BaseColumnWidth => _baseColumnWidth;

    /// <summary>
    /// 基準行高さ
    /// </summary>
    public double BaseRowHeight => _baseRowHeight;

    /// <summary>
    /// 横方向ズームレベルインデックス
    /// </summary>
    public int HorizontalZoomLevelIndex => _horizontalZoomLevelIndex;

    /// <summary>
    /// 縦方向ズームレベルインデックス
    /// </summary>
    public int VerticalZoomLevelIndex => _verticalZoomLevelIndex;

    /// <summary>
    /// 横方向にズームインできるかどうか
    /// </summary>
    public bool CanZoomInHorizontal => _horizontalZoomLevelIndex < GanttConstants.ZoomLevels.Count - 1;

    /// <summary>
    /// 横方向にズームアウトできるかどうか
    /// </summary>
    public bool CanZoomOutHorizontal => _horizontalZoomLevelIndex > 0;

    /// <summary>
    /// 縦方向にズームインできるかどうか
    /// </summary>
    public bool CanZoomInVertical => _verticalZoomLevelIndex < GanttConstants.ZoomLevels.Count - 1;

    /// <summary>
    /// 縦方向にズームアウトできるかどうか
    /// </summary>
    public bool CanZoomOutVertical => _verticalZoomLevelIndex > 0;

    /// <summary>
    /// 基準セル幅を計算
    /// </summary>
    /// <param name="totalDays">全体の日数</param>
    /// <param name="availableWidth">利用可能な幅</param>
    /// <param name="force">強制的に再計算するかどうか</param>
    public void CalculateBaseColumnWidth(int totalDays, double availableWidth, bool force = false)
    {
        if (_isBaseColumnWidthCalculated && !force) return;

        if (totalDays <= 0 || availableWidth <= double.Epsilon)
        {
            _baseColumnWidth = GanttConstants.CellWidth.DefaultBase;
            return;
        }

        _baseColumnWidth = availableWidth / totalDays;
        _isBaseColumnWidthCalculated = true;
    }

    /// <summary>
    /// 基準行高さを計算
    /// </summary>
    /// <param name="taskCount">タスク数</param>
    /// <param name="availableHeight">利用可能な高さ</param>
    /// <param name="force">強制的に再計算するかどうか</param>
    public void CalculateBaseRowHeight(int taskCount, double availableHeight, bool force = false)
    {
        if (_isBaseRowHeightCalculated && !force) return;

        if (taskCount <= 0 || availableHeight <= double.Epsilon)
        {
            _baseRowHeight = GanttConstants.RowHeight.DefaultBase;
            return;
        }

        _baseRowHeight = availableHeight / taskCount;
        _isBaseRowHeightCalculated = true;
    }

    /// <summary>
    /// 実際のセル幅を取得
    /// </summary>
    public double GetActualCellWidth()
    {
        var width = _baseColumnWidth * _horizontalZoomScale;
        return Math.Max(GanttConstants.CellWidth.Min, Math.Min(GanttConstants.CellWidth.Max, width));
    }

    /// <summary>
    /// 実際の行高さを取得
    /// </summary>
    public double GetActualRowHeight()
    {
        var height = _baseRowHeight * _verticalZoomScale;
        return Math.Max(GanttConstants.RowHeight.Min, Math.Min(GanttConstants.RowHeight.Max, height));
    }

    /// <summary>
    /// バーの高さを取得
    /// </summary>
    public double GetBarHeight()
    {
        var rowHeight = GetActualRowHeight();
        var barHeight = rowHeight * GanttConstants.Bar.HeightRatio;
        return Math.Max(GanttConstants.Bar.MinHeight, Math.Min(GanttConstants.Bar.MaxHeight, barHeight));
    }

    /// <summary>
    /// バー間のギャップを取得
    /// </summary>
    public double GetBarGap()
    {
        var rowHeight = GetActualRowHeight();
        var gap = rowHeight * GanttConstants.Bar.GapRatio;
        return Math.Max(GanttConstants.Bar.MinGap, Math.Min(GanttConstants.Bar.MaxGap, gap));
    }

    /// <summary>
    /// 横方向にズームイン
    /// </summary>
    public void ZoomInHorizontal()
    {
        if (CanZoomInHorizontal)
        {
            _horizontalZoomLevelIndex++;
            _horizontalZoomScale = GanttConstants.ZoomLevels[_horizontalZoomLevelIndex];
        }
    }

    /// <summary>
    /// 横方向にズームアウト
    /// </summary>
    public void ZoomOutHorizontal()
    {
        if (CanZoomOutHorizontal)
        {
            _horizontalZoomLevelIndex--;
            _horizontalZoomScale = GanttConstants.ZoomLevels[_horizontalZoomLevelIndex];
        }
    }

    /// <summary>
    /// 縦方向にズームイン
    /// </summary>
    public void ZoomInVertical()
    {
        if (CanZoomInVertical)
        {
            _verticalZoomLevelIndex++;
            _verticalZoomScale = GanttConstants.ZoomLevels[_verticalZoomLevelIndex];
        }
    }

    /// <summary>
    /// 縦方向にズームアウト
    /// </summary>
    public void ZoomOutVertical()
    {
        if (CanZoomOutVertical)
        {
            _verticalZoomLevelIndex--;
            _verticalZoomScale = GanttConstants.ZoomLevels[_verticalZoomLevelIndex];
        }
    }

    /// <summary>
    /// 全体表示にリセット
    /// </summary>
    /// <param name="totalDays">全体の日数</param>
    /// <param name="taskCount">タスク数</param>
    /// <param name="availableWidth">利用可能な幅</param>
    /// <param name="availableHeight">利用可能な高さ</param>
    public void FitToScreen(int totalDays, int taskCount, double availableWidth, double availableHeight)
    {
        CalculateBaseColumnWidth(totalDays, availableWidth, force: true);
        CalculateBaseRowHeight(taskCount, availableHeight, force: true);

        _horizontalZoomLevelIndex = 0;
        _verticalZoomLevelIndex = 0;
        _horizontalZoomScale = GanttConstants.ZoomLevels[0];
        _verticalZoomScale = GanttConstants.ZoomLevels[0];
    }

    /// <summary>
    /// ズームレベルを復元
    /// </summary>
    /// <param name="horizontalZoomIndex">横方向ズームレベルインデックス</param>
    /// <param name="verticalZoomIndex">縦方向ズームレベルインデックス</param>
    public void RestoreZoomLevels(int horizontalZoomIndex, int verticalZoomIndex)
    {
        if (horizontalZoomIndex >= 0 && horizontalZoomIndex < GanttConstants.ZoomLevels.Count)
        {
            _horizontalZoomLevelIndex = horizontalZoomIndex;
            _horizontalZoomScale = GanttConstants.ZoomLevels[horizontalZoomIndex];
        }

        if (verticalZoomIndex >= 0 && verticalZoomIndex < GanttConstants.ZoomLevels.Count)
        {
            _verticalZoomLevelIndex = verticalZoomIndex;
            _verticalZoomScale = GanttConstants.ZoomLevels[verticalZoomIndex];
        }
    }

    /// <summary>
    /// 日付ラベルを表示すべきかどうかを判定
    /// </summary>
    /// <param name="dayIndex">日のインデックス</param>
    /// <param name="day">日(1-31)</param>
    public bool ShouldDisplayDateLabel(int dayIndex, int day)
    {
        var cellWidth = GetActualCellWidth();

        if (cellWidth >= GanttConstants.DateLabel.ShowAllThreshold)
        {
            return true;
        }
        else if (cellWidth >= GanttConstants.DateLabel.ShowEvery2DaysThreshold)
        {
            return dayIndex % 2 == 0;
        }
        else
        {
            return dayIndex % 3 == 0;
        }
    }
}

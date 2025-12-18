using RewindPM.Web.Components.Tasks;

namespace RewindPM.Web.Test.Components.Tasks;

public class GanttZoomManagerTests
{
    [Fact]
    public void CalculateBaseColumnWidth_WithValidInputs_SetsBaseColumnWidth()
    {
        // Arrange
        var manager = new GanttZoomManager();
        var totalDays = 30;
        var availableWidth = 1200.0;

        // Act
        manager.CalculateBaseColumnWidth(totalDays, availableWidth);

        // Assert
        Assert.Equal(40.0, manager.BaseColumnWidth); // 1200 / 30 = 40
    }

    [Fact]
    public void CalculateBaseColumnWidth_WithZeroDays_UsesDefaultBase()
    {
        // Arrange
        var manager = new GanttZoomManager();

        // Act
        manager.CalculateBaseColumnWidth(0, 1200.0);

        // Assert
        Assert.Equal(GanttConstants.CellWidth.DefaultBase, manager.BaseColumnWidth);
    }

    [Fact]
    public void CalculateBaseColumnWidth_WithZeroWidth_UsesDefaultBase()
    {
        // Arrange
        var manager = new GanttZoomManager();

        // Act
        manager.CalculateBaseColumnWidth(30, 0.0);

        // Assert
        Assert.Equal(GanttConstants.CellWidth.DefaultBase, manager.BaseColumnWidth);
    }

    [Fact]
    public void CalculateBaseRowHeight_WithValidInputs_SetsBaseRowHeight()
    {
        // Arrange
        var manager = new GanttZoomManager();
        var taskCount = 10;
        var availableHeight = 480.0;

        // Act
        manager.CalculateBaseRowHeight(taskCount, availableHeight);

        // Assert
        Assert.Equal(48.0, manager.BaseRowHeight); // 480 / 10 = 48
    }

    [Fact]
    public void CalculateBaseRowHeight_WithZeroTaskCount_UsesDefaultBase()
    {
        // Arrange
        var manager = new GanttZoomManager();

        // Act
        manager.CalculateBaseRowHeight(0, 480.0);

        // Assert
        Assert.Equal(GanttConstants.RowHeight.DefaultBase, manager.BaseRowHeight);
    }

    [Fact]
    public void CalculateBaseRowHeight_WithZeroHeight_UsesDefaultBase()
    {
        // Arrange
        var manager = new GanttZoomManager();

        // Act
        manager.CalculateBaseRowHeight(10, 0.0);

        // Assert
        Assert.Equal(GanttConstants.RowHeight.DefaultBase, manager.BaseRowHeight);
    }

    [Fact]
    public void GetActualCellWidth_WithDefaultZoom_ReturnsBaseWidth()
    {
        // Arrange
        var manager = new GanttZoomManager();
        manager.CalculateBaseColumnWidth(30, 1200.0);

        // Act
        var actualWidth = manager.GetActualCellWidth();

        // Assert
        Assert.Equal(40.0, actualWidth);
    }

    [Fact]
    public void GetActualCellWidth_ClampsToMinimum()
    {
        // Arrange
        var manager = new GanttZoomManager();
        manager.CalculateBaseColumnWidth(1000, 100.0); // Would result in 0.1

        // Act
        var actualWidth = manager.GetActualCellWidth();

        // Assert
        Assert.Equal(GanttConstants.CellWidth.Min, actualWidth);
    }

    [Fact]
    public void GetActualCellWidth_ClampsToMaximum()
    {
        // Arrange
        var manager = new GanttZoomManager();
        manager.CalculateBaseColumnWidth(1, 1000.0); // Would result in 1000

        // Act
        var actualWidth = manager.GetActualCellWidth();

        // Assert
        Assert.Equal(GanttConstants.CellWidth.Max, actualWidth);
    }

    [Fact]
    public void ZoomInHorizontal_IncreasesZoomLevel()
    {
        // Arrange
        var manager = new GanttZoomManager();
        var initialScale = manager.HorizontalZoomScale;

        // Act
        manager.ZoomInHorizontal();

        // Assert
        Assert.True(manager.HorizontalZoomScale > initialScale);
        Assert.Equal(1, manager.HorizontalZoomLevelIndex);
    }

    [Fact]
    public void ZoomInHorizontal_AtMaxLevel_DoesNotChange()
    {
        // Arrange
        var manager = new GanttZoomManager();
        
        // ズームレベルを最大まで上げる
        for (int i = 0; i < GanttConstants.ZoomLevels.Count; i++)
        {
            manager.ZoomInHorizontal();
        }

        var maxScale = manager.HorizontalZoomScale;
        var maxIndex = manager.HorizontalZoomLevelIndex;

        // Act
        manager.ZoomInHorizontal();

        // Assert
        Assert.Equal(maxScale, manager.HorizontalZoomScale);
        Assert.Equal(maxIndex, manager.HorizontalZoomLevelIndex);
    }

    [Fact]
    public void ZoomOutHorizontal_DecreasesZoomLevel()
    {
        // Arrange
        var manager = new GanttZoomManager();
        manager.ZoomInHorizontal();
        var currentScale = manager.HorizontalZoomScale;

        // Act
        manager.ZoomOutHorizontal();

        // Assert
        Assert.True(manager.HorizontalZoomScale < currentScale);
        Assert.Equal(0, manager.HorizontalZoomLevelIndex);
    }

    [Fact]
    public void ZoomOutHorizontal_AtMinLevel_DoesNotChange()
    {
        // Arrange
        var manager = new GanttZoomManager();
        var minScale = manager.HorizontalZoomScale;
        var minIndex = manager.HorizontalZoomLevelIndex;

        // Act
        manager.ZoomOutHorizontal();

        // Assert
        Assert.Equal(minScale, manager.HorizontalZoomScale);
        Assert.Equal(minIndex, manager.HorizontalZoomLevelIndex);
    }

    [Fact]
    public void ZoomInVertical_IncreasesZoomLevel()
    {
        // Arrange
        var manager = new GanttZoomManager();
        var initialScale = manager.VerticalZoomScale;

        // Act
        manager.ZoomInVertical();

        // Assert
        Assert.True(manager.VerticalZoomScale > initialScale);
        Assert.Equal(1, manager.VerticalZoomLevelIndex);
    }

    [Fact]
    public void ZoomOutVertical_DecreasesZoomLevel()
    {
        // Arrange
        var manager = new GanttZoomManager();
        manager.ZoomInVertical();
        var currentScale = manager.VerticalZoomScale;

        // Act
        manager.ZoomOutVertical();

        // Assert
        Assert.True(manager.VerticalZoomScale < currentScale);
        Assert.Equal(0, manager.VerticalZoomLevelIndex);
    }

    [Fact]
    public void FitToScreen_ResetsToBaseZoomLevel()
    {
        // Arrange
        var manager = new GanttZoomManager();
        manager.ZoomInHorizontal();
        manager.ZoomInVertical();

        // Act
        manager.FitToScreen(30, 10, 1200.0, 480.0);

        // Assert
        Assert.Equal(0, manager.HorizontalZoomLevelIndex);
        Assert.Equal(0, manager.VerticalZoomLevelIndex);
        Assert.Equal(GanttConstants.ZoomLevels[0], manager.HorizontalZoomScale);
        Assert.Equal(GanttConstants.ZoomLevels[0], manager.VerticalZoomScale);
    }

    [Fact]
    public void FitToScreen_RecalculatesBaseValues()
    {
        // Arrange
        var manager = new GanttZoomManager();
        manager.CalculateBaseColumnWidth(30, 1200.0);

        // Act
        manager.FitToScreen(60, 10, 2400.0, 480.0);

        // Assert
        Assert.Equal(40.0, manager.BaseColumnWidth); // 2400 / 60 = 40
        Assert.Equal(48.0, manager.BaseRowHeight); // 480 / 10 = 48
    }

    [Fact]
    public void RestoreZoomLevels_WithValidIndices_RestoresState()
    {
        // Arrange
        var manager = new GanttZoomManager();

        // Act
        manager.RestoreZoomLevels(2, 1);

        // Assert
        Assert.Equal(2, manager.HorizontalZoomLevelIndex);
        Assert.Equal(1, manager.VerticalZoomLevelIndex);
        Assert.Equal(GanttConstants.ZoomLevels[2], manager.HorizontalZoomScale);
        Assert.Equal(GanttConstants.ZoomLevels[1], manager.VerticalZoomScale);
    }

    [Fact]
    public void RestoreZoomLevels_WithInvalidIndices_IgnoresInvalidValues()
    {
        // Arrange
        var manager = new GanttZoomManager();
        var initialHScale = manager.HorizontalZoomScale;
        var initialVScale = manager.VerticalZoomScale;

        // Act
        manager.RestoreZoomLevels(-1, 100);

        // Assert
        Assert.Equal(initialHScale, manager.HorizontalZoomScale);
        Assert.Equal(initialVScale, manager.VerticalZoomScale);
    }

    [Theory]
    [InlineData(35.0, 0, 1, true)]  // セル幅35px、全日付表示閾値以上、dayIndex=0は表示
    [InlineData(35.0, 5, 6, true)]  // セル幅35px、全日付表示閾値以上、dayIndex=5も表示
    [InlineData(27.0, 1, 2, false)] // セル幅27px、2日おき、dayIndex=1は非表示
    [InlineData(27.0, 2, 3, true)]  // セル幅27px、2日おき、dayIndex=2は表示
    [InlineData(20.0, 1, 2, false)] // セル幅20px、2日おき、dayIndex=1は非表示
    [InlineData(20.0, 2, 3, true)]  // セル幅20px、2日おき、dayIndex=2は表示
    [InlineData(18.0, 2, 3, true)]  // セル幅18.0px、境界値(2日おき閾値)、dayIndex=2は表示
    [InlineData(17.9, 3, 4, true)]  // セル幅17.9px、境界値(3日おき閾値)、dayIndex=3は表示
    [InlineData(17.8, 3, 4, false)] // セル幅17.8px、3日おき閾値未満、dayIndex=3は非表示(5日おきになる)
    [InlineData(17.8, 5, 6, true)]  // セル幅17.8px、3日おき閾値未満、dayIndex=5は表示(5日おき)
    [InlineData(15.0, 2, 3, false)] // セル幅15px、3日おき閾値未満、dayIndex=2は非表示
    [InlineData(15.0, 5, 6, true)]  // セル幅15px、3日おき閾値未満、dayIndex=5は表示(5日おき)
    [InlineData(9.0, 4, 5, false)]  // セル幅9px、3日おき閾値未満、dayIndex=4は非表示(5日おき)
    [InlineData(9.0, 5, 6, true)]   // セル幅9px、3日おき閾値未満、dayIndex=5は表示(5日おき)
    public void ShouldDisplayDateLabel_ReturnsCorrectValue(double cellWidth, int dayIndex, int day, bool expected)
    {
        // Arrange
        var manager = new GanttZoomManager();
        
        // セル幅を設定するために基準値を調整
        var totalDays = 100;
        var availableWidth = cellWidth * totalDays;
        manager.CalculateBaseColumnWidth(totalDays, availableWidth);

        // Act
        var result = manager.ShouldDisplayDateLabel(dayIndex, day);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetBarHeight_ReturnsCorrectValue()
    {
        // Arrange
        var manager = new GanttZoomManager();
        manager.CalculateBaseRowHeight(10, 480.0); // baseRowHeight = 48

        // Act
        var barHeight = manager.GetBarHeight();

        // Assert
        // 48 * 0.35 = 16.8、これは最大値16にクランプされる
        Assert.Equal(GanttConstants.Bar.MaxHeight, barHeight);
    }

    [Fact]
    public void GetBarGap_ReturnsCorrectValue()
    {
        // Arrange
        var manager = new GanttZoomManager();
        manager.CalculateBaseRowHeight(10, 480.0); // baseRowHeight = 48

        // Act
        var gap = manager.GetBarGap();

        // Assert
        // 48 * 0.02 = 0.96、これは最小値と最大値の間に収まる
        Assert.True(gap >= GanttConstants.Bar.MinGap);
        Assert.True(gap <= GanttConstants.Bar.MaxGap);
    }

    [Fact]
    public void CanZoomInHorizontal_ReturnsTrueWhenNotAtMax()
    {
        // Arrange
        var manager = new GanttZoomManager();

        // Act & Assert
        Assert.True(manager.CanZoomInHorizontal);
    }

    [Fact]
    public void CanZoomInHorizontal_ReturnsFalseWhenAtMax()
    {
        // Arrange
        var manager = new GanttZoomManager();
        
        // 最大まで上げる
        for (int i = 0; i < GanttConstants.ZoomLevels.Count; i++)
        {
            manager.ZoomInHorizontal();
        }

        // Act & Assert
        Assert.False(manager.CanZoomInHorizontal);
    }

    [Fact]
    public void CanZoomOutHorizontal_ReturnsFalseAtMin()
    {
        // Arrange
        var manager = new GanttZoomManager();

        // Act & Assert
        Assert.False(manager.CanZoomOutHorizontal);
    }

    [Fact]
    public void CanZoomOutHorizontal_ReturnsTrueWhenNotAtMin()
    {
        // Arrange
        var manager = new GanttZoomManager();
        manager.ZoomInHorizontal();

        // Act & Assert
        Assert.True(manager.CanZoomOutHorizontal);
    }
}

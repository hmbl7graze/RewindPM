using RewindPM.Application.Read.DTOs;
using RewindPM.Domain.ValueObjects;
using RewindPM.Web.Components.Tasks;
using TaskStatus = RewindPM.Domain.ValueObjects.TaskStatus;

namespace RewindPM.Web.Test.Components.Tasks;

public class GanttTimelineCalculatorTests
{
    private static TaskDto CreateTaskDto(
        Guid? id = null,
        string title = "Task",
        DateTimeOffset? scheduledStartDate = null,
        DateTimeOffset? scheduledEndDate = null,
        DateTimeOffset? actualStartDate = null,
        DateTimeOffset? actualEndDate = null)
    {
        return new TaskDto
        {
            Id = id ?? Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            Title = title,
            Description = "",
            Status = TaskStatus.InProgress,
            ScheduledStartDate = scheduledStartDate,
            ScheduledEndDate = scheduledEndDate,
            ActualStartDate = actualStartDate,
            ActualEndDate = actualEndDate,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = null,
            CreatedBy = "test"
        };
    }

    [Fact]
    public void CalculateTimeline_WithEmptyTasks_ReturnsZeroValues()
    {
        // Arrange
        var calculator = new GanttTimelineCalculator();
        var tasks = new List<TaskDto>();

        // Act
        var changed = calculator.CalculateTimeline(tasks);

        // Assert
        Assert.True(changed);
        Assert.Null(calculator.TimelineStart);
        Assert.Null(calculator.TimelineEnd);
        Assert.Equal(0, calculator.TotalDays);
    }

    [Fact]
    public void CalculateTimeline_WithNullTasks_ReturnsZeroValues()
    {
        // Arrange
        var calculator = new GanttTimelineCalculator();

        // Act
        var changed = calculator.CalculateTimeline(null!);

        // Assert
        Assert.True(changed);
        Assert.Null(calculator.TimelineStart);
        Assert.Null(calculator.TimelineEnd);
        Assert.Equal(0, calculator.TotalDays);
    }

    [Fact]
    public void CalculateTimeline_WithTasksWithoutDates_ReturnsZeroValues()
    {
        // Arrange
        var calculator = new GanttTimelineCalculator();
        var tasks = new List<TaskDto>
        {
            CreateTaskDto(title: "Task 1")
        };

        // Act
        var changed = calculator.CalculateTimeline(tasks);

        // Assert
        Assert.True(changed);
        Assert.Null(calculator.TimelineStart);
        Assert.Null(calculator.TimelineEnd);
        Assert.Equal(0, calculator.TotalDays);
    }

    [Fact]
    public void CalculateTimeline_WithValidTasks_CalculatesCorrectRange()
    {
        // Arrange
        var calculator = new GanttTimelineCalculator();
        var startDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var endDate = new DateTimeOffset(2024, 1, 31, 0, 0, 0, TimeSpan.Zero);
        
        var tasks = new List<TaskDto>
        {
            CreateTaskDto(
                title: "Task 1",
                scheduledStartDate: startDate,
                scheduledEndDate: endDate)
        };

        // Act
        var changed = calculator.CalculateTimeline(tasks);

        // Assert
        Assert.True(changed);
        Assert.Equal(new DateTime(2024, 1, 1), calculator.TimelineStart);
        Assert.Equal(new DateTime(2024, 1, 31), calculator.TimelineEnd);
        Assert.Equal(31, calculator.TotalDays);
    }

    [Fact]
    public void CalculateTimeline_WithMultipleTasks_UsesEarliestAndLatestDates()
    {
        // Arrange
        var calculator = new GanttTimelineCalculator();
        var tasks = new List<TaskDto>
        {
            CreateTaskDto(
                title: "Task 1",
                scheduledStartDate: new DateTimeOffset(2024, 1, 10, 0, 0, 0, TimeSpan.Zero),
                scheduledEndDate: new DateTimeOffset(2024, 1, 20, 0, 0, 0, TimeSpan.Zero)),
            CreateTaskDto(
                title: "Task 2",
                scheduledStartDate: new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
                scheduledEndDate: new DateTimeOffset(2024, 1, 31, 0, 0, 0, TimeSpan.Zero))
        };

        // Act
        var changed = calculator.CalculateTimeline(tasks);

        // Assert
        Assert.True(changed);
        Assert.Equal(new DateTime(2024, 1, 1), calculator.TimelineStart);
        Assert.Equal(new DateTime(2024, 1, 31), calculator.TimelineEnd);
        Assert.Equal(31, calculator.TotalDays);
    }

    [Fact]
    public void CalculateTimeline_WithActualDates_IncludesActualDatesInRange()
    {
        // Arrange
        var calculator = new GanttTimelineCalculator();
        var tasks = new List<TaskDto>
        {
            CreateTaskDto(
                title: "Task 1",
                scheduledStartDate: new DateTimeOffset(2024, 1, 10, 0, 0, 0, TimeSpan.Zero),
                scheduledEndDate: new DateTimeOffset(2024, 1, 20, 0, 0, 0, TimeSpan.Zero),
                actualStartDate: new DateTimeOffset(2024, 1, 5, 0, 0, 0, TimeSpan.Zero),
                actualEndDate: new DateTimeOffset(2024, 1, 25, 0, 0, 0, TimeSpan.Zero))
        };

        // Act
        var changed = calculator.CalculateTimeline(tasks);

        // Assert
        Assert.True(changed);
        Assert.Equal(new DateTime(2024, 1, 5), calculator.TimelineStart);
        Assert.Equal(new DateTime(2024, 1, 25), calculator.TimelineEnd);
        Assert.Equal(21, calculator.TotalDays);
    }

    [Fact]
    public void CalculateTimeline_ReturnsFalseWhenTimelineNotChanged()
    {
        // Arrange
        var calculator = new GanttTimelineCalculator();
        var tasks = new List<TaskDto>
        {
            CreateTaskDto(
                title: "Task 1",
                scheduledStartDate: new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
                scheduledEndDate: new DateTimeOffset(2024, 1, 31, 0, 0, 0, TimeSpan.Zero))
        };

        calculator.CalculateTimeline(tasks);

        // Act
        var changed = calculator.CalculateTimeline(tasks);

        // Assert
        Assert.False(changed);
    }

    [Fact]
    public void GetGridColumn_WithNullTimelineStart_ReturnsDefaultValue()
    {
        // Arrange
        var calculator = new GanttTimelineCalculator();
        var startDate = new DateTimeOffset(2024, 1, 10, 0, 0, 0, TimeSpan.Zero);
        var endDate = new DateTimeOffset(2024, 1, 20, 0, 0, 0, TimeSpan.Zero);

        // Act
        var result = calculator.GetGridColumn(startDate, endDate);

        // Assert
        Assert.Equal("1 / 2", result);
    }

    [Fact]
    public void GetGridColumn_WithValidDates_ReturnsCorrectColumn()
    {
        // Arrange
        var calculator = new GanttTimelineCalculator();
        var tasks = new List<TaskDto>
        {
            CreateTaskDto(
                title: "Task 1",
                scheduledStartDate: new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
                scheduledEndDate: new DateTimeOffset(2024, 1, 31, 0, 0, 0, TimeSpan.Zero))
        };
        calculator.CalculateTimeline(tasks);

        var barStartDate = new DateTimeOffset(2024, 1, 10, 0, 0, 0, TimeSpan.Zero);
        var barEndDate = new DateTimeOffset(2024, 1, 20, 0, 0, 0, TimeSpan.Zero);

        // Act
        var result = calculator.GetGridColumn(barStartDate, barEndDate);

        // Assert
        Assert.Equal("10 / 21", result); // 10日目から21日目まで
    }

    [Fact]
    public void GetGridColumn_ClampsToTimelineRange()
    {
        // Arrange
        var calculator = new GanttTimelineCalculator();
        var tasks = new List<TaskDto>
        {
            CreateTaskDto(
                title: "Task 1",
                scheduledStartDate: new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
                scheduledEndDate: new DateTimeOffset(2024, 1, 31, 0, 0, 0, TimeSpan.Zero))
        };
        calculator.CalculateTimeline(tasks);

        // タイムライン範囲外の日付
        var barStartDate = new DateTimeOffset(2023, 12, 25, 0, 0, 0, TimeSpan.Zero);
        var barEndDate = new DateTimeOffset(2024, 2, 5, 0, 0, 0, TimeSpan.Zero);

        // Act
        var result = calculator.GetGridColumn(barStartDate, barEndDate);

        // Assert
        Assert.Equal("1 / 32", result); // 範囲内にクランプ
    }

    [Fact]
    public void GetMonthGroups_WithEmptyTimeline_ReturnsEmptyList()
    {
        // Arrange
        var calculator = new GanttTimelineCalculator();

        // Act
        var groups = calculator.GetMonthGroups();

        // Assert
        Assert.Empty(groups);
    }

    [Fact]
    public void GetMonthGroups_WithSingleMonth_ReturnsSingleGroup()
    {
        // Arrange
        var calculator = new GanttTimelineCalculator();
        var tasks = new List<TaskDto>
        {
            CreateTaskDto(
                title: "Task 1",
                scheduledStartDate: new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
                scheduledEndDate: new DateTimeOffset(2024, 1, 31, 0, 0, 0, TimeSpan.Zero))
        };
        calculator.CalculateTimeline(tasks);

        // Act
        var groups = calculator.GetMonthGroups();

        // Assert
        Assert.Single(groups);
        Assert.Equal(2024, groups[0].Year);
        Assert.Equal(1, groups[0].Month);
        Assert.Equal(1, groups[0].StartColumn);
        Assert.Equal(32, groups[0].EndColumn);
        Assert.Equal(31, groups[0].DayCount);
    }

    [Fact]
    public void GetMonthGroups_WithMultipleMonths_ReturnsCorrectGroups()
    {
        // Arrange
        var calculator = new GanttTimelineCalculator();
        var tasks = new List<TaskDto>
        {
            CreateTaskDto(
                title: "Task 1",
                scheduledStartDate: new DateTimeOffset(2024, 1, 15, 0, 0, 0, TimeSpan.Zero),
                scheduledEndDate: new DateTimeOffset(2024, 3, 15, 0, 0, 0, TimeSpan.Zero))
        };
        calculator.CalculateTimeline(tasks);

        // Act
        var groups = calculator.GetMonthGroups();

        // Assert
        Assert.Equal(3, groups.Count);
        
        // 1月グループ (1/15-1/31 = 17日間)
        Assert.Equal(2024, groups[0].Year);
        Assert.Equal(1, groups[0].Month);
        Assert.Equal(17, groups[0].DayCount);
        
        // 2月グループ (2/1-2/29 = 29日間、2024年はうるう年)
        Assert.Equal(2024, groups[1].Year);
        Assert.Equal(2, groups[1].Month);
        Assert.Equal(29, groups[1].DayCount);
        
        // 3月グループ (3/1-3/15 = 15日間)
        Assert.Equal(2024, groups[2].Year);
        Assert.Equal(3, groups[2].Month);
        Assert.Equal(15, groups[2].DayCount);
    }

    [Fact]
    public void GetMonthGroups_ColumnsAreSequential()
    {
        // Arrange
        var calculator = new GanttTimelineCalculator();
        var tasks = new List<TaskDto>
        {
            CreateTaskDto(
                title: "Task 1",
                scheduledStartDate: new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
                scheduledEndDate: new DateTimeOffset(2024, 2, 29, 0, 0, 0, TimeSpan.Zero))
        };
        calculator.CalculateTimeline(tasks);

        // Act
        var groups = calculator.GetMonthGroups();

        // Assert
        Assert.Equal(2, groups.Count);
        
        // 1月: カラム1-32
        Assert.Equal(1, groups[0].StartColumn);
        Assert.Equal(32, groups[0].EndColumn);
        
        // 2月: カラム32-61 (1月の終了カラムから開始)
        Assert.Equal(32, groups[1].StartColumn);
        Assert.Equal(61, groups[1].EndColumn);
    }
}

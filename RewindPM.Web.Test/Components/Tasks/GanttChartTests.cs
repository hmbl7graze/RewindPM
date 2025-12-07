using Bunit;
using Microsoft.AspNetCore.Components;
using RewindPM.Application.Read.DTOs;
using TaskStatus = RewindPM.Domain.ValueObjects.TaskStatus;
using RewindPM.Web.Components.Tasks;

namespace RewindPM.Web.Test.Components.Tasks;

public class GanttChartTests : Bunit.TestContext
{
    [Fact(DisplayName = "タスクがない場合、空のメッセージが表示される")]
    public void GanttChart_DisplaysEmptyMessage_WhenNoTasks()
    {
        // Arrange
        var tasks = new List<TaskDto>();

        // Act
        var cut = RenderComponent<GanttChart>(parameters => parameters
            .Add(p => p.Tasks, tasks));

        // Assert
        var emptyMessage = cut.Find(".gantt-empty");
        Assert.Contains("タスクがありません", emptyMessage.TextContent);
    }

    [Fact(DisplayName = "予定期間のないタスクのみの場合、空のメッセージが表示される")]
    public void GanttChart_DisplaysEmptyMessage_WhenNoScheduledDates()
    {
        // Arrange
        var tasks = new List<TaskDto>
        {
            new TaskDto
            {
                Id = Guid.NewGuid(),
                ProjectId = Guid.NewGuid(),
                Title = "Task without dates",
                Description = "Test",
                Status = TaskStatus.Todo,
                ScheduledStartDate = null,
                ScheduledEndDate = null,
                EstimatedHours = null,
                ActualStartDate = null,
                ActualEndDate = null,
                ActualHours = null,
                CreatedAt = DateTime.Now,
                UpdatedAt = null,
                CreatedBy = "test-user",
                UpdatedBy = null
            }
        };

        // Act
        var cut = RenderComponent<GanttChart>(parameters => parameters
            .Add(p => p.Tasks, tasks));

        // Assert
        var emptyMessage = cut.Find(".gantt-empty");
        Assert.Contains("予定期間が設定されたタスクがありません", emptyMessage.TextContent);
    }

    [Fact(DisplayName = "タスクが存在する場合、ガントチャート行が表示される")]
    public void GanttChart_DisplaysGanttRows_WhenTasksExist()
    {
        // Arrange
        var tasks = CreateTestTasks();

        // Act
        var cut = RenderComponent<GanttChart>(parameters => parameters
            .Add(p => p.Tasks, tasks));

        // Assert
        var ganttRows = cut.FindAll(".gantt-row");
        Assert.Equal(2, ganttRows.Count);
    }

    [Fact(DisplayName = "タスク名が正しく表示される")]
    public void GanttChart_DisplaysTaskNames_Correctly()
    {
        // Arrange
        var tasks = CreateTestTasks();

        // Act
        var cut = RenderComponent<GanttChart>(parameters => parameters
            .Add(p => p.Tasks, tasks));

        // Assert
        var taskTitles = cut.FindAll(".gantt-task-title");
        Assert.Equal(2, taskTitles.Count);
        Assert.Contains("Task 1", taskTitles[0].TextContent);
        Assert.Contains("Task 2", taskTitles[1].TextContent);
    }

    [Fact(DisplayName = "ステータスバッジが正しく表示される")]
    public void GanttChart_DisplaysStatusBadges_Correctly()
    {
        // Arrange
        var tasks = CreateTestTasks();

        // Act
        var cut = RenderComponent<GanttChart>(parameters => parameters
            .Add(p => p.Tasks, tasks));

        // Assert
        var statusBadges = cut.FindAll(".gantt-task-status");
        Assert.Equal(2, statusBadges.Count);
        Assert.Contains(statusBadges, b => b.ClassList.Contains("gantt-status-todo"));
        Assert.Contains(statusBadges, b => b.ClassList.Contains("gantt-status-inprogress"));
    }

    [Fact(DisplayName = "予定期間バーが表示される")]
    public void GanttChart_DisplaysScheduledBars_WhenScheduledDatesExist()
    {
        // Arrange
        var tasks = CreateTestTasks();

        // Act
        var cut = RenderComponent<GanttChart>(parameters => parameters
            .Add(p => p.Tasks, tasks));

        // Assert
        var scheduledBars = cut.FindAll(".gantt-bar-scheduled");
        Assert.Equal(2, scheduledBars.Count);
    }

    [Fact(DisplayName = "実績期間バーが表示される")]
    public void GanttChart_DisplaysActualBars_WhenActualDatesExist()
    {
        // Arrange
        var tasks = CreateTestTasks();

        // Act
        var cut = RenderComponent<GanttChart>(parameters => parameters
            .Add(p => p.Tasks, tasks));

        // Assert
        var actualBars = cut.FindAll(".gantt-bar-actual");
        Assert.Single(actualBars); // Only Task 2 has actual dates
    }

    [Fact(DisplayName = "日付ヘッダーが表示される")]
    public void GanttChart_DisplaysDateHeaders_Correctly()
    {
        // Arrange
        var tasks = CreateTestTasks();

        // Act
        var cut = RenderComponent<GanttChart>(parameters => parameters
            .Add(p => p.Tasks, tasks));

        // Assert
        var dateHeaders = cut.FindAll(".gantt-date-cell");
        Assert.True(dateHeaders.Count > 0); // Should have date headers for the timeline range
    }

    [Fact(DisplayName = "タスククリック時にコールバックが呼ばれる")]
    public async Task GanttChart_InvokesCallback_WhenTaskClicked()
    {
        // Arrange
        var tasks = CreateTestTasks();
        TaskDto? clickedTask = null;
        var onTaskClick = EventCallback.Factory.Create<TaskDto>(this, (task) => clickedTask = task);

        var cut = RenderComponent<GanttChart>(parameters => parameters
            .Add(p => p.Tasks, tasks)
            .Add(p => p.OnTaskClick, onTaskClick));

        // Act
        var ganttRow = cut.Find(".gantt-row");
        await cut.InvokeAsync(() => ganttRow.Click());

        // Assert
        Assert.NotNull(clickedTask);
        Assert.Equal("Task 1", clickedTask.Title);
    }

    [Fact(DisplayName = "タイムライン範囲が正しく計算される")]
    public void GanttChart_CalculatesTimelineRange_Correctly()
    {
        // Arrange
        var startDate = new DateTime(2024, 1, 1);
        var endDate = new DateTime(2024, 1, 10);
        var tasks = new List<TaskDto>
        {
            new TaskDto
            {
                Id = Guid.NewGuid(),
                ProjectId = Guid.NewGuid(),
                Title = "Task",
                Description = "Test",
                Status = TaskStatus.Todo,
                ScheduledStartDate = startDate,
                ScheduledEndDate = endDate,
                EstimatedHours = 40,
                ActualStartDate = null,
                ActualEndDate = null,
                ActualHours = null,
                CreatedAt = DateTime.Now,
                UpdatedAt = null,
                CreatedBy = "test-user",
                UpdatedBy = null
            }
        };

        // Act
        var cut = RenderComponent<GanttChart>(parameters => parameters
            .Add(p => p.Tasks, tasks));

        // Assert
        var dateHeaders = cut.FindAll(".gantt-date-cell");
        Assert.Equal(10, dateHeaders.Count); // 10 days from Jan 1 to Jan 10
    }

    [Fact(DisplayName = "実績期間が予定期間より広い場合、タイムラインが実績に合わせて拡張される")]
    public void GanttChart_ExtendsTimeline_WhenActualExceedsScheduled()
    {
        // Arrange
        var tasks = new List<TaskDto>
        {
            new TaskDto
            {
                Id = Guid.NewGuid(),
                ProjectId = Guid.NewGuid(),
                Title = "Task",
                Description = "Test",
                Status = TaskStatus.Done,
                ScheduledStartDate = new DateTime(2024, 1, 5),
                ScheduledEndDate = new DateTime(2024, 1, 10),
                EstimatedHours = 40,
                ActualStartDate = new DateTime(2024, 1, 1), // Earlier than scheduled
                ActualEndDate = new DateTime(2024, 1, 15), // Later than scheduled
                ActualHours = 60,
                CreatedAt = DateTime.Now,
                UpdatedAt = null,
                CreatedBy = "test-user",
                UpdatedBy = null
            }
        };

        // Act
        var cut = RenderComponent<GanttChart>(parameters => parameters
            .Add(p => p.Tasks, tasks));

        // Assert
        var dateHeaders = cut.FindAll(".gantt-date-cell");
        Assert.Equal(15, dateHeaders.Count); // 15 days from Jan 1 to Jan 15
    }

    [Fact(DisplayName = "月のヘッダーが表示される")]
    public void GanttChart_DisplaysMonthHeaders_Correctly()
    {
        // Arrange
        var tasks = CreateTestTasks();

        // Act
        var cut = RenderComponent<GanttChart>(parameters => parameters
            .Add(p => p.Tasks, tasks));

        // Assert
        var monthCells = cut.FindAll(".gantt-month-cell");
        Assert.True(monthCells.Count > 0); // Should have at least one month header
    }

    [Fact(DisplayName = "単一月の場合、月が1つだけ表示される")]
    public void GanttChart_DisplaysSingleMonth_WhenTasksInSameMonth()
    {
        // Arrange
        var tasks = CreateTestTasks(); // All tasks are in January 2024

        // Act
        var cut = RenderComponent<GanttChart>(parameters => parameters
            .Add(p => p.Tasks, tasks));

        // Assert
        var monthCells = cut.FindAll(".gantt-month-cell");
        Assert.Single(monthCells);
        Assert.Contains("2024/1", monthCells[0].TextContent);
    }

    [Fact(DisplayName = "複数月にまたがる場合、各月が正しく表示される")]
    public void GanttChart_DisplaysMultipleMonths_WhenTasksSpanMonths()
    {
        // Arrange
        var tasks = new List<TaskDto>
        {
            new TaskDto
            {
                Id = Guid.NewGuid(),
                ProjectId = Guid.NewGuid(),
                Title = "Task spanning months",
                Description = "Test",
                Status = TaskStatus.InProgress,
                ScheduledStartDate = new DateTime(2024, 1, 15),
                ScheduledEndDate = new DateTime(2024, 3, 15),
                EstimatedHours = 100,
                ActualStartDate = null,
                ActualEndDate = null,
                ActualHours = null,
                CreatedAt = DateTime.Now,
                UpdatedAt = null,
                CreatedBy = "test-user",
                UpdatedBy = null
            }
        };

        // Act
        var cut = RenderComponent<GanttChart>(parameters => parameters
            .Add(p => p.Tasks, tasks));

        // Assert
        var monthCells = cut.FindAll(".gantt-month-cell");
        Assert.Equal(3, monthCells.Count); // January, February, March
        Assert.Contains("2024/1", monthCells[0].TextContent);
        Assert.Contains("2024/2", monthCells[1].TextContent);
        Assert.Contains("2024/3", monthCells[2].TextContent);
    }

    [Fact(DisplayName = "日付セルには日のみが表示される")]
    public void GanttChart_DisplaysOnlyDayNumbers_InDateCells()
    {
        // Arrange
        var tasks = new List<TaskDto>
        {
            new TaskDto
            {
                Id = Guid.NewGuid(),
                ProjectId = Guid.NewGuid(),
                Title = "Task",
                Description = "Test",
                Status = TaskStatus.Todo,
                ScheduledStartDate = new DateTime(2024, 1, 1),
                ScheduledEndDate = new DateTime(2024, 1, 3),
                EstimatedHours = 10,
                ActualStartDate = null,
                ActualEndDate = null,
                ActualHours = null,
                CreatedAt = DateTime.Now,
                UpdatedAt = null,
                CreatedBy = "test-user",
                UpdatedBy = null
            }
        };

        // Act
        var cut = RenderComponent<GanttChart>(parameters => parameters
            .Add(p => p.Tasks, tasks));

        // Assert
        var dateCells = cut.FindAll(".gantt-date-cell");
        Assert.Equal(3, dateCells.Count);

        // 日付セルには日のみが表示される（月や年は含まれない）
        Assert.Equal("1", dateCells[0].TextContent.Trim());
        Assert.Equal("2", dateCells[1].TextContent.Trim());
        Assert.Equal("3", dateCells[2].TextContent.Trim());
    }

    private List<TaskDto> CreateTestTasks()
    {
        return new List<TaskDto>
        {
            new TaskDto
            {
                Id = Guid.NewGuid(),
                ProjectId = Guid.NewGuid(),
                Title = "Task 1",
                Description = "Description 1",
                Status = TaskStatus.Todo,
                ScheduledStartDate = new DateTime(2024, 1, 1),
                ScheduledEndDate = new DateTime(2024, 1, 5),
                EstimatedHours = 20,
                ActualStartDate = null,
                ActualEndDate = null,
                ActualHours = null,
                CreatedAt = DateTime.Now,
                UpdatedAt = null,
                CreatedBy = "test-user",
                UpdatedBy = null
            },
            new TaskDto
            {
                Id = Guid.NewGuid(),
                ProjectId = Guid.NewGuid(),
                Title = "Task 2",
                Description = "Description 2",
                Status = TaskStatus.InProgress,
                ScheduledStartDate = new DateTime(2024, 1, 3),
                ScheduledEndDate = new DateTime(2024, 1, 8),
                EstimatedHours = 30,
                ActualStartDate = new DateTime(2024, 1, 3),
                ActualEndDate = new DateTime(2024, 1, 7),
                ActualHours = 28,
                CreatedAt = DateTime.Now,
                UpdatedAt = null,
                CreatedBy = "test-user",
                UpdatedBy = null
            }
        };
    }
}

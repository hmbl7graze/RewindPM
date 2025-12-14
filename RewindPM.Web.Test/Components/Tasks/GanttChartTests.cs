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
                CreatedAt = DateTimeOffset.Now,
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
        var ganttRows = cut.FindAll(".gantt-task-name-cell");
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
        var taskNameCell = cut.Find(".gantt-task-name-cell");
        await cut.InvokeAsync(() => taskNameCell.Click());

        // Assert
        Assert.NotNull(clickedTask);
        Assert.Equal("Task 1", clickedTask.Title);
    }

    [Fact(DisplayName = "タイムライン範囲が正しく計算される")]
    public void GanttChart_CalculatesTimelineRange_Correctly()
    {
        // Arrange
        var startDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var endDate = new DateTimeOffset(2024, 1, 10, 0, 0, 0, TimeSpan.Zero);
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
                CreatedAt = DateTimeOffset.Now,
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
                ScheduledStartDate = new DateTimeOffset(2024, 1, 5, 0, 0, 0, TimeSpan.Zero),
                ScheduledEndDate = new DateTimeOffset(2024, 1, 10, 0, 0, 0, TimeSpan.Zero),
                EstimatedHours = 40,
                ActualStartDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero), // Earlier than scheduled
                ActualEndDate = new DateTimeOffset(2024, 1, 15, 0, 0, 0, TimeSpan.Zero), // Later than scheduled
                ActualHours = 60,
                CreatedAt = DateTimeOffset.Now,
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
                ScheduledStartDate = new DateTimeOffset(2024, 1, 15, 0, 0, 0, TimeSpan.Zero),
                ScheduledEndDate = new DateTimeOffset(2024, 3, 15, 0, 0, 0, TimeSpan.Zero),
                EstimatedHours = 100,
                ActualStartDate = null,
                ActualEndDate = null,
                ActualHours = null,
                CreatedAt = DateTimeOffset.Now,
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
                ScheduledStartDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
                ScheduledEndDate = new DateTimeOffset(2024, 1, 3, 0, 0, 0, TimeSpan.Zero),
                EstimatedHours = 10,
                ActualStartDate = null,
                ActualEndDate = null,
                ActualHours = null,
                CreatedAt = DateTimeOffset.Now,
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

    [Fact(DisplayName = "予定期間バーにリサイズハンドルが表示される")]
    public void GanttChart_DisplaysResizeHandles_OnScheduledBars()
    {
        // Arrange
        var tasks = CreateTestTasks();

        // Act
        var cut = RenderComponent<GanttChart>(parameters => parameters
            .Add(p => p.Tasks, tasks));

        // Assert
        var scheduledBars = cut.FindAll(".gantt-bar-scheduled");
        Assert.Equal(2, scheduledBars.Count);

        // 各予定期間バーに左右のリサイズハンドルがあることを確認
        foreach (var bar in scheduledBars)
        {
            var leftHandle = bar.QuerySelector(".gantt-resize-handle-left");
            var rightHandle = bar.QuerySelector(".gantt-resize-handle-right");
            Assert.NotNull(leftHandle);
            Assert.NotNull(rightHandle);
        }
    }

    [Fact(DisplayName = "実績期間バーにリサイズハンドルが表示される")]
    public void GanttChart_DisplaysResizeHandles_OnActualBars()
    {
        // Arrange
        var tasks = CreateTestTasks();

        // Act
        var cut = RenderComponent<GanttChart>(parameters => parameters
            .Add(p => p.Tasks, tasks));

        // Assert
        var actualBars = cut.FindAll(".gantt-bar-actual");
        Assert.Single(actualBars); // Task 2 only

        // 実績期間バーに左右のリサイズハンドルがあることを確認
        var bar = actualBars[0];
        var leftHandle = bar.QuerySelector(".gantt-resize-handle-left");
        var rightHandle = bar.QuerySelector(".gantt-resize-handle-right");
        Assert.NotNull(leftHandle);
        Assert.NotNull(rightHandle);
    }

    [Fact(DisplayName = "バーにタスクIDとバー種別のデータ属性が設定される")]
    public void GanttChart_SetsDataAttributes_OnBars()
    {
        // Arrange
        var tasks = CreateTestTasks();
        var taskId = tasks[0].Id;

        // Act
        var cut = RenderComponent<GanttChart>(parameters => parameters
            .Add(p => p.Tasks, tasks));

        // Assert
        var scheduledBar = cut.Find(".gantt-bar-scheduled");
        Assert.Equal(taskId.ToString(), scheduledBar.GetAttribute("data-task-id"));
        Assert.Equal("scheduled", scheduledBar.GetAttribute("data-bar-type"));
    }

    [Fact(DisplayName = "OnBarResizeコールバックが呼ばれる")]
    public async Task GanttChart_InvokesOnBarResize_WhenBarResized()
    {
        // Arrange
        var tasks = CreateTestTasks();
        var taskId = tasks[0].Id;

        (Guid taskId, string barType, DateTimeOffset newStartDate, DateTimeOffset newEndDate)? resizeData = null;
        var onBarResize = EventCallback.Factory.Create<(Guid, string, DateTimeOffset, DateTimeOffset)>(
            this,
            (data) => resizeData = data
        );

        var cut = RenderComponent<GanttChart>(parameters => parameters
            .Add(p => p.Tasks, tasks)
            .Add(p => p.OnBarResize, onBarResize));

        // Act - OnBarResizedメソッドを直接呼び出してテスト
        await cut.Instance.OnBarResized(taskId.ToString(), "scheduled", 0, 2);

        // Assert
        Assert.NotNull(resizeData);
        Assert.Equal(taskId, resizeData.Value.taskId);
        Assert.Equal("scheduled", resizeData.Value.barType);
        Assert.Equal(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero), resizeData.Value.newStartDate);
        Assert.Equal(new DateTimeOffset(2024, 1, 3, 0, 0, 0, TimeSpan.Zero), resizeData.Value.newEndDate);
    }

    [Fact(DisplayName = "OnBarResizedで無効なGUIDの場合は処理されない")]
    public async Task GanttChart_IgnoresInvalidTaskId_InOnBarResized()
    {
        // Arrange
        var tasks = CreateTestTasks();
        var callbackInvoked = false;
        var onBarResize = EventCallback.Factory.Create<(Guid, string, DateTimeOffset, DateTimeOffset)>(
            this,
            (data) => callbackInvoked = true
        );

        var cut = RenderComponent<GanttChart>(parameters => parameters
            .Add(p => p.Tasks, tasks)
            .Add(p => p.OnBarResize, onBarResize));

        // Act - 無効なGUIDで呼び出し
        await cut.Instance.OnBarResized("invalid-guid", "scheduled", 0, 2);

        // Assert - コールバックは呼ばれない
        Assert.False(callbackInvoked);
    }

    [Fact(DisplayName = "OnBarResizedで日付が正しく計算される")]
    public async Task GanttChart_CalculatesDatesCorrectly_InOnBarResized()
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
                Status = TaskStatus.InProgress,
                ScheduledStartDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
                ScheduledEndDate = new DateTimeOffset(2024, 1, 10, 0, 0, 0, TimeSpan.Zero),
                EstimatedHours = 40,
                ActualStartDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
                ActualEndDate = new DateTimeOffset(2024, 1, 5, 0, 0, 0, TimeSpan.Zero),
                ActualHours = 20,
                CreatedAt = DateTimeOffset.Now,
                UpdatedAt = null,
                CreatedBy = "test-user",
                UpdatedBy = null
            }
        };

        (Guid taskId, string barType, DateTimeOffset newStartDate, DateTimeOffset newEndDate)? resizeData = null;
        var onBarResize = EventCallback.Factory.Create<(Guid, string, DateTimeOffset, DateTimeOffset)>(
            this,
            (data) => resizeData = data
        );

        var cut = RenderComponent<GanttChart>(parameters => parameters
            .Add(p => p.Tasks, tasks)
            .Add(p => p.OnBarResize, onBarResize));

        // Act - タイムライン開始日から5日目～8日目にリサイズ
        await cut.Instance.OnBarResized(tasks[0].Id.ToString(), "actual", 5, 8);

        // Assert
        Assert.NotNull(resizeData);
        Assert.Equal(new DateTimeOffset(2024, 1, 6, 0, 0, 0, TimeSpan.Zero), resizeData.Value.newStartDate); // 1/1 + 5日
        Assert.Equal(new DateTimeOffset(2024, 1, 9, 0, 0, 0, TimeSpan.Zero), resizeData.Value.newEndDate);   // 1/1 + 8日
    }

    // ========== リワインド機能（IsReadOnly）のテスト ==========

    [Fact(DisplayName = "IsReadOnlyがfalseの場合、readonly-modeクラスが付与されない")]
    public void GanttChart_DoesNotHaveReadonlyClass_WhenIsReadOnlyIsFalse()
    {
        // Arrange
        var tasks = CreateTestTasks();

        // Act
        var cut = RenderComponent<GanttChart>(parameters => parameters
            .Add(p => p.Tasks, tasks)
            .Add(p => p.IsReadOnly, false));

        // Assert
        var ganttChart = cut.Find(".gantt-chart");
        Assert.DoesNotContain("readonly-mode", ganttChart.ClassName);
    }

    [Fact(DisplayName = "IsReadOnlyがtrueの場合、readonly-modeクラスが付与される")]
    public void GanttChart_HasReadonlyClass_WhenIsReadOnlyIsTrue()
    {
        // Arrange
        var tasks = CreateTestTasks();

        // Act
        var cut = RenderComponent<GanttChart>(parameters => parameters
            .Add(p => p.Tasks, tasks)
            .Add(p => p.IsReadOnly, true));

        // Assert
        var ganttChart = cut.Find(".gantt-chart");
        Assert.Contains("readonly-mode", ganttChart.ClassName);
    }

    [Fact(DisplayName = "IsReadOnlyがfalseの場合、予定期間バーにリサイズハンドルが表示される")]
    public void GanttChart_DisplaysResizeHandles_WhenIsReadOnlyIsFalse()
    {
        // Arrange
        var tasks = CreateTestTasks();

        // Act
        var cut = RenderComponent<GanttChart>(parameters => parameters
            .Add(p => p.Tasks, tasks)
            .Add(p => p.IsReadOnly, false));

        // Assert
        var scheduledBars = cut.FindAll(".gantt-bar-scheduled");
        Assert.True(scheduledBars.Count > 0);

        // 各予定期間バーに左右のリサイズハンドルがあることを確認
        foreach (var bar in scheduledBars)
        {
            var leftHandle = bar.QuerySelector(".gantt-resize-handle-left");
            var rightHandle = bar.QuerySelector(".gantt-resize-handle-right");
            Assert.NotNull(leftHandle);
            Assert.NotNull(rightHandle);
        }
    }

    [Fact(DisplayName = "IsReadOnlyがtrueの場合、予定期間バーにリサイズハンドルが表示されない")]
    public void GanttChart_DoesNotDisplayResizeHandles_WhenIsReadOnlyIsTrue()
    {
        // Arrange
        var tasks = CreateTestTasks();

        // Act
        var cut = RenderComponent<GanttChart>(parameters => parameters
            .Add(p => p.Tasks, tasks)
            .Add(p => p.IsReadOnly, true));

        // Assert
        var scheduledBars = cut.FindAll(".gantt-bar-scheduled");
        Assert.True(scheduledBars.Count > 0);

        // 予定期間バーにリサイズハンドルがないことを確認
        foreach (var bar in scheduledBars)
        {
            var leftHandle = bar.QuerySelector(".gantt-resize-handle-left");
            var rightHandle = bar.QuerySelector(".gantt-resize-handle-right");
            Assert.Null(leftHandle);
            Assert.Null(rightHandle);
        }
    }

    [Fact(DisplayName = "IsReadOnlyがtrueの場合、実績期間バーにリサイズハンドルが表示されない")]
    public void GanttChart_DoesNotDisplayResizeHandlesOnActualBars_WhenIsReadOnlyIsTrue()
    {
        // Arrange
        var tasks = CreateTestTasks();

        // Act
        var cut = RenderComponent<GanttChart>(parameters => parameters
            .Add(p => p.Tasks, tasks)
            .Add(p => p.IsReadOnly, true));

        // Assert
        var actualBars = cut.FindAll(".gantt-bar-actual");
        Assert.Single(actualBars); // Task 2 only

        // 実績期間バーにリサイズハンドルがないことを確認
        var bar = actualBars[0];
        var leftHandle = bar.QuerySelector(".gantt-resize-handle-left");
        var rightHandle = bar.QuerySelector(".gantt-resize-handle-right");
        Assert.Null(leftHandle);
        Assert.Null(rightHandle);
    }

    [Fact(DisplayName = "ViewDateパラメータが設定できる")]
    public void GanttChart_AcceptsViewDateParameter()
    {
        // Arrange
        var tasks = CreateTestTasks();
        var viewDate = new DateTimeOffset(2024, 1, 5, 0, 0, 0, TimeSpan.Zero);

        // Act
        var cut = RenderComponent<GanttChart>(parameters => parameters
            .Add(p => p.Tasks, tasks)
            .Add(p => p.ViewDate, viewDate));

        // Assert
        Assert.Equal(viewDate, cut.Instance.ViewDate);
    }

    [Fact(DisplayName = "ViewDateがnullでも正常に動作する")]
    public void GanttChart_WorksCorrectly_WhenViewDateIsNull()
    {
        // Arrange
        var tasks = CreateTestTasks();

        // Act
        var cut = RenderComponent<GanttChart>(parameters => parameters
            .Add(p => p.Tasks, tasks)
            .Add(p => p.ViewDate, null));

        // Assert
        Assert.Null(cut.Instance.ViewDate);
        var ganttChart = cut.Find(".gantt-chart");
        Assert.NotNull(ganttChart);
    }

    [Fact(DisplayName = "デフォルトでIsReadOnlyはfalse")]
    public void GanttChart_IsReadOnlyDefaultsToFalse()
    {
        // Arrange
        var tasks = CreateTestTasks();

        // Act
        var cut = RenderComponent<GanttChart>(parameters => parameters
            .Add(p => p.Tasks, tasks));

        // Assert
        Assert.False(cut.Instance.IsReadOnly);
        var ganttChart = cut.Find(".gantt-chart");
        Assert.DoesNotContain("readonly-mode", ganttChart.ClassName);
    }

    // ========== アニメーション機能のテスト ==========

    [Fact(DisplayName = "初回ロード時はアニメーションクラスが付与されない")]
    public void GanttChart_DoesNotApplyAnimationClass_OnInitialLoad()
    {
        // Arrange
        var tasks = CreateTestTasks();

        // Act
        var cut = RenderComponent<GanttChart>(parameters => parameters
            .Add(p => p.Tasks, tasks));

        // Assert
        var scheduledBars = cut.FindAll(".gantt-bar-scheduled");
        var actualBars = cut.FindAll(".gantt-bar-actual");

        // 初回ロード時はanimatingクラスが付与されない
        foreach (var bar in scheduledBars)
        {
            Assert.DoesNotContain("animating", bar.ClassName);
        }
        foreach (var bar in actualBars)
        {
            Assert.DoesNotContain("animating", bar.ClassName);
        }
    }

    [Fact(DisplayName = "バーが追加されたときにanimatingクラスが付与される")]
    public async Task GanttChart_AppliesAnimatingClass_WhenBarIsAdded()
    {
        // Arrange
        var initialTasks = new List<TaskDto>
        {
            new TaskDto
            {
                Id = Guid.NewGuid(),
                ProjectId = Guid.NewGuid(),
                Title = "Task 1",
                Description = "Test",
                Status = TaskStatus.Todo,
                ScheduledStartDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
                ScheduledEndDate = new DateTimeOffset(2024, 1, 5, 0, 0, 0, TimeSpan.Zero),
                EstimatedHours = 20,
                ActualStartDate = null,
                ActualEndDate = null,
                ActualHours = null,
                CreatedAt = DateTimeOffset.Now,
                UpdatedAt = null,
                CreatedBy = "test-user",
                UpdatedBy = null
            }
        };

        var cut = RenderComponent<GanttChart>(parameters => parameters
            .Add(p => p.Tasks, initialTasks));

        // Act - 新しいタスクを追加
        var updatedTasks = new List<TaskDto>(initialTasks)
        {
            new TaskDto
            {
                Id = Guid.NewGuid(),
                ProjectId = Guid.NewGuid(),
                Title = "Task 2",
                Description = "Test",
                Status = TaskStatus.Todo,
                ScheduledStartDate = new DateTimeOffset(2024, 1, 6, 0, 0, 0, TimeSpan.Zero),
                ScheduledEndDate = new DateTimeOffset(2024, 1, 10, 0, 0, 0, TimeSpan.Zero),
                EstimatedHours = 30,
                ActualStartDate = null,
                ActualEndDate = null,
                ActualHours = null,
                CreatedAt = DateTimeOffset.Now,
                UpdatedAt = null,
                CreatedBy = "test-user",
                UpdatedBy = null
            }
        };

        await cut.InvokeAsync(() => cut.SetParametersAndRender(parameters => parameters
            .Add(p => p.Tasks, updatedTasks)));

        // Assert
        var scheduledBars = cut.FindAll(".gantt-bar-scheduled");
        Assert.Equal(2, scheduledBars.Count);

        // 2番目のバー（新規追加）にanimatingクラスが付与されている
        var newBar = scheduledBars[1];
        Assert.Contains("animating", newBar.ClassName);
    }

    [Fact(DisplayName = "バーの日付が変更されたときにanimatingクラスが付与される")]
    public async Task GanttChart_AppliesAnimatingClass_WhenBarDatesChange()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var initialTasks = new List<TaskDto>
        {
            new TaskDto
            {
                Id = taskId,
                ProjectId = Guid.NewGuid(),
                Title = "Task 1",
                Description = "Test",
                Status = TaskStatus.Todo,
                ScheduledStartDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
                ScheduledEndDate = new DateTimeOffset(2024, 1, 5, 0, 0, 0, TimeSpan.Zero),
                EstimatedHours = 20,
                ActualStartDate = null,
                ActualEndDate = null,
                ActualHours = null,
                CreatedAt = DateTimeOffset.Now,
                UpdatedAt = null,
                CreatedBy = "test-user",
                UpdatedBy = null
            }
        };

        var cut = RenderComponent<GanttChart>(parameters => parameters
            .Add(p => p.Tasks, initialTasks));

        // Act - タスクの予定日を変更
        var updatedTasks = new List<TaskDto>
        {
            new TaskDto
            {
                Id = taskId,
                ProjectId = initialTasks[0].ProjectId,
                Title = "Task 1",
                Description = "Test",
                Status = TaskStatus.Todo,
                ScheduledStartDate = new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero), // 変更
                ScheduledEndDate = new DateTimeOffset(2024, 1, 8, 0, 0, 0, TimeSpan.Zero),   // 変更
                EstimatedHours = 20,
                ActualStartDate = null,
                ActualEndDate = null,
                ActualHours = null,
                CreatedAt = DateTimeOffset.Now,
                UpdatedAt = null,
                CreatedBy = "test-user",
                UpdatedBy = null
            }
        };

        await cut.InvokeAsync(() => cut.SetParametersAndRender(parameters => parameters
            .Add(p => p.Tasks, updatedTasks)));

        // Assert
        var scheduledBars = cut.FindAll(".gantt-bar-scheduled");
        Assert.Single(scheduledBars);

        // 日付が変更されたバーにanimatingクラスが付与されている
        Assert.Contains("animating", scheduledBars[0].ClassName);
    }

    [Fact(DisplayName = "変更のないバーにはアニメーションクラスが付与されない")]
    public async Task GanttChart_DoesNotApplyAnimatingClass_WhenBarUnchanged()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var initialTasks = new List<TaskDto>
        {
            new TaskDto
            {
                Id = taskId,
                ProjectId = Guid.NewGuid(),
                Title = "Task 1",
                Description = "Test",
                Status = TaskStatus.Todo,
                ScheduledStartDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
                ScheduledEndDate = new DateTimeOffset(2024, 1, 5, 0, 0, 0, TimeSpan.Zero),
                EstimatedHours = 20,
                ActualStartDate = null,
                ActualEndDate = null,
                ActualHours = null,
                CreatedAt = DateTimeOffset.Now,
                UpdatedAt = null,
                CreatedBy = "test-user",
                UpdatedBy = null
            }
        };

        var cut = RenderComponent<GanttChart>(parameters => parameters
            .Add(p => p.Tasks, initialTasks));

        // Act - 日付は変更せずに他のプロパティを変更
        var updatedTasks = new List<TaskDto>
        {
            new TaskDto
            {
                Id = taskId,
                ProjectId = initialTasks[0].ProjectId,
                Title = "Task 1 Updated", // タイトル変更
                Description = "Test Updated", // 説明変更
                Status = TaskStatus.InProgress, // ステータス変更
                ScheduledStartDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero), // 日付は同じ
                ScheduledEndDate = new DateTimeOffset(2024, 1, 5, 0, 0, 0, TimeSpan.Zero),   // 日付は同じ
                EstimatedHours = 30, // 見積もり変更
                ActualStartDate = null,
                ActualEndDate = null,
                ActualHours = null,
                CreatedAt = DateTimeOffset.Now,
                UpdatedAt = DateTimeOffset.Now,
                CreatedBy = "test-user",
                UpdatedBy = "test-user"
            }
        };

        await cut.InvokeAsync(() => cut.SetParametersAndRender(parameters => parameters
            .Add(p => p.Tasks, updatedTasks)));

        // Assert
        var scheduledBars = cut.FindAll(".gantt-bar-scheduled");
        Assert.Single(scheduledBars);

        // 日付が変更されていないのでanimatingクラスは付与されない
        Assert.DoesNotContain("animating", scheduledBars[0].ClassName);
    }

    [Fact(DisplayName = "実績バーが追加されたときにanimatingクラスが付与される")]
    public async Task GanttChart_AppliesAnimatingClass_WhenActualBarIsAdded()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var initialTasks = new List<TaskDto>
        {
            new TaskDto
            {
                Id = taskId,
                ProjectId = Guid.NewGuid(),
                Title = "Task 1",
                Description = "Test",
                Status = TaskStatus.Todo,
                ScheduledStartDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
                ScheduledEndDate = new DateTimeOffset(2024, 1, 5, 0, 0, 0, TimeSpan.Zero),
                EstimatedHours = 20,
                ActualStartDate = null, // 実績なし
                ActualEndDate = null,
                ActualHours = null,
                CreatedAt = DateTimeOffset.Now,
                UpdatedAt = null,
                CreatedBy = "test-user",
                UpdatedBy = null
            }
        };

        var cut = RenderComponent<GanttChart>(parameters => parameters
            .Add(p => p.Tasks, initialTasks));

        // Act - 実績期間を追加
        var updatedTasks = new List<TaskDto>
        {
            new TaskDto
            {
                Id = taskId,
                ProjectId = initialTasks[0].ProjectId,
                Title = "Task 1",
                Description = "Test",
                Status = TaskStatus.InProgress,
                ScheduledStartDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
                ScheduledEndDate = new DateTimeOffset(2024, 1, 5, 0, 0, 0, TimeSpan.Zero),
                EstimatedHours = 20,
                ActualStartDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero), // 実績追加
                ActualEndDate = new DateTimeOffset(2024, 1, 3, 0, 0, 0, TimeSpan.Zero),   // 実績追加
                ActualHours = 15,
                CreatedAt = DateTimeOffset.Now,
                UpdatedAt = DateTimeOffset.Now,
                CreatedBy = "test-user",
                UpdatedBy = "test-user"
            }
        };

        await cut.InvokeAsync(() => cut.SetParametersAndRender(parameters => parameters
            .Add(p => p.Tasks, updatedTasks)));

        // Assert
        var actualBars = cut.FindAll(".gantt-bar-actual");
        Assert.Single(actualBars);

        // 新規追加された実績バーにanimatingクラスが付与されている
        Assert.Contains("animating", actualBars[0].ClassName);
    }

    [Fact(DisplayName = "複数のバーのうち変更されたバーのみにanimatingクラスが付与される")]
    public async Task GanttChart_AppliesAnimatingClassOnlyToChangedBars()
    {
        // Arrange
        var task1Id = Guid.NewGuid();
        var task2Id = Guid.NewGuid();
        var initialTasks = new List<TaskDto>
        {
            new TaskDto
            {
                Id = task1Id,
                ProjectId = Guid.NewGuid(),
                Title = "Task 1",
                Description = "Test",
                Status = TaskStatus.Todo,
                ScheduledStartDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
                ScheduledEndDate = new DateTimeOffset(2024, 1, 5, 0, 0, 0, TimeSpan.Zero),
                EstimatedHours = 20,
                ActualStartDate = null,
                ActualEndDate = null,
                ActualHours = null,
                CreatedAt = DateTimeOffset.Now,
                UpdatedAt = null,
                CreatedBy = "test-user",
                UpdatedBy = null
            },
            new TaskDto
            {
                Id = task2Id,
                ProjectId = Guid.NewGuid(),
                Title = "Task 2",
                Description = "Test",
                Status = TaskStatus.Todo,
                ScheduledStartDate = new DateTimeOffset(2024, 1, 6, 0, 0, 0, TimeSpan.Zero),
                ScheduledEndDate = new DateTimeOffset(2024, 1, 10, 0, 0, 0, TimeSpan.Zero),
                EstimatedHours = 30,
                ActualStartDate = null,
                ActualEndDate = null,
                ActualHours = null,
                CreatedAt = DateTimeOffset.Now,
                UpdatedAt = null,
                CreatedBy = "test-user",
                UpdatedBy = null
            }
        };

        var cut = RenderComponent<GanttChart>(parameters => parameters
            .Add(p => p.Tasks, initialTasks));

        // Act - Task 1の日付のみ変更
        var updatedTasks = new List<TaskDto>
        {
            new TaskDto
            {
                Id = task1Id,
                ProjectId = initialTasks[0].ProjectId,
                Title = "Task 1",
                Description = "Test",
                Status = TaskStatus.Todo,
                ScheduledStartDate = new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero), // 変更
                ScheduledEndDate = new DateTimeOffset(2024, 1, 6, 0, 0, 0, TimeSpan.Zero),   // 変更
                EstimatedHours = 20,
                ActualStartDate = null,
                ActualEndDate = null,
                ActualHours = null,
                CreatedAt = DateTimeOffset.Now,
                UpdatedAt = DateTimeOffset.Now,
                CreatedBy = "test-user",
                UpdatedBy = "test-user"
            },
            new TaskDto
            {
                Id = task2Id,
                ProjectId = initialTasks[1].ProjectId,
                Title = "Task 2",
                Description = "Test",
                Status = TaskStatus.Todo,
                ScheduledStartDate = new DateTimeOffset(2024, 1, 6, 0, 0, 0, TimeSpan.Zero), // 変更なし
                ScheduledEndDate = new DateTimeOffset(2024, 1, 10, 0, 0, 0, TimeSpan.Zero),  // 変更なし
                EstimatedHours = 30,
                ActualStartDate = null,
                ActualEndDate = null,
                ActualHours = null,
                CreatedAt = DateTimeOffset.Now,
                UpdatedAt = null,
                CreatedBy = "test-user",
                UpdatedBy = null
            }
        };

        await cut.InvokeAsync(() => cut.SetParametersAndRender(parameters => parameters
            .Add(p => p.Tasks, updatedTasks)));

        // Assert
        var scheduledBars = cut.FindAll(".gantt-bar-scheduled");
        Assert.Equal(2, scheduledBars.Count);

        // Task 1のバー（変更あり）にはanimatingクラスが付与されている
        Assert.Contains("animating", scheduledBars[0].ClassName);

        // Task 2のバー（変更なし）にはanimatingクラスが付与されていない
        Assert.DoesNotContain("animating", scheduledBars[1].ClassName);
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
                ScheduledStartDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
                ScheduledEndDate = new DateTimeOffset(2024, 1, 5, 0, 0, 0, TimeSpan.Zero),
                EstimatedHours = 20,
                ActualStartDate = null,
                ActualEndDate = null,
                ActualHours = null,
                CreatedAt = DateTimeOffset.Now,
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
                ScheduledStartDate = new DateTimeOffset(2024, 1, 3, 0, 0, 0, TimeSpan.Zero),
                ScheduledEndDate = new DateTimeOffset(2024, 1, 8, 0, 0, 0, TimeSpan.Zero),
                EstimatedHours = 30,
                ActualStartDate = new DateTimeOffset(2024, 1, 3, 0, 0, 0, TimeSpan.Zero),
                ActualEndDate = new DateTimeOffset(2024, 1, 7, 0, 0, 0, TimeSpan.Zero),
                ActualHours = 28,
                CreatedAt = DateTimeOffset.Now,
                UpdatedAt = null,
                CreatedBy = "test-user",
                UpdatedBy = null
            }
        };
    }
}

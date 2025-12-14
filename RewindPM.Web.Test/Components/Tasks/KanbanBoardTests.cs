using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using RewindPM.Application.Read.DTOs;
using RewindPM.Web.Components.Tasks;
using TaskStatus = RewindPM.Domain.ValueObjects.TaskStatus;
using Xunit;

namespace RewindPM.Web.Test.Components.Tasks;

public class KanbanBoardTests : Bunit.TestContext
{
    private List<TaskDto> CreateTestTasks()
    {
        return new List<TaskDto>
        {
            new TaskDto
            {
                Id = Guid.NewGuid(),
                ProjectId = Guid.NewGuid(),
                Title = "Todo Task",
                Description = "Description 1",
                Status = TaskStatus.Todo,
                ScheduledStartDate = DateTimeOffset.Now.Date,
                ScheduledEndDate = DateTimeOffset.Now.Date.AddDays(1),
                EstimatedHours = 5,
                ActualStartDate = null,
                ActualEndDate = null,
                ActualHours = null,
                CreatedAt = DateTimeOffset.Now,
                UpdatedAt = null,
                CreatedBy = "User1",
                UpdatedBy = null
            },
            new TaskDto
            {
                Id = Guid.NewGuid(),
                ProjectId = Guid.NewGuid(),
                Title = "Progress Task",
                Description = "Description 2",
                Status = TaskStatus.InProgress,
                ScheduledStartDate = DateTimeOffset.Now.Date,
                ScheduledEndDate = DateTimeOffset.Now.Date.AddDays(1),
                EstimatedHours = 8,
                ActualStartDate = null,
                ActualEndDate = null,
                ActualHours = null,
                CreatedAt = DateTimeOffset.Now,
                UpdatedAt = null,
                CreatedBy = "User2",
                UpdatedBy = null
            }
        };
    }

    [Fact(DisplayName = "KanbanBoard_RendersColumnsCorrectly")]
    public void KanbanBoard_RendersColumnsCorrectly()
    {
        // Act
        var cut = RenderComponent<KanbanBoard>();

        // Assert
        var columns = cut.FindAll(".kanban-column");
        Assert.Equal(4, columns.Count); // Todo, InProgress, InReview, Done

        var headers = cut.FindAll(".column-header h3");
        Assert.Contains("未着手", headers[0].TextContent);
        Assert.Contains("進行中", headers[1].TextContent);
        Assert.Contains("レビュー中", headers[2].TextContent);
        Assert.Contains("完了", headers[3].TextContent);
    }

    [Fact(DisplayName = "KanbanBoard_RendersTasksInCorrectColumns")]
    public void KanbanBoard_RendersTasksInCorrectColumns()
    {
        // Arrange
        var tasks = CreateTestTasks();

        // Act
        var cut = RenderComponent<KanbanBoard>(parameters => parameters
            .Add(p => p.Tasks, tasks));

        // Assert
        var todoColumn = cut.FindAll(".kanban-column")[0];
        var progressColumn = cut.FindAll(".kanban-column")[1];

        // Check Todo Column
        var todoTasks = todoColumn.QuerySelectorAll(".task-card");
        Assert.Single(todoTasks);
        Assert.Contains("Todo Task", todoTasks[0].TextContent);

        // Check InProgress Column
        var progressTasks = progressColumn.QuerySelectorAll(".task-card");
        Assert.Single(progressTasks);
        Assert.Contains("Progress Task", progressTasks[0].TextContent);
    }

    [Fact(DisplayName = "KanbanBoard_RendersTaskCounts")]
    public void KanbanBoard_RendersTaskCounts()
    {
        // Arrange
        var tasks = CreateTestTasks();

        // Act
        var cut = RenderComponent<KanbanBoard>(parameters => parameters
            .Add(p => p.Tasks, tasks));

        // Assert
        var counts = cut.FindAll(".task-count");
        Assert.Equal("1", counts[0].TextContent); // Todo
        Assert.Equal("1", counts[1].TextContent); // InProgress
        Assert.Equal("0", counts[2].TextContent); // InReview
        Assert.Equal("0", counts[3].TextContent); // Done
    }

    [Fact(DisplayName = "KanbanBoard_InvokesOnTaskClick")]
    public async Task KanbanBoard_InvokesOnTaskClick()
    {
        // Arrange
        var tasks = CreateTestTasks();
        TaskDto? clickedTask = null;
        var cut = RenderComponent<KanbanBoard>(parameters => parameters
            .Add(p => p.Tasks, tasks)
            .Add(p => p.OnTaskClick, EventCallback.Factory.Create<TaskDto>(this, t => clickedTask = t)));

        // Act
        var taskCard = cut.Find(".task-card"); // Gets the first one (Todo Task)
        await taskCard.ClickAsync(new MouseEventArgs());

        // Assert
        Assert.NotNull(clickedTask);
        Assert.Equal("Todo Task", clickedTask!.Title);
    }

    [Fact(DisplayName = "KanbanBoard_DragDrop_ChangesStatus")]
    public async Task KanbanBoard_DragDrop_ChangesStatus()
    {
        // Arrange
        var tasks = CreateTestTasks();
        var todoTask = tasks[0];
        (TaskDto task, TaskStatus newStatus)? statusChange = null;

        var cut = RenderComponent<KanbanBoard>(parameters => parameters
            .Add(p => p.Tasks, tasks)
            .Add(p => p.OnTaskStatusChanged, EventCallback.Factory.Create<(TaskDto, TaskStatus)>(this, args => statusChange = args)));

        // Act
        // 1. Simulate Drag Start on Todo Task
        var taskCard = cut.FindAll(".task-card")[0];
        taskCard.DragStart(new DragEventArgs());

        // 2. Simulate Drop on InProgress Column
        var inProgressColumn = cut.FindAll(".kanban-column")[1];
        inProgressColumn.Drop(new DragEventArgs());

        // Assert
        Assert.NotNull(statusChange);
        Assert.Equal(todoTask.Id, statusChange!.Value.task.Id);
        Assert.Equal(TaskStatus.InProgress, statusChange.Value.newStatus);
    }

    [Fact(DisplayName = "KanbanBoard_ReadOnly_DisablesDragAndDrop")]
    public void KanbanBoard_ReadOnly_DisablesDragAndDrop()
    {
        // Arrange
        var tasks = CreateTestTasks();

        // Act
        var cut = RenderComponent<KanbanBoard>(parameters => parameters
            .Add(p => p.Tasks, tasks)
            .Add(p => p.IsReadOnly, true));

        // Assert
        var taskCards = cut.FindAll(".task-card");
        foreach (var card in taskCards)
        {
            Assert.Equal("false", card.GetAttribute("draggable"));
        }
    }
    
    [Fact(DisplayName = "KanbanBoard_ReadOnly_DoesNotTriggerDrop")]
    public async Task KanbanBoard_ReadOnly_DoesNotTriggerDrop()
    {
        // Arrange
        var tasks = CreateTestTasks();
        bool statusChanged = false;

        var cut = RenderComponent<KanbanBoard>(parameters => parameters
            .Add(p => p.Tasks, tasks)
            .Add(p => p.IsReadOnly, true)
            .Add(p => p.OnTaskStatusChanged, EventCallback.Factory.Create<(TaskDto, TaskStatus)>(this, _ => statusChanged = true)));

        // Act
        // Attempt trigger drag flow (though UI prevents it, we test logic safety)
        var taskCard = cut.FindAll(".task-card")[0];
        // We can force invoke the event handlers even if draggable=false in some testing scenarios,
        // but here we want to ensure the component logic respects IsReadOnly check inside handlers.
        
        // Use reflection or internal state simulation if needed, but since we can't easily set private state _draggedTask from outside without triggering DragStart:
        // Trigger DragStart (should return early or not set state if we had a check, but the component check is inside HandleDragStart)
        taskCard.DragStart(new DragEventArgs());
        
        // Trigger Drop
        var inProgressColumn = cut.FindAll(".kanban-column")[1];
        inProgressColumn.Drop(new DragEventArgs());

        // Assert
        Assert.False(statusChanged, "Status change should not occur in ReadOnly mode");
    }

    // ========== アニメーション機能のテスト ==========

    [Fact(DisplayName = "初回ロード時はアニメーションクラスが付与されない")]
    public void KanbanBoard_DoesNotApplyAnimationClass_OnInitialLoad()
    {
        // Arrange
        var tasks = CreateTestTasks();

        // Act
        var cut = RenderComponent<KanbanBoard>(parameters => parameters
            .Add(p => p.Tasks, tasks));

        // Assert
        var taskCards = cut.FindAll(".task-card");
        
        // 初回ロード時はfading-outクラスが付与されない
        foreach (var card in taskCards)
        {
            Assert.DoesNotContain("fading-out", card.ClassName);
        }
    }

    [Fact(DisplayName = "タスクが削除されたときにfading-outクラスが付与される")]
    public async Task KanbanBoard_AppliesFadingOutClass_WhenTaskIsRemoved()
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
                ScheduledStartDate = DateTimeOffset.Now.Date,
                ScheduledEndDate = DateTimeOffset.Now.Date.AddDays(1),
                EstimatedHours = 5,
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
                ScheduledStartDate = DateTimeOffset.Now.Date,
                ScheduledEndDate = DateTimeOffset.Now.Date.AddDays(1),
                EstimatedHours = 8,
                ActualStartDate = null,
                ActualEndDate = null,
                ActualHours = null,
                CreatedAt = DateTimeOffset.Now,
                UpdatedAt = null,
                CreatedBy = "test-user",
                UpdatedBy = null
            }
        };

        var cut = RenderComponent<KanbanBoard>(parameters => parameters
            .Add(p => p.Tasks, initialTasks));

        // Act - タスクを1つ削除
        var updatedTasks = new List<TaskDto>
        {
            new TaskDto
            {
                Id = task2Id,
                ProjectId = initialTasks[1].ProjectId,
                Title = "Task 2",
                Description = "Test",
                Status = TaskStatus.Todo,
                ScheduledStartDate = DateTimeOffset.Now.Date,
                ScheduledEndDate = DateTimeOffset.Now.Date.AddDays(1),
                EstimatedHours = 8,
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
        var taskCards = cut.FindAll(".task-card");
        // フェードアウトアニメーション中は2つのカードが表示される（１つはfading-out）
        Assert.Equal(2, taskCards.Count);

        // 削除されたタスク（task1）がfading-outクラスを持つ
        var fadingOutCards = taskCards.Where(c => c.ClassName?.Contains("fading-out") ?? false).ToList();
        Assert.Single(fadingOutCards);
    }

    [Fact(DisplayName = "タスクのステータスが変更されたときにfading-outクラスが付与される")]
    public async Task KanbanBoard_AppliesFadingOutClass_WhenTaskStatusChanges()
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
                ScheduledStartDate = DateTimeOffset.Now.Date,
                ScheduledEndDate = DateTimeOffset.Now.Date.AddDays(1),
                EstimatedHours = 5,
                ActualStartDate = null,
                ActualEndDate = null,
                ActualHours = null,
                CreatedAt = DateTimeOffset.Now,
                UpdatedAt = null,
                CreatedBy = "test-user",
                UpdatedBy = null
            }
        };

        var cut = RenderComponent<KanbanBoard>(parameters => parameters
            .Add(p => p.Tasks, initialTasks));

        // Act - タスクのステータスを変更
        var updatedTasks = new List<TaskDto>
        {
            new TaskDto
            {
                Id = taskId,
                ProjectId = initialTasks[0].ProjectId,
                Title = "Task 1",
                Description = "Test",
                Status = TaskStatus.InProgress, // ステータス変更
                ScheduledStartDate = DateTimeOffset.Now.Date,
                ScheduledEndDate = DateTimeOffset.Now.Date.AddDays(1),
                EstimatedHours = 5,
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
        var taskCards = cut.FindAll(".task-card");
        
        // ステータス変更時は古いステータスのカードがfading-outになる
        var fadingOutCards = taskCards.Where(c => c.ClassName?.Contains("fading-out") ?? false).ToList();
        Assert.Single(fadingOutCards);
    }

    [Fact(DisplayName = "新しいタスクが追加されたときはfading-outクラスが付与されない")]
    public async Task KanbanBoard_DoesNotApplyFadingOutClass_WhenTaskIsAdded()
    {
        // Arrange
        var task1Id = Guid.NewGuid();
        var initialTasks = new List<TaskDto>
        {
            new TaskDto
            {
                Id = task1Id,
                ProjectId = Guid.NewGuid(),
                Title = "Task 1",
                Description = "Test",
                Status = TaskStatus.Todo,
                ScheduledStartDate = DateTimeOffset.Now.Date,
                ScheduledEndDate = DateTimeOffset.Now.Date.AddDays(1),
                EstimatedHours = 5,
                ActualStartDate = null,
                ActualEndDate = null,
                ActualHours = null,
                CreatedAt = DateTimeOffset.Now,
                UpdatedAt = null,
                CreatedBy = "test-user",
                UpdatedBy = null
            }
        };

        var cut = RenderComponent<KanbanBoard>(parameters => parameters
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
                ScheduledStartDate = DateTimeOffset.Now.Date,
                ScheduledEndDate = DateTimeOffset.Now.Date.AddDays(1),
                EstimatedHours = 8,
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
        var taskCards = cut.FindAll(".task-card");
        Assert.Equal(2, taskCards.Count);

        // 新規追加なのでfading-outクラスは付与されない
        var fadingOutCards = taskCards.Where(c => c.ClassName?.Contains("fading-out") ?? false).ToList();
        Assert.Empty(fadingOutCards);
    }
}

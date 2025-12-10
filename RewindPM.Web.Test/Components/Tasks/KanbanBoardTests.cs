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
                ScheduledStartDate = DateTime.Today,
                ScheduledEndDate = DateTime.Today.AddDays(1),
                EstimatedHours = 5,
                ActualStartDate = null,
                ActualEndDate = null,
                ActualHours = null,
                CreatedAt = DateTime.Now,
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
                ScheduledStartDate = DateTime.Today,
                ScheduledEndDate = DateTime.Today.AddDays(1),
                EstimatedHours = 8,
                ActualStartDate = null,
                ActualEndDate = null,
                ActualHours = null,
                CreatedAt = DateTime.Now,
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
        Assert.Contains("To Do", headers[0].TextContent);
        Assert.Contains("In Progress", headers[1].TextContent);
        Assert.Contains("In Review", headers[2].TextContent);
        Assert.Contains("Done", headers[3].TextContent);
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
}

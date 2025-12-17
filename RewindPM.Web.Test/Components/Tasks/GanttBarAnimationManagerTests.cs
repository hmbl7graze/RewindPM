using RewindPM.Application.Read.DTOs;
using RewindPM.Domain.ValueObjects;
using RewindPM.Web.Components.Tasks;
using TaskStatus = RewindPM.Domain.ValueObjects.TaskStatus;

namespace RewindPM.Web.Test.Components.Tasks;

public class GanttBarAnimationManagerTests
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
    public void IsInitialLoad_ReturnsTrue_WhenNoPreviousBarStates()
    {
        // Arrange
        var manager = new GanttBarAnimationManager();

        // Act
        var result = manager.IsInitialLoad();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsInitialLoad_ReturnsFalse_AfterInitialization()
    {
        // Arrange
        var manager = new GanttBarAnimationManager();
        var tasks = new List<TaskDto>
        {
            CreateTaskDto(
                title: "Task 1",
                scheduledStartDate: DateTimeOffset.Now,
                scheduledEndDate: DateTimeOffset.Now.AddDays(5))
        };

        // Act
        manager.InitializeBarStates(tasks);

        // Assert
        Assert.False(manager.IsInitialLoad());
    }

    [Fact]
    public void InitializeBarStates_WithValidTasks_StoresBarStates()
    {
        // Arrange
        var manager = new GanttBarAnimationManager();
        var taskId = Guid.NewGuid();
        var startDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var endDate = new DateTimeOffset(2024, 1, 10, 0, 0, 0, TimeSpan.Zero);
        
        var tasks = new List<TaskDto>
        {
            CreateTaskDto(
                id: taskId,
                title: "Task 1",
                scheduledStartDate: startDate,
                scheduledEndDate: endDate)
        };

        // Act
        manager.InitializeBarStates(tasks);
        manager.InitializeDisplayBarStates();

        // Assert
        Assert.True(manager.ShouldDisplayBar(taskId, "scheduled"));
        
        var state = manager.GetDisplayBarState(taskId, "scheduled");
        Assert.NotNull(state);
        Assert.Equal(startDate, state.StartDate);
        Assert.Equal(endDate, state.EndDate);
    }

    [Fact]
    public void InitializeBarStates_WithActualDates_StoresBothBarStates()
    {
        // Arrange
        var manager = new GanttBarAnimationManager();
        var taskId = Guid.NewGuid();
        var scheduledStart = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var scheduledEnd = new DateTimeOffset(2024, 1, 10, 0, 0, 0, TimeSpan.Zero);
        var actualStart = new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero);
        var actualEnd = new DateTimeOffset(2024, 1, 12, 0, 0, 0, TimeSpan.Zero);
        
        var tasks = new List<TaskDto>
        {
            CreateTaskDto(
                id: taskId,
                title: "Task 1",
                scheduledStartDate: scheduledStart,
                scheduledEndDate: scheduledEnd,
                actualStartDate: actualStart,
                actualEndDate: actualEnd)
        };

        // Act
        manager.InitializeBarStates(tasks);
        manager.InitializeDisplayBarStates();

        // Assert
        Assert.True(manager.ShouldDisplayBar(taskId, "scheduled"));
        Assert.True(manager.ShouldDisplayBar(taskId, "actual"));
    }

    [Fact]
    public void GetCurrentBarStates_WithNullTasks_ReturnsEmptyDictionary()
    {
        // Arrange
        var manager = new GanttBarAnimationManager();

        // Act
        var states = manager.GetCurrentBarStates(null!);

        // Assert
        Assert.Empty(states);
    }

    [Fact]
    public void GetRemovedBars_ReturnsRemovedBarKeys()
    {
        // Arrange
        var manager = new GanttBarAnimationManager();
        var taskId1 = Guid.NewGuid();
        var taskId2 = Guid.NewGuid();
        
        var initialTasks = new List<TaskDto>
        {
            CreateTaskDto(
                id: taskId1,
                title: "Task 1",
                scheduledStartDate: DateTimeOffset.Now,
                scheduledEndDate: DateTimeOffset.Now.AddDays(5)),
            CreateTaskDto(
                id: taskId2,
                title: "Task 2",
                scheduledStartDate: DateTimeOffset.Now,
                scheduledEndDate: DateTimeOffset.Now.AddDays(3))
        };
        
        manager.InitializeBarStates(initialTasks);

        var updatedTasks = new List<TaskDto>
        {
            CreateTaskDto(
                id: taskId1,
                title: "Task 1",
                scheduledStartDate: DateTimeOffset.Now,
                scheduledEndDate: DateTimeOffset.Now.AddDays(5))
        };
        
        var currentStates = manager.GetCurrentBarStates(updatedTasks);

        // Act
        var removedBars = manager.GetRemovedBars(currentStates);

        // Assert
        Assert.Single(removedBars);
        Assert.Contains(GanttBarAnimationManager.GetBarKey(taskId2, "scheduled"), removedBars);
    }

    [Fact]
    public void PrepareBarRemovalAnimation_MarksBarsForFadeOut()
    {
        // Arrange
        var manager = new GanttBarAnimationManager();
        var taskId = Guid.NewGuid();
        var tasks = new List<TaskDto>
        {
            CreateTaskDto(
                id: taskId,
                title: "Task 1",
                scheduledStartDate: DateTimeOffset.Now,
                scheduledEndDate: DateTimeOffset.Now.AddDays(5))
        };
        
        manager.InitializeBarStates(tasks);
        manager.InitializeDisplayBarStates();

        var removedBars = new List<string> { GanttBarAnimationManager.GetBarKey(taskId, "scheduled") };

        // Act
        manager.PrepareBarRemovalAnimation(removedBars);

        // Assert
        var animationClass = manager.GetBarAnimationClass(taskId, "scheduled");
        Assert.Equal("fading-out", animationClass);
    }

    [Fact]
    public void CompleteBarRemovalAnimation_ClearsFadingOutBars()
    {
        // Arrange
        var manager = new GanttBarAnimationManager();
        var taskId = Guid.NewGuid();
        var tasks = new List<TaskDto>
        {
            CreateTaskDto(
                id: taskId,
                title: "Task 1",
                scheduledStartDate: DateTimeOffset.Now,
                scheduledEndDate: DateTimeOffset.Now.AddDays(5))
        };
        
        manager.InitializeBarStates(tasks);
        manager.InitializeDisplayBarStates();

        var removedBars = new List<string> { GanttBarAnimationManager.GetBarKey(taskId, "scheduled") };
        manager.PrepareBarRemovalAnimation(removedBars);

        // Act
        manager.CompleteBarRemovalAnimation();

        // Assert
        var animationClass = manager.GetBarAnimationClass(taskId, "scheduled");
        Assert.Equal("", animationClass);
    }

    [Fact]
    public void DetectChangesAndUpdate_DetectsNewBars()
    {
        // Arrange
        var manager = new GanttBarAnimationManager();
        var taskId = Guid.NewGuid();
        
        manager.InitializeBarStates(new List<TaskDto>());

        var newTasks = new List<TaskDto>
        {
            CreateTaskDto(
                id: taskId,
                title: "Task 1",
                scheduledStartDate: DateTimeOffset.Now,
                scheduledEndDate: DateTimeOffset.Now.AddDays(5))
        };

        // Act
        manager.DetectChangesAndUpdate(newTasks);

        // Assert
        var animationClass = manager.GetBarAnimationClass(taskId, "scheduled");
        Assert.Equal("animating", animationClass);
    }

    [Fact]
    public void DetectChangesAndUpdate_DetectsBarChanges()
    {
        // Arrange
        var manager = new GanttBarAnimationManager();
        var taskId = Guid.NewGuid();
        var initialStart = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var initialEnd = new DateTimeOffset(2024, 1, 10, 0, 0, 0, TimeSpan.Zero);
        
        var initialTasks = new List<TaskDto>
        {
            CreateTaskDto(
                id: taskId,
                title: "Task 1",
                scheduledStartDate: initialStart,
                scheduledEndDate: initialEnd)
        };
        
        manager.InitializeBarStates(initialTasks);
        manager.InitializeDisplayBarStates();

        var updatedStart = new DateTimeOffset(2024, 1, 5, 0, 0, 0, TimeSpan.Zero);
        var updatedEnd = new DateTimeOffset(2024, 1, 15, 0, 0, 0, TimeSpan.Zero);
        
        var updatedTasks = new List<TaskDto>
        {
            CreateTaskDto(
                id: taskId,
                title: "Task 1",
                scheduledStartDate: updatedStart,
                scheduledEndDate: updatedEnd)
        };

        // Act
        manager.DetectChangesAndUpdate(updatedTasks);

        // Assert
        var animationClass = manager.GetBarAnimationClass(taskId, "scheduled");
        Assert.Equal("animating", animationClass);
    }

    [Fact]
    public void GetBarKey_ReturnsCorrectFormat()
    {
        // Arrange
        var taskId = Guid.Parse("12345678-1234-1234-1234-123456789012");

        // Act
        var scheduledKey = GanttBarAnimationManager.GetBarKey(taskId, "scheduled");
        var actualKey = GanttBarAnimationManager.GetBarKey(taskId, "actual");

        // Assert
        Assert.Equal("12345678-1234-1234-1234-123456789012_scheduled", scheduledKey);
        Assert.Equal("12345678-1234-1234-1234-123456789012_actual", actualKey);
    }

    [Fact]
    public void ShouldDisplayBar_ReturnsFalse_ForNonExistentBar()
    {
        // Arrange
        var manager = new GanttBarAnimationManager();
        var taskId = Guid.NewGuid();

        // Act
        var result = manager.ShouldDisplayBar(taskId, "scheduled");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetDisplayBarState_ReturnsNull_ForNonExistentBar()
    {
        // Arrange
        var manager = new GanttBarAnimationManager();
        var taskId = Guid.NewGuid();

        // Act
        var result = manager.GetDisplayBarState(taskId, "scheduled");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetBarAnimationClass_ReturnsEmpty_ForNonAnimatingBar()
    {
        // Arrange
        var manager = new GanttBarAnimationManager();
        var taskId = Guid.NewGuid();
        var tasks = new List<TaskDto>
        {
            CreateTaskDto(
                id: taskId,
                title: "Task 1",
                scheduledStartDate: DateTimeOffset.Now,
                scheduledEndDate: DateTimeOffset.Now.AddDays(5))
        };
        
        manager.InitializeBarStates(tasks);
        manager.InitializeDisplayBarStates();

        // Act
        var animationClass = manager.GetBarAnimationClass(taskId, "scheduled");

        // Assert
        Assert.Equal("", animationClass);
    }

    [Fact]
    public void BarState_Equals_ReturnsTrueForSameDates()
    {
        // Arrange
        var startDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var endDate = new DateTimeOffset(2024, 1, 10, 0, 0, 0, TimeSpan.Zero);
        
        var state1 = new GanttBarAnimationManager.BarState(startDate, endDate);
        var state2 = new GanttBarAnimationManager.BarState(startDate, endDate);

        // Act & Assert
        Assert.True(state1.Equals(state2));
    }

    [Fact]
    public void BarState_Equals_ReturnsFalseForDifferentDates()
    {
        // Arrange
        var state1 = new GanttBarAnimationManager.BarState(
            new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 1, 10, 0, 0, 0, TimeSpan.Zero)
        );
        
        var state2 = new GanttBarAnimationManager.BarState(
            new DateTimeOffset(2024, 1, 5, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2024, 1, 15, 0, 0, 0, TimeSpan.Zero)
        );

        // Act & Assert
        Assert.False(state1.Equals(state2));
    }

    [Fact]
    public void BarState_GetHashCode_ReturnsSameHashForEqualStates()
    {
        // Arrange
        var startDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var endDate = new DateTimeOffset(2024, 1, 10, 0, 0, 0, TimeSpan.Zero);
        
        var state1 = new GanttBarAnimationManager.BarState(startDate, endDate);
        var state2 = new GanttBarAnimationManager.BarState(startDate, endDate);

        // Act & Assert
        Assert.Equal(state1.GetHashCode(), state2.GetHashCode());
    }
}

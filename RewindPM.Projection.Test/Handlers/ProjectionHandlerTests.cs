using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RewindPM.Domain.Events;
using RewindPM.Domain.ValueObjects;
using RewindPM.Infrastructure.Read.Persistence;
using RewindPM.Projection.Handlers;
using TaskStatus = RewindPM.Domain.ValueObjects.TaskStatus;

namespace RewindPM.Projection.Test.Handlers;

/// <summary>
/// プロジェクションハンドラーの単体テスト
/// </summary>
public class ProjectionHandlerTests : IAsyncDisposable
{
    private readonly ReadModelDbContext _context;

    public ProjectionHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ReadModelDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ReadModelDbContext(options);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    private ILogger<T> CreateLogger<T>()
    {
        return LoggerFactory.Create(builder => { })
            .CreateLogger<T>();
    }

    #region ProjectCreatedEventHandler Tests

    [Fact(DisplayName = "ProjectCreatedイベントでプロジェクトとスナップショットが作成されること")]
    public async Task ProjectCreatedEventHandler_Should_Create_Project_And_Snapshot()
    {
        // Arrange
        var handler = new ProjectCreatedEventHandler(_context, CreateLogger<ProjectCreatedEventHandler>());
        var projectId = Guid.NewGuid();
        var occurredAt = new DateTime(2025, 12, 6, 10, 0, 0, DateTimeKind.Utc);
        var @event = new ProjectCreated
        {
            AggregateId = projectId,
            Title = "Test Project",
            Description = "Test Description",
            CreatedBy = "user1",
            OccurredAt = occurredAt
        };

        // Act
        await handler.HandleAsync(@event);

        // Assert
        var project = await _context.Projects.FindAsync([projectId], TestContext.Current.CancellationToken);
        Assert.NotNull(project);
        Assert.Equal("Test Project", project.Title);
        Assert.Equal("Test Description", project.Description);
        Assert.Equal("user1", project.CreatedBy);
        Assert.Equal(occurredAt, project.CreatedAt);
        Assert.Null(project.UpdatedAt);
        Assert.Null(project.UpdatedBy);

        var snapshot = await _context.ProjectHistories
            .FirstOrDefaultAsync(h => h.ProjectId == projectId && h.SnapshotDate == occurredAt.Date, TestContext.Current.CancellationToken);
        Assert.NotNull(snapshot);
        Assert.Equal("Test Project", snapshot.Title);
        Assert.Equal("Test Description", snapshot.Description);
        Assert.Equal("user1", snapshot.CreatedBy);
    }

    #endregion

    #region ProjectUpdatedEventHandler Tests

    [Fact(DisplayName = "ProjectUpdatedイベントでプロジェクトが更新され、スナップショットが作成されること")]
    public async Task ProjectUpdatedEventHandler_Should_Update_Project_And_Create_Snapshot()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var createdAt = new DateTime(2025, 12, 5, 10, 0, 0, DateTimeKind.Utc);
        var updatedAt = new DateTime(2025, 12, 6, 15, 0, 0, DateTimeKind.Utc);

        // 既存のプロジェクトを作成
        var project = new Infrastructure.Read.Entities.ProjectEntity
        {
            Id = projectId,
            Title = "Old Title",
            Description = "Old Description",
            CreatedBy = "user1",
            CreatedAt = createdAt
        };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ProjectUpdatedEventHandler(_context, CreateLogger<ProjectUpdatedEventHandler>());
        var @event = new ProjectUpdated
        {
            AggregateId = projectId,
            Title = "New Title",
            Description = "New Description",
            UpdatedBy = "user2",
            OccurredAt = updatedAt
        };

        // Act
        await handler.HandleAsync(@event);

        // Assert
        var updatedProject = await _context.Projects.FindAsync([projectId], TestContext.Current.CancellationToken);
        Assert.NotNull(updatedProject);
        Assert.Equal("New Title", updatedProject.Title);
        Assert.Equal("New Description", updatedProject.Description);
        Assert.Equal("user2", updatedProject.UpdatedBy);
        Assert.Equal(updatedAt, updatedProject.UpdatedAt);

        var snapshot = await _context.ProjectHistories
            .FirstOrDefaultAsync(h => h.ProjectId == projectId && h.SnapshotDate == updatedAt.Date, TestContext.Current.CancellationToken);
        Assert.NotNull(snapshot);
        Assert.Equal("New Title", snapshot.Title);
        Assert.Equal("New Description", snapshot.Description);
    }

    [Fact(DisplayName = "ProjectUpdatedイベントで同日の既存スナップショットが更新されること")]
    public async Task ProjectUpdatedEventHandler_Should_Update_Existing_Snapshot_On_Same_Day()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var today = new DateTime(2025, 12, 6, 10, 0, 0, DateTimeKind.Utc);

        // 既存のプロジェクトとスナップショットを作成
        var project = new Infrastructure.Read.Entities.ProjectEntity
        {
            Id = projectId,
            Title = "Original Title",
            Description = "Original Description",
            CreatedBy = "user1",
            CreatedAt = today.AddDays(-1)
        };
        _context.Projects.Add(project);

        var existingSnapshot = new Infrastructure.Read.Entities.ProjectHistoryEntity
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            SnapshotDate = today.Date,
            Title = "First Update",
            Description = "First Update Description",
            CreatedBy = "user1",
            CreatedAt = today.AddDays(-1),
            SnapshotCreatedAt = today.AddHours(-2)
        };
        _context.ProjectHistories.Add(existingSnapshot);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ProjectUpdatedEventHandler(_context, CreateLogger<ProjectUpdatedEventHandler>());
        var @event = new ProjectUpdated
        {
            AggregateId = projectId,
            Title = "Second Update",
            Description = "Second Update Description",
            UpdatedBy = "user2",
            OccurredAt = today
        };

        // Act
        await handler.HandleAsync(@event);

        // Assert
        var snapshots = await _context.ProjectHistories
            .Where(h => h.ProjectId == projectId && h.SnapshotDate == today.Date)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Single(snapshots); // 同じ日のスナップショットは1つだけ
        Assert.Equal("Second Update", snapshots[0].Title);
        Assert.Equal("Second Update Description", snapshots[0].Description);
        Assert.Equal("user2", snapshots[0].UpdatedBy);
    }

    #endregion

    #region TaskCreatedEventHandler Tests

    [Fact(DisplayName = "TaskCreatedイベントでタスクとスナップショットが作成されること")]
    public async Task TaskCreatedEventHandler_Should_Create_Task_And_Snapshot()
    {
        // Arrange
        var handler = new TaskCreatedEventHandler(_context, CreateLogger<TaskCreatedEventHandler>());
        var taskId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var occurredAt = new DateTime(2025, 12, 6, 10, 0, 0, DateTimeKind.Utc);
        var scheduledPeriod = new ScheduledPeriod(
            new DateTime(2025, 12, 10),
            new DateTime(2025, 12, 20),
            40
        );

        var @event = new TaskCreated
        {
            AggregateId = taskId,
            ProjectId = projectId,
            Title = "Test Task",
            Description = "Test Task Description",
            ScheduledPeriod = scheduledPeriod,
            CreatedBy = "user1",
            OccurredAt = occurredAt
        };

        // Act
        await handler.HandleAsync(@event);

        // Assert
        var task = await _context.Tasks.FindAsync([taskId], TestContext.Current.CancellationToken);
        Assert.NotNull(task);
        Assert.Equal(projectId, task.ProjectId);
        Assert.Equal("Test Task", task.Title);
        Assert.Equal("Test Task Description", task.Description);
        Assert.Equal(TaskStatus.Todo, task.Status);
        Assert.Equal(new DateTime(2025, 12, 10), task.ScheduledStartDate);
        Assert.Equal(new DateTime(2025, 12, 20), task.ScheduledEndDate);
        Assert.Equal(40, task.EstimatedHours);
        Assert.Equal("user1", task.CreatedBy);
        Assert.Null(task.ActualStartDate);
        Assert.Null(task.ActualEndDate);
        Assert.Null(task.ActualHours);

        var snapshot = await _context.TaskHistories
            .FirstOrDefaultAsync(h => h.TaskId == taskId && h.SnapshotDate == occurredAt.Date, TestContext.Current.CancellationToken);
        Assert.NotNull(snapshot);
        Assert.Equal("Test Task", snapshot.Title);
        Assert.Equal(TaskStatus.Todo, snapshot.Status);
    }

    #endregion

    #region TaskUpdatedEventHandler Tests

    [Fact(DisplayName = "TaskUpdatedイベントでタスクが更新され、スナップショットが作成されること")]
    public async Task TaskUpdatedEventHandler_Should_Update_Task_And_Create_Snapshot()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var createdAt = new DateTime(2025, 12, 5, 10, 0, 0, DateTimeKind.Utc);
        var updatedAt = new DateTime(2025, 12, 6, 15, 0, 0, DateTimeKind.Utc);

        // 既存のタスクを作成
        var task = new Infrastructure.Read.Entities.TaskEntity
        {
            Id = taskId,
            ProjectId = projectId,
            Title = "Old Task Title",
            Description = "Old Task Description",
            Status = TaskStatus.Todo,
            CreatedBy = "user1",
            CreatedAt = createdAt
        };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new TaskUpdatedEventHandler(_context, CreateLogger<TaskUpdatedEventHandler>());
        var @event = new TaskUpdated
        {
            AggregateId = taskId,
            Title = "New Task Title",
            Description = "New Task Description",
            UpdatedBy = "user2",
            OccurredAt = updatedAt
        };

        // Act
        await handler.HandleAsync(@event);

        // Assert
        var updatedTask = await _context.Tasks.FindAsync([taskId], TestContext.Current.CancellationToken);
        Assert.NotNull(updatedTask);
        Assert.Equal("New Task Title", updatedTask.Title);
        Assert.Equal("New Task Description", updatedTask.Description);
        Assert.Equal("user2", updatedTask.UpdatedBy);
        Assert.Equal(updatedAt, updatedTask.UpdatedAt);

        var snapshot = await _context.TaskHistories
            .FirstOrDefaultAsync(h => h.TaskId == taskId && h.SnapshotDate == updatedAt.Date, TestContext.Current.CancellationToken);
        Assert.NotNull(snapshot);
        Assert.Equal("New Task Title", snapshot.Title);
        Assert.Equal("New Task Description", snapshot.Description);
    }

    [Fact(DisplayName = "TaskUpdatedイベントで同日の既存スナップショットが更新されること")]
    public async Task TaskUpdatedEventHandler_Should_Update_Existing_Snapshot_On_Same_Day()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var today = new DateTime(2025, 12, 6, 10, 0, 0, DateTimeKind.Utc);

        // 既存のタスクとスナップショットを作成
        var task = new Infrastructure.Read.Entities.TaskEntity
        {
            Id = taskId,
            ProjectId = projectId,
            Title = "Original Task Title",
            Description = "Original Task Description",
            Status = TaskStatus.Todo,
            CreatedBy = "user1",
            CreatedAt = today.AddDays(-1)
        };
        _context.Tasks.Add(task);

        var existingSnapshot = new Infrastructure.Read.Entities.TaskHistoryEntity
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            ProjectId = projectId,
            SnapshotDate = today.Date,
            Title = "First Update",
            Description = "First Update Description",
            Status = TaskStatus.Todo,
            CreatedBy = "user1",
            CreatedAt = today.AddDays(-1),
            SnapshotCreatedAt = today.AddHours(-2)
        };
        _context.TaskHistories.Add(existingSnapshot);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new TaskUpdatedEventHandler(_context, CreateLogger<TaskUpdatedEventHandler>());
        var @event = new TaskUpdated
        {
            AggregateId = taskId,
            Title = "Second Update",
            Description = "Second Update Description",
            UpdatedBy = "user2",
            OccurredAt = today
        };

        // Act
        await handler.HandleAsync(@event);

        // Assert
        var snapshots = await _context.TaskHistories
            .Where(h => h.TaskId == taskId && h.SnapshotDate == today.Date)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Single(snapshots);
        Assert.Equal("Second Update", snapshots[0].Title);
        Assert.Equal("Second Update Description", snapshots[0].Description);
    }

    #endregion

    #region TaskStatusChangedEventHandler Tests

    [Fact(DisplayName = "TaskStatusChangedイベントでタスクステータスが更新され、スナップショットが作成されること")]
    public async Task TaskStatusChangedEventHandler_Should_Update_Task_Status_And_Create_Snapshot()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var createdAt = new DateTime(2025, 12, 5, 10, 0, 0, DateTimeKind.Utc);
        var changedAt = new DateTime(2025, 12, 6, 15, 0, 0, DateTimeKind.Utc);

        // 既存のタスクを作成
        var task = new Infrastructure.Read.Entities.TaskEntity
        {
            Id = taskId,
            ProjectId = projectId,
            Title = "Test Task",
            Description = "Test Description",
            Status = TaskStatus.Todo,
            CreatedBy = "user1",
            CreatedAt = createdAt
        };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new TaskStatusChangedEventHandler(_context, CreateLogger<TaskStatusChangedEventHandler>());
        var @event = new TaskStatusChanged
        {
            AggregateId = taskId,
            OldStatus = TaskStatus.Todo,
            NewStatus = TaskStatus.InProgress,
            ChangedBy = "user1",
            OccurredAt = changedAt
        };

        // Act
        await handler.HandleAsync(@event);

        // Assert
        var updatedTask = await _context.Tasks.FindAsync([taskId], TestContext.Current.CancellationToken);
        Assert.NotNull(updatedTask);
        Assert.Equal(TaskStatus.InProgress, updatedTask.Status);
        Assert.Equal("user1", updatedTask.UpdatedBy);
        Assert.Equal(changedAt, updatedTask.UpdatedAt);

        var snapshot = await _context.TaskHistories
            .FirstOrDefaultAsync(h => h.TaskId == taskId && h.SnapshotDate == changedAt.Date, TestContext.Current.CancellationToken);
        Assert.NotNull(snapshot);
        Assert.Equal(TaskStatus.InProgress, snapshot.Status);
    }

    [Fact(DisplayName = "TaskStatusChangedイベントで同日の既存スナップショットが更新されること（複数回のステータス変更）")]
    public async Task TaskStatusChangedEventHandler_Should_Update_Existing_Snapshot_On_Same_Day()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var today = new DateTime(2025, 12, 6, 10, 0, 0, DateTimeKind.Utc);

        // 既存のタスクとスナップショットを作成
        var task = new Infrastructure.Read.Entities.TaskEntity
        {
            Id = taskId,
            ProjectId = projectId,
            Title = "Test Task",
            Description = "Test Description",
            Status = TaskStatus.Todo,
            CreatedBy = "user1",
            CreatedAt = today.AddDays(-1)
        };
        _context.Tasks.Add(task);

        var existingSnapshot = new Infrastructure.Read.Entities.TaskHistoryEntity
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            ProjectId = projectId,
            SnapshotDate = today.Date,
            Title = "Test Task",
            Description = "Test Description",
            Status = TaskStatus.Todo,
            CreatedBy = "user1",
            CreatedAt = today.AddDays(-1),
            SnapshotCreatedAt = today.AddHours(-2)
        };
        _context.TaskHistories.Add(existingSnapshot);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new TaskStatusChangedEventHandler(_context, CreateLogger<TaskStatusChangedEventHandler>());
        var firstChange = new TaskStatusChanged
        {
            AggregateId = taskId,
            OldStatus = TaskStatus.Todo,
            NewStatus = TaskStatus.InProgress,
            ChangedBy = "user1",
            OccurredAt = today
        };

        // Act - First status change
        await handler.HandleAsync(firstChange);

        // Assert - First change
        var snapshotsAfterFirst = await _context.TaskHistories
            .Where(h => h.TaskId == taskId && h.SnapshotDate == today.Date)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Single(snapshotsAfterFirst);
        Assert.Equal(TaskStatus.InProgress, snapshotsAfterFirst[0].Status);

        // Act - Second status change on the same day
        var secondChange = new TaskStatusChanged
        {
            AggregateId = taskId,
            OldStatus = TaskStatus.InProgress,
            NewStatus = TaskStatus.Done,
            ChangedBy = "user1",
            OccurredAt = today.AddHours(2)
        };
        await handler.HandleAsync(secondChange);

        // Assert - Second change (should update the same snapshot)
        var snapshotsAfterSecond = await _context.TaskHistories
            .Where(h => h.TaskId == taskId && h.SnapshotDate == today.Date)
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Single(snapshotsAfterSecond);
        Assert.Equal(TaskStatus.Done, snapshotsAfterSecond[0].Status);
    }

    [Fact(DisplayName = "TaskStatusChangedイベントで存在しないタスクを適切に処理すること")]
    public async Task TaskStatusChangedEventHandler_Should_Handle_Missing_Task_Gracefully()
    {
        // Arrange
        var handler = new TaskStatusChangedEventHandler(_context, CreateLogger<TaskStatusChangedEventHandler>());
        var @event = new TaskStatusChanged
        {
            AggregateId = Guid.NewGuid(), // 存在しないタスク
            OldStatus = TaskStatus.Todo,
            NewStatus = TaskStatus.InProgress,
            ChangedBy = "user1",
            OccurredAt = DateTime.UtcNow
        };

        // Act & Assert - 例外が発生しないことを確認
        await handler.HandleAsync(@event);

        // タスクが存在しないため、何も更新されないことを確認
        var task = await _context.Tasks.FindAsync([@event.AggregateId], TestContext.Current.CancellationToken);
        Assert.Null(task);
    }

    #endregion
}

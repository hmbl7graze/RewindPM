using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RewindPM.Domain.Events;
using RewindPM.Domain.ValueObjects;
using RewindPM.Infrastructure.Read.Persistence;
using RewindPM.Infrastructure.Read.Services;
using RewindPM.Projection.Handlers;
using RewindPM.Projection.Services;
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

    private ITimeZoneService CreateTimeZoneService()
    {
        return new TestTimeZoneService();
    }

    private TaskSnapshotService CreateTaskSnapshotService()
    {
        return new TaskSnapshotService(_context, CreateTimeZoneService(), CreateLogger<TaskSnapshotService>());
    }

    /// <summary>
    /// テスト用のTimeZoneService実装（UTCを使用）
    /// </summary>
    private class TestTimeZoneService : ITimeZoneService
    {
        public TimeZoneInfo TimeZone => TimeZoneInfo.Utc;

        public DateTimeOffset GetSnapshotDate(DateTimeOffset utcDateTime)
        {
            return new DateTimeOffset(utcDateTime.Date, TimeSpan.Zero);
        }

        public DateTimeOffset ConvertUtcToLocal(DateTimeOffset utcDateTime)
        {
            return utcDateTime;
        }
    }

    #region ProjectCreatedEventHandler Tests

    [Fact(DisplayName = "ProjectCreatedイベントでプロジェクトとスナップショットが作成されること")]
    public async Task ProjectCreatedEventHandler_Should_Create_Project_And_Snapshot()
    {
        // Arrange
        var handler = new ProjectCreatedEventHandler(_context, CreateTimeZoneService(), CreateLogger<ProjectCreatedEventHandler>());
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

        var handler = new ProjectUpdatedEventHandler(_context, CreateTimeZoneService(), CreateLogger<ProjectUpdatedEventHandler>());
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

        var handler = new ProjectUpdatedEventHandler(_context, CreateTimeZoneService(), CreateLogger<ProjectUpdatedEventHandler>());
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
        var handler = new TaskCreatedEventHandler(_context, CreateTimeZoneService(), CreateLogger<TaskCreatedEventHandler>());
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

        var handler = new TaskUpdatedEventHandler(_context, CreateTaskSnapshotService(), CreateLogger<TaskUpdatedEventHandler>());
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

        var handler = new TaskUpdatedEventHandler(_context, CreateTaskSnapshotService(), CreateLogger<TaskUpdatedEventHandler>());
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

        var handler = new TaskStatusChangedEventHandler(_context, CreateTaskSnapshotService(), CreateLogger<TaskStatusChangedEventHandler>());
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

        var handler = new TaskStatusChangedEventHandler(_context, CreateTaskSnapshotService(), CreateLogger<TaskStatusChangedEventHandler>());
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
        var handler = new TaskStatusChangedEventHandler(_context, CreateTaskSnapshotService(), CreateLogger<TaskStatusChangedEventHandler>());
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

    #region TaskScheduledPeriodChangedEventHandler Tests

    [Fact(DisplayName = "TaskScheduledPeriodChangedイベントで予定期間が更新され、スナップショットが作成されること")]
    public async Task TaskScheduledPeriodChangedEventHandler_Should_Update_ScheduledPeriod_And_Create_Snapshot()
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
            ScheduledStartDate = new DateTime(2025, 12, 10),
            ScheduledEndDate = new DateTime(2025, 12, 15),
            EstimatedHours = 20,
            CreatedBy = "user1",
            CreatedAt = createdAt
        };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new TaskScheduledPeriodChangedEventHandler(_context, CreateTaskSnapshotService(), CreateLogger<TaskScheduledPeriodChangedEventHandler>());
        var newScheduledPeriod = new ScheduledPeriod(
            new DateTime(2025, 12, 12),
            new DateTime(2025, 12, 22),
            40
        );
        var @event = new TaskScheduledPeriodChanged
        {
            AggregateId = taskId,
            ScheduledPeriod = newScheduledPeriod,
            ChangedBy = "user1",
            OccurredAt = changedAt
        };

        // Act
        await handler.HandleAsync(@event);

        // Assert
        var updatedTask = await _context.Tasks.FindAsync([taskId], TestContext.Current.CancellationToken);
        Assert.NotNull(updatedTask);
        Assert.Equal(new DateTime(2025, 12, 12), updatedTask.ScheduledStartDate);
        Assert.Equal(new DateTime(2025, 12, 22), updatedTask.ScheduledEndDate);
        Assert.Equal(40, updatedTask.EstimatedHours);
        Assert.Equal("user1", updatedTask.UpdatedBy);
        Assert.Equal(changedAt, updatedTask.UpdatedAt);

        var snapshot = await _context.TaskHistories
            .FirstOrDefaultAsync(h => h.TaskId == taskId && h.SnapshotDate == changedAt.Date, TestContext.Current.CancellationToken);
        Assert.NotNull(snapshot);
        Assert.Equal(new DateTime(2025, 12, 12), snapshot.ScheduledStartDate);
        Assert.Equal(new DateTime(2025, 12, 22), snapshot.ScheduledEndDate);
        Assert.Equal(40, snapshot.EstimatedHours);
    }

    [Fact(DisplayName = "TaskScheduledPeriodChangedイベントで同日の既存スナップショットが更新されること")]
    public async Task TaskScheduledPeriodChangedEventHandler_Should_Update_Existing_Snapshot_On_Same_Day()
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
            ScheduledStartDate = new DateTime(2025, 12, 10),
            ScheduledEndDate = new DateTime(2025, 12, 15),
            EstimatedHours = 20,
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
            ScheduledStartDate = new DateTime(2025, 12, 10),
            ScheduledEndDate = new DateTime(2025, 12, 15),
            EstimatedHours = 20,
            CreatedBy = "user1",
            CreatedAt = today.AddDays(-1),
            SnapshotCreatedAt = today.AddHours(-2)
        };
        _context.TaskHistories.Add(existingSnapshot);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new TaskScheduledPeriodChangedEventHandler(_context, CreateTaskSnapshotService(), CreateLogger<TaskScheduledPeriodChangedEventHandler>());
        var newScheduledPeriod = new ScheduledPeriod(
            new DateTime(2025, 12, 12),
            new DateTime(2025, 12, 22),
            40
        );
        var @event = new TaskScheduledPeriodChanged
        {
            AggregateId = taskId,
            ScheduledPeriod = newScheduledPeriod,
            ChangedBy = "user1",
            OccurredAt = today
        };

        // Act
        await handler.HandleAsync(@event);

        // Assert
        var snapshots = await _context.TaskHistories
            .Where(h => h.TaskId == taskId && h.SnapshotDate == today.Date)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Single(snapshots);
        Assert.Equal(new DateTime(2025, 12, 12), snapshots[0].ScheduledStartDate);
        Assert.Equal(new DateTime(2025, 12, 22), snapshots[0].ScheduledEndDate);
        Assert.Equal(40, snapshots[0].EstimatedHours);
    }

    #endregion

    #region TaskActualPeriodChangedEventHandler Tests

    [Fact(DisplayName = "TaskActualPeriodChangedイベントで実績期間が更新され、スナップショットが作成されること")]
    public async Task TaskActualPeriodChangedEventHandler_Should_Update_ActualPeriod_And_Create_Snapshot()
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
            Status = TaskStatus.InProgress,
            ActualStartDate = null,
            ActualEndDate = null,
            ActualHours = null,
            CreatedBy = "user1",
            CreatedAt = createdAt
        };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new TaskActualPeriodChangedEventHandler(_context, CreateTaskSnapshotService(), CreateLogger<TaskActualPeriodChangedEventHandler>());
        var newActualPeriod = new ActualPeriod(
            new DateTime(2025, 12, 11),
            new DateTime(2025, 12, 20),
            35
        );
        var @event = new TaskActualPeriodChanged
        {
            AggregateId = taskId,
            ActualPeriod = newActualPeriod,
            ChangedBy = "user1",
            OccurredAt = changedAt
        };

        // Act
        await handler.HandleAsync(@event);

        // Assert
        var updatedTask = await _context.Tasks.FindAsync([taskId], TestContext.Current.CancellationToken);
        Assert.NotNull(updatedTask);
        Assert.Equal(new DateTime(2025, 12, 11), updatedTask.ActualStartDate);
        Assert.Equal(new DateTime(2025, 12, 20), updatedTask.ActualEndDate);
        Assert.Equal(35, updatedTask.ActualHours);
        Assert.Equal("user1", updatedTask.UpdatedBy);
        Assert.Equal(changedAt, updatedTask.UpdatedAt);

        var snapshot = await _context.TaskHistories
            .FirstOrDefaultAsync(h => h.TaskId == taskId && h.SnapshotDate == changedAt.Date, TestContext.Current.CancellationToken);
        Assert.NotNull(snapshot);
        Assert.Equal(new DateTime(2025, 12, 11), snapshot.ActualStartDate);
        Assert.Equal(new DateTime(2025, 12, 20), snapshot.ActualEndDate);
        Assert.Equal(35, snapshot.ActualHours);
    }

    [Fact(DisplayName = "TaskActualPeriodChangedイベントで同日の既存スナップショットが更新されること")]
    public async Task TaskActualPeriodChangedEventHandler_Should_Update_Existing_Snapshot_On_Same_Day()
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
            Status = TaskStatus.InProgress,
            ActualStartDate = new DateTime(2025, 12, 10),
            ActualEndDate = null,
            ActualHours = 10,
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
            Status = TaskStatus.InProgress,
            ActualStartDate = new DateTime(2025, 12, 10),
            ActualEndDate = null,
            ActualHours = 10,
            CreatedBy = "user1",
            CreatedAt = today.AddDays(-1),
            SnapshotCreatedAt = today.AddHours(-2)
        };
        _context.TaskHistories.Add(existingSnapshot);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new TaskActualPeriodChangedEventHandler(_context, CreateTaskSnapshotService(), CreateLogger<TaskActualPeriodChangedEventHandler>());
        var newActualPeriod = new ActualPeriod(
            new DateTime(2025, 12, 10),
            new DateTime(2025, 12, 15),
            25
        );
        var @event = new TaskActualPeriodChanged
        {
            AggregateId = taskId,
            ActualPeriod = newActualPeriod,
            ChangedBy = "user1",
            OccurredAt = today
        };

        // Act
        await handler.HandleAsync(@event);

        // Assert
        var snapshots = await _context.TaskHistories
            .Where(h => h.TaskId == taskId && h.SnapshotDate == today.Date)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Single(snapshots);
        Assert.Equal(new DateTime(2025, 12, 10), snapshots[0].ActualStartDate);
        Assert.Equal(new DateTime(2025, 12, 15), snapshots[0].ActualEndDate);
        Assert.Equal(25, snapshots[0].ActualHours);
    }

    [Fact(DisplayName = "TaskActualPeriodChangedイベントでnullable値を適切に処理すること")]
    public async Task TaskActualPeriodChangedEventHandler_Should_Handle_Nullable_Values()
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
            Status = TaskStatus.InProgress,
            CreatedBy = "user1",
            CreatedAt = createdAt
        };
        _context.Tasks.Add(task);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new TaskActualPeriodChangedEventHandler(_context, CreateTaskSnapshotService(), CreateLogger<TaskActualPeriodChangedEventHandler>());

        // 開始日のみ設定
        var partialActualPeriod = new ActualPeriod(
            startDate: new DateTime(2025, 12, 11),
            endDate: null,
            actualHours: null
        );
        var @event = new TaskActualPeriodChanged
        {
            AggregateId = taskId,
            ActualPeriod = partialActualPeriod,
            ChangedBy = "user1",
            OccurredAt = changedAt
        };

        // Act
        await handler.HandleAsync(@event);

        // Assert
        var updatedTask = await _context.Tasks.FindAsync([taskId], TestContext.Current.CancellationToken);
        Assert.NotNull(updatedTask);
        Assert.Equal(new DateTime(2025, 12, 11), updatedTask.ActualStartDate);
        Assert.Null(updatedTask.ActualEndDate);
        Assert.Null(updatedTask.ActualHours);
    }

    #endregion

    #region ProjectDeletedEventHandler Tests

    [Fact(DisplayName = "ProjectDeletedイベントでプロジェクトが論理削除されること")]
    public async Task ProjectDeletedEventHandler_Should_Mark_Project_As_Deleted()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var createdAt = new DateTime(2025, 12, 5, 10, 0, 0, DateTimeKind.Utc);
        var deletedAt = new DateTime(2025, 12, 6, 15, 0, 0, DateTimeKind.Utc);

        // 既存のプロジェクトを作成
        var project = new Infrastructure.Read.Entities.ProjectEntity
        {
            Id = projectId,
            Title = "Test Project",
            Description = "Test Description",
            CreatedBy = "user1",
            CreatedAt = createdAt
        };
        _context.Projects.Add(project);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var handler = new ProjectDeletedEventHandler(_context, CreateLogger<ProjectDeletedEventHandler>());
        var @event = new ProjectDeleted
        {
            AggregateId = projectId,
            DeletedBy = "user2",
            OccurredAt = deletedAt
        };

        // Act
        await handler.HandleAsync(@event);

        // Assert
        var deletedProject = await _context.Projects.FindAsync([projectId], TestContext.Current.CancellationToken);
        Assert.NotNull(deletedProject);
        Assert.True(deletedProject.IsDeleted);
        Assert.Equal(deletedAt, deletedProject.DeletedAt);
        Assert.Equal("user2", deletedProject.DeletedBy);
    }

    [Fact(DisplayName = "ProjectDeletedイベントで存在しないプロジェクトを適切に処理すること")]
    public async Task ProjectDeletedEventHandler_Should_Handle_Missing_Project_Gracefully()
    {
        // Arrange
        var handler = new ProjectDeletedEventHandler(_context, CreateLogger<ProjectDeletedEventHandler>());
        var @event = new ProjectDeleted
        {
            AggregateId = Guid.NewGuid(), // 存在しないプロジェクト
            DeletedBy = "user1",
            OccurredAt = DateTime.UtcNow
        };

        // Act & Assert - 例外が発生しないことを確認
        await handler.HandleAsync(@event);

        // プロジェクトが存在しないため、何も更新されないことを確認
        var project = await _context.Projects.FindAsync([@event.AggregateId], TestContext.Current.CancellationToken);
        Assert.Null(project);
    }

    #endregion

    #region TaskDeletedEventHandler Tests

    [Fact(DisplayName = "TaskDeletedイベントでタスクが論理削除されること")]
    public async Task TaskDeletedEventHandler_Should_Mark_Task_As_Deleted()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var createdAt = new DateTime(2025, 12, 5, 10, 0, 0, DateTimeKind.Utc);
        var deletedAt = new DateTime(2025, 12, 6, 15, 0, 0, DateTimeKind.Utc);

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

        var handler = new TaskDeletedEventHandler(_context, CreateLogger<TaskDeletedEventHandler>());
        var @event = new TaskDeleted
        {
            AggregateId = taskId,
            ProjectId = projectId,
            DeletedBy = "user2",
            OccurredAt = deletedAt
        };

        // Act
        await handler.HandleAsync(@event);

        // Assert
        var deletedTask = await _context.Tasks.FindAsync([taskId], TestContext.Current.CancellationToken);
        Assert.NotNull(deletedTask);
        Assert.True(deletedTask.IsDeleted);
        Assert.Equal(deletedAt, deletedTask.DeletedAt);
        Assert.Equal("user2", deletedTask.DeletedBy);
    }

    [Fact(DisplayName = "TaskDeletedイベントで存在しないタスクを適切に処理すること")]
    public async Task TaskDeletedEventHandler_Should_Handle_Missing_Task_Gracefully()
    {
        // Arrange
        var handler = new TaskDeletedEventHandler(_context, CreateLogger<TaskDeletedEventHandler>());
        var @event = new TaskDeleted
        {
            AggregateId = Guid.NewGuid(), // 存在しないタスク
            ProjectId = Guid.NewGuid(),
            DeletedBy = "user1",
            OccurredAt = DateTime.UtcNow
        };

        // Act & Assert - 例外が発生しないことを確認
        await handler.HandleAsync(@event);

        // タスクが存在しないため、何も更新されないことを確認
        var task = await _context.Tasks.FindAsync([@event.AggregateId], TestContext.Current.CancellationToken);
        Assert.Null(task);
    }

    #endregion

    #region TaskCompletelyUpdatedEventHandler Tests

    [Fact(DisplayName = "TaskCompletelyUpdatedイベントでタスクのすべてのプロパティが更新されること")]
    public async Task TaskCompletelyUpdatedEventHandler_Should_Update_All_Task_Properties()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var createdAt = new DateTime(2025, 12, 6, 10, 0, 0, DateTimeKind.Utc);
        var updatedAt = new DateTime(2025, 12, 7, 10, 0, 0, DateTimeKind.Utc);

        // 既存のプロジェクトとタスクを作成
        var projectHandler = new ProjectCreatedEventHandler(_context, CreateTimeZoneService(), CreateLogger<ProjectCreatedEventHandler>());
        await projectHandler.HandleAsync(new ProjectCreated
        {
            AggregateId = projectId,
            Title = "Test Project",
            Description = "Test Description",
            CreatedBy = "user1",
            OccurredAt = createdAt
        });

        var taskHandler = new TaskCreatedEventHandler(_context, CreateTimeZoneService(), CreateLogger<TaskCreatedEventHandler>());
        await taskHandler.HandleAsync(new TaskCreated
        {
            AggregateId = taskId,
            ProjectId = projectId,
            Title = "Old Title",
            Description = "Old Description",
            ScheduledPeriod = new ScheduledPeriod(
                new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2025, 1, 10, 0, 0, 0, TimeSpan.Zero),
                40),
            CreatedBy = "user1",
            OccurredAt = createdAt
        });

        var handler = new TaskCompletelyUpdatedEventHandler(_context, CreateTaskSnapshotService(), CreateLogger<TaskCompletelyUpdatedEventHandler>());
        var newScheduledPeriod = new ScheduledPeriod(
            new DateTimeOffset(2025, 2, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2025, 2, 15, 0, 0, 0, TimeSpan.Zero),
            60);
        var newActualPeriod = new ActualPeriod(
            new DateTimeOffset(2025, 2, 2, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2025, 2, 10, 0, 0, 0, TimeSpan.Zero),
            40);

        var @event = new TaskCompletelyUpdated
        {
            AggregateId = taskId,
            Title = "New Title",
            Description = "New Description",
            Status = TaskStatus.InProgress,
            ScheduledPeriod = newScheduledPeriod,
            ActualPeriod = newActualPeriod,
            UpdatedBy = "user2",
            OccurredAt = updatedAt
        };

        // Act
        await handler.HandleAsync(@event);

        // Assert
        var task = await _context.Tasks.FindAsync([taskId], TestContext.Current.CancellationToken);
        Assert.NotNull(task);
        Assert.Equal("New Title", task.Title);
        Assert.Equal("New Description", task.Description);
        Assert.Equal(TaskStatus.InProgress, task.Status);
        Assert.Equal(new DateTimeOffset(2025, 2, 1, 0, 0, 0, TimeSpan.Zero), task.ScheduledStartDate);
        Assert.Equal(new DateTimeOffset(2025, 2, 15, 0, 0, 0, TimeSpan.Zero), task.ScheduledEndDate);
        Assert.Equal(60, task.EstimatedHours);
        Assert.Equal(new DateTimeOffset(2025, 2, 2, 0, 0, 0, TimeSpan.Zero), task.ActualStartDate);
        Assert.Equal(new DateTimeOffset(2025, 2, 10, 0, 0, 0, TimeSpan.Zero), task.ActualEndDate);
        Assert.Equal(40, task.ActualHours);
        Assert.Equal(updatedAt, task.UpdatedAt);
        Assert.Equal("user2", task.UpdatedBy);
    }

    [Fact(DisplayName = "TaskCompletelyUpdatedイベントでスナップショットが作成または更新されること")]
    public async Task TaskCompletelyUpdatedEventHandler_Should_Create_Or_Update_Snapshot()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var createdAt = new DateTime(2025, 12, 6, 10, 0, 0, DateTimeKind.Utc);
        var updatedAt = new DateTime(2025, 12, 7, 10, 0, 0, DateTimeKind.Utc);

        // 既存のプロジェクトとタスクを作成
        var projectHandler = new ProjectCreatedEventHandler(_context, CreateTimeZoneService(), CreateLogger<ProjectCreatedEventHandler>());
        await projectHandler.HandleAsync(new ProjectCreated
        {
            AggregateId = projectId,
            Title = "Test Project",
            Description = "Test Description",
            CreatedBy = "user1",
            OccurredAt = createdAt
        });

        var taskHandler = new TaskCreatedEventHandler(_context, CreateTimeZoneService(), CreateLogger<TaskCreatedEventHandler>());
        await taskHandler.HandleAsync(new TaskCreated
        {
            AggregateId = taskId,
            ProjectId = projectId,
            Title = "Old Title",
            Description = "Old Description",
            ScheduledPeriod = new ScheduledPeriod(
                new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2025, 1, 10, 0, 0, 0, TimeSpan.Zero),
                40),
            CreatedBy = "user1",
            OccurredAt = createdAt
        });

        var handler = new TaskCompletelyUpdatedEventHandler(_context, CreateTaskSnapshotService(), CreateLogger<TaskCompletelyUpdatedEventHandler>());
        var @event = new TaskCompletelyUpdated
        {
            AggregateId = taskId,
            Title = "New Title",
            Description = "New Description",
            Status = TaskStatus.InProgress,
            ScheduledPeriod = new ScheduledPeriod(
                new DateTimeOffset(2025, 2, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2025, 2, 15, 0, 0, 0, TimeSpan.Zero),
                60),
            ActualPeriod = new ActualPeriod(
                new DateTimeOffset(2025, 2, 2, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2025, 2, 10, 0, 0, 0, TimeSpan.Zero),
                40),
            UpdatedBy = "user2",
            OccurredAt = updatedAt
        };

        // Act
        await handler.HandleAsync(@event);

        // Assert
        var snapshot = await _context.TaskHistories
            .FirstOrDefaultAsync(h => h.TaskId == taskId && h.SnapshotDate == updatedAt.Date, TestContext.Current.CancellationToken);
        Assert.NotNull(snapshot);
        Assert.Equal("New Title", snapshot.Title);
        Assert.Equal("New Description", snapshot.Description);
        Assert.Equal(TaskStatus.InProgress, snapshot.Status);
        Assert.Equal(new DateTimeOffset(2025, 2, 1, 0, 0, 0, TimeSpan.Zero), snapshot.ScheduledStartDate);
        Assert.Equal(new DateTimeOffset(2025, 2, 15, 0, 0, 0, TimeSpan.Zero), snapshot.ScheduledEndDate);
        Assert.Equal(60, snapshot.EstimatedHours);
        Assert.Equal(new DateTimeOffset(2025, 2, 2, 0, 0, 0, TimeSpan.Zero), snapshot.ActualStartDate);
        Assert.Equal(new DateTimeOffset(2025, 2, 10, 0, 0, 0, TimeSpan.Zero), snapshot.ActualEndDate);
        Assert.Equal(40, snapshot.ActualHours);
        Assert.Equal(updatedAt, snapshot.UpdatedAt);
        Assert.Equal("user2", snapshot.UpdatedBy);
    }

    [Fact(DisplayName = "TaskCompletelyUpdatedイベントで存在しないタスクを適切に処理すること")]
    public async Task TaskCompletelyUpdatedEventHandler_Should_Handle_Missing_Task_Gracefully()
    {
        // Arrange
        var handler = new TaskCompletelyUpdatedEventHandler(_context, CreateTaskSnapshotService(), CreateLogger<TaskCompletelyUpdatedEventHandler>());
        var @event = new TaskCompletelyUpdated
        {
            AggregateId = Guid.NewGuid(), // 存在しないタスク
            Title = "New Title",
            Description = "New Description",
            Status = TaskStatus.InProgress,
            ScheduledPeriod = new ScheduledPeriod(
                new DateTimeOffset(2025, 2, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2025, 2, 15, 0, 0, 0, TimeSpan.Zero),
                60),
            ActualPeriod = new ActualPeriod(),
            UpdatedBy = "user1",
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

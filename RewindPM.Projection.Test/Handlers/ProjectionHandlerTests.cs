using Microsoft.Extensions.Logging;
using NSubstitute;
using RewindPM.Domain.Events;
using RewindPM.Domain.ValueObjects;
using RewindPM.Infrastructure.Read.Entities;
using RewindPM.Infrastructure.Read.Services;
using RewindPM.Projection.Handlers;
using RewindPM.Projection.Services;
using TaskStatus = RewindPM.Domain.ValueObjects.TaskStatus;

namespace RewindPM.Projection.Test.Handlers;

/// <summary>
/// プロジェクションハンドラーの単体テスト（DB抽象化版）
/// IReadModelContextインターフェースを使用
/// </summary>
public class ProjectionHandlerTests
{
    private readonly IReadModelContext _context;
    private readonly ITimeZoneService _timeZoneService;

    public ProjectionHandlerTests()
    {
        _context = Substitute.For<IReadModelContext>();
        _timeZoneService = new TestTimeZoneService();
    }

    private ILogger<T> CreateLogger<T>()
    {
        return Substitute.For<ILogger<T>>();
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
        var handler = new ProjectCreatedEventHandler(_context, _timeZoneService, CreateLogger<ProjectCreatedEventHandler>());
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
        _context.Received(1).AddProject(Arg.Is<ProjectEntity>(p =>
            p.Id == projectId &&
            p.Title == "Test Project" &&
            p.Description == "Test Description" &&
            p.CreatedBy == "user1" &&
            p.CreatedAt == occurredAt &&
            p.UpdatedAt == null &&
            p.UpdatedBy == null));

        _context.Received(1).AddProjectHistory(Arg.Is<ProjectHistoryEntity>(h =>
            h.ProjectId == projectId &&
            h.SnapshotDate == occurredAt.Date &&
            h.Title == "Test Project" &&
            h.Description == "Test Description" &&
            h.CreatedBy == "user1"));

        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
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

        // 既存のプロジェクトを返すようにモックを設定
        var existingProject = new ProjectEntity
        {
            Id = projectId,
            Title = "Old Title",
            Description = "Old Description",
            CreatedBy = "user1",
            CreatedAt = createdAt
        };

        var projects = new List<ProjectEntity> { existingProject }.AsQueryable();
        _context.Projects.Returns(projects);

        // 既存スナップショットなし
        var emptyHistories = new List<ProjectHistoryEntity>().AsQueryable();
        _context.ProjectHistories.Returns(emptyHistories);

        var handler = new ProjectUpdatedEventHandler(_context, _timeZoneService, CreateLogger<ProjectUpdatedEventHandler>());
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
        Assert.Equal("New Title", existingProject.Title);
        Assert.Equal("New Description", existingProject.Description);
        Assert.Equal("user2", existingProject.UpdatedBy);
        Assert.Equal(updatedAt, existingProject.UpdatedAt);

        _context.Received(1).AddProjectHistory(Arg.Is<ProjectHistoryEntity>(h =>
            h.ProjectId == projectId &&
            h.Title == "New Title" &&
            h.Description == "New Description"));

        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region TaskCreatedEventHandler Tests

    [Fact(DisplayName = "TaskCreatedイベントでタスクとスナップショットが作成されること")]
    public async Task TaskCreatedEventHandler_Should_Create_Task_And_Snapshot()
    {
        // Arrange
        var handler = new TaskCreatedEventHandler(_context, _timeZoneService, CreateLogger<TaskCreatedEventHandler>());
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
        _context.Received(1).AddTask(Arg.Is<TaskEntity>(t =>
            t.Id == taskId &&
            t.ProjectId == projectId &&
            t.Title == "Test Task" &&
            t.Description == "Test Task Description" &&
            t.Status == TaskStatus.Todo &&
            t.ScheduledStartDate == new DateTime(2025, 12, 10) &&
            t.ScheduledEndDate == new DateTime(2025, 12, 20) &&
            t.EstimatedHours == 40 &&
            t.CreatedBy == "user1" &&
            t.ActualStartDate == null &&
            t.ActualEndDate == null &&
            t.ActualHours == null));

        _context.Received(1).AddTaskHistory(Arg.Is<TaskHistoryEntity>(h =>
            h.TaskId == taskId &&
            h.SnapshotDate == occurredAt.Date &&
            h.Title == "Test Task" &&
            h.Status == TaskStatus.Todo));

        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
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

        // 既存のタスクを返すようにモックを設定
        var existingTask = new TaskEntity
        {
            Id = taskId,
            ProjectId = projectId,
            Title = "Old Task Title",
            Description = "Old Task Description",
            Status = TaskStatus.Todo,
            CreatedBy = "user1",
            CreatedAt = createdAt
        };

        var tasks = new List<TaskEntity> { existingTask }.AsQueryable();
        _context.Tasks.Returns(tasks);

        // 既存スナップショットなし
        var emptyHistories = new List<TaskHistoryEntity>().AsQueryable();
        _context.TaskHistories.Returns(emptyHistories);

        var taskSnapshotService = new TaskSnapshotService(_context, _timeZoneService, CreateLogger<TaskSnapshotService>());
        var handler = new TaskUpdatedEventHandler(_context, taskSnapshotService, CreateLogger<TaskUpdatedEventHandler>());
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
        Assert.Equal("New Task Title", existingTask.Title);
        Assert.Equal("New Task Description", existingTask.Description);
        Assert.Equal("user2", existingTask.UpdatedBy);
        Assert.Equal(updatedAt, existingTask.UpdatedAt);

        _context.Received(1).AddTaskHistory(Arg.Is<TaskHistoryEntity>(h =>
            h.TaskId == taskId &&
            h.Title == "New Task Title" &&
            h.Description == "New Task Description"));

        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
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

        // 既存のタスクを返すようにモックを設定
        var existingTask = new TaskEntity
        {
            Id = taskId,
            ProjectId = projectId,
            Title = "Test Task",
            Description = "Test Description",
            Status = TaskStatus.Todo,
            CreatedBy = "user1",
            CreatedAt = createdAt
        };

        var tasks = new List<TaskEntity> { existingTask }.AsQueryable();
        _context.Tasks.Returns(tasks);

        // 既存スナップショットなし
        var emptyHistories = new List<TaskHistoryEntity>().AsQueryable();
        _context.TaskHistories.Returns(emptyHistories);

        var taskSnapshotService = new TaskSnapshotService(_context, _timeZoneService, CreateLogger<TaskSnapshotService>());
        var handler = new TaskStatusChangedEventHandler(_context, taskSnapshotService, CreateLogger<TaskStatusChangedEventHandler>());
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
        Assert.Equal(TaskStatus.InProgress, existingTask.Status);
        Assert.Equal("user1", existingTask.UpdatedBy);
        Assert.Equal(changedAt, existingTask.UpdatedAt);

        _context.Received(1).AddTaskHistory(Arg.Is<TaskHistoryEntity>(h =>
            h.TaskId == taskId &&
            h.Status == TaskStatus.InProgress));

        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "TaskStatusChangedイベントで存在しないタスクを適切に処理すること")]
    public async Task TaskStatusChangedEventHandler_Should_Handle_Missing_Task_Gracefully()
    {
        // Arrange
        var emptyTasks = new List<TaskEntity>().AsQueryable();
        _context.Tasks.Returns(emptyTasks);

        var taskSnapshotService = new TaskSnapshotService(_context, _timeZoneService, CreateLogger<TaskSnapshotService>());
        var handler = new TaskStatusChangedEventHandler(_context, taskSnapshotService, CreateLogger<TaskStatusChangedEventHandler>());
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

        // SaveChangesAsyncが呼ばれないことを確認（タスクが存在しないため）
        await _context.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
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

        // 既存のプロジェクトを返すようにモックを設定
        var existingProject = new ProjectEntity
        {
            Id = projectId,
            Title = "Test Project",
            Description = "Test Description",
            CreatedBy = "user1",
            CreatedAt = createdAt
        };

        var projects = new List<ProjectEntity> { existingProject }.AsQueryable();
        _context.Projects.Returns(projects);

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
        Assert.True(existingProject.IsDeleted);
        Assert.Equal(deletedAt, existingProject.DeletedAt);
        Assert.Equal("user2", existingProject.DeletedBy);

        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
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

        // 既存のタスクを返すようにモックを設定
        var existingTask = new TaskEntity
        {
            Id = taskId,
            ProjectId = projectId,
            Title = "Test Task",
            Description = "Test Description",
            Status = TaskStatus.Todo,
            CreatedBy = "user1",
            CreatedAt = createdAt
        };

        var tasks = new List<TaskEntity> { existingTask }.AsQueryable();
        _context.Tasks.Returns(tasks);

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
        Assert.True(existingTask.IsDeleted);
        Assert.Equal(deletedAt, existingTask.DeletedAt);
        Assert.Equal("user2", existingTask.DeletedBy);

        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    #endregion
}

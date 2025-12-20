using Microsoft.Extensions.Logging;
using NSubstitute;
using RewindPM.Infrastructure.Read.Entities;
using RewindPM.Infrastructure.Read.Services;
using RewindPM.Projection.Services;
using TaskStatus = RewindPM.Domain.ValueObjects.TaskStatus;

namespace RewindPM.Projection.Test.Services;

/// <summary>
/// TaskSnapshotServiceのテスト（DB抽象化版）
/// IReadModelContextインターフェースを使用
/// </summary>
public class TaskSnapshotServiceTest
{
    private readonly IReadModelContext _context;
    private readonly ITimeZoneService _timeZoneService;
    private readonly ILogger<TaskSnapshotService> _logger;
    private readonly TaskSnapshotService _service;

    public TaskSnapshotServiceTest()
    {
        _context = Substitute.For<IReadModelContext>();
        _timeZoneService = new TestTimeZoneService();
        _logger = Substitute.For<ILogger<TaskSnapshotService>>();
        _service = new TaskSnapshotService(_context, _timeZoneService, _logger);
    }

    [Fact(DisplayName = "PrepareTaskSnapshotAsync_Should_Create_New_Snapshot")]
    public async Task PrepareTaskSnapshotAsync_Should_Create_New_Snapshot()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var task = new TaskEntity
        {
            Id = taskId,
            ProjectId = projectId,
            Title = "テストタスク",
            Description = "説明",
            Status = TaskStatus.InProgress,
            EstimatedHours = 8,
            ActualHours = 4,
            ScheduledStartDate = new DateTimeOffset(2025, 12, 1, 0, 0, 0, TimeSpan.Zero),
            ScheduledEndDate = new DateTimeOffset(2025, 12, 31, 0, 0, 0, TimeSpan.Zero),
            ActualStartDate = new DateTimeOffset(2025, 12, 10, 0, 0, 0, TimeSpan.Zero),
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "test-user",
            UpdatedBy = "test-user"
        };

        var occurredAt = new DateTimeOffset(2025, 12, 15, 10, 0, 0, TimeSpan.Zero);

        // 既存スナップショットなし
        var emptyHistories = new List<TaskHistoryEntity>().AsQueryable();
        _context.TaskHistories.Returns(emptyHistories);

        // Act
        await _service.PrepareTaskSnapshotAsync(taskId, task, occurredAt);

        // Assert
        _context.Received(1).AddTaskHistory(Arg.Is<TaskHistoryEntity>(h =>
            h.TaskId == taskId &&
            h.ProjectId == projectId &&
            h.Title == "テストタスク" &&
            h.Description == "説明" &&
            h.Status == TaskStatus.InProgress &&
            h.EstimatedHours == 8 &&
            h.ActualHours == 4));
    }

    [Fact(DisplayName = "PrepareTaskSnapshotAsync_Should_Update_Existing_Snapshot_On_Same_Date")]
    public async Task PrepareTaskSnapshotAsync_Should_Update_Existing_Snapshot_On_Same_Date()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var snapshotDate = new DateTimeOffset(2025, 12, 15, 0, 0, 0, TimeSpan.Zero);

        // 既存のスナップショット
        var existingSnapshot = new TaskHistoryEntity
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            ProjectId = projectId,
            SnapshotDate = snapshotDate,
            Title = "旧タイトル",
            Description = "旧説明",
            Status = TaskStatus.Todo,
            EstimatedHours = 10,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "test-user",
            UpdatedBy = "test-user",
            SnapshotCreatedAt = DateTimeOffset.UtcNow
        };

        var histories = new List<TaskHistoryEntity> { existingSnapshot }.AsQueryable();
        _context.TaskHistories.Returns(histories);

        var task = new TaskEntity
        {
            Id = taskId,
            ProjectId = projectId,
            Title = "新タイトル",
            Description = "新説明",
            Status = TaskStatus.InProgress,
            EstimatedHours = 8,
            ActualHours = 4,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "test-user",
            UpdatedBy = "test-user"
        };

        var occurredAt = new DateTimeOffset(2025, 12, 15, 14, 0, 0, TimeSpan.Zero); // 同じ日

        // Act
        await _service.PrepareTaskSnapshotAsync(taskId, task, occurredAt);

        // Assert
        // 既存のスナップショットが更新されたことを確認
        Assert.Equal("新タイトル", existingSnapshot.Title);
        Assert.Equal("新説明", existingSnapshot.Description);
        Assert.Equal(TaskStatus.InProgress, existingSnapshot.Status);
        Assert.Equal(8, existingSnapshot.EstimatedHours);
        Assert.Equal(4, existingSnapshot.ActualHours);

        // 新しいスナップショットは追加されない
        _context.DidNotReceive().AddTaskHistory(Arg.Any<TaskHistoryEntity>());
    }

    [Fact(DisplayName = "PrepareTaskSnapshotAsync_Should_Handle_Null_Values")]
    public async Task PrepareTaskSnapshotAsync_Should_Handle_Null_Values()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var task = new TaskEntity
        {
            Id = taskId,
            ProjectId = projectId,
            Title = "タスク",
            Description = "説明",
            Status = TaskStatus.Todo,
            EstimatedHours = null, // null
            ActualHours = null, // null
            ScheduledStartDate = null, // null
            ScheduledEndDate = null, // null
            ActualStartDate = null, // null
            ActualEndDate = null, // null
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "test-user",
            UpdatedBy = "test-user"
        };

        var occurredAt = new DateTimeOffset(2025, 12, 15, 10, 0, 0, TimeSpan.Zero);

        // 既存スナップショットなし
        var emptyHistories = new List<TaskHistoryEntity>().AsQueryable();
        _context.TaskHistories.Returns(emptyHistories);

        // Act
        await _service.PrepareTaskSnapshotAsync(taskId, task, occurredAt);

        // Assert
        _context.Received(1).AddTaskHistory(Arg.Is<TaskHistoryEntity>(h =>
            h.TaskId == taskId &&
            h.EstimatedHours == null &&
            h.ActualHours == null &&
            h.ScheduledStartDate == null &&
            h.ScheduledEndDate == null &&
            h.ActualStartDate == null &&
            h.ActualEndDate == null));
    }

    [Fact(DisplayName = "PrepareTaskSnapshotAsync_Should_Throw_When_CurrentState_Is_Null")]
    public async Task PrepareTaskSnapshotAsync_Should_Throw_When_CurrentState_Is_Null()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var occurredAt = DateTimeOffset.UtcNow;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _service.PrepareTaskSnapshotAsync(taskId, null!, occurredAt));
    }

    /// <summary>
    /// テスト用のTimeZoneService実装(UTCを使用)
    /// </summary>
    private class TestTimeZoneService : ITimeZoneService
    {
        public TimeZoneInfo TimeZone => TimeZoneInfo.Utc;

        public DateTimeOffset ConvertUtcToLocal(DateTimeOffset utcDateTime)
        {
            return utcDateTime;
        }

        public DateTimeOffset GetSnapshotDate(DateTimeOffset occurredAt)
        {
            return new DateTimeOffset(occurredAt.UtcDateTime.Date, TimeSpan.Zero);
        }
    }
}

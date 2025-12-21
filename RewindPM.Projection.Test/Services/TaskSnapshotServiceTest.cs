using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RewindPM.Infrastructure.Read.Entities;
using RewindPM.Infrastructure.Read.Services;
using RewindPM.Infrastructure.Read.SQLite.Persistence;
using RewindPM.Projection.Services;
using TaskStatus = RewindPM.Domain.ValueObjects.TaskStatus;

namespace RewindPM.Projection.Test.Services;

/// <summary>
/// TaskSnapshotServiceのテスト（InMemoryデータベース使用）
/// </summary>
public class TaskSnapshotServiceTest : IDisposable
{
    private readonly ReadModelDbContext _context;
    private readonly ITimeZoneService _timeZoneService;
    private readonly ILogger<TaskSnapshotService> _logger;
    private readonly TaskSnapshotService _service;

    public TaskSnapshotServiceTest()
    {
        // InMemoryデータベースを使用
        var options = new DbContextOptionsBuilder<ReadModelDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ReadModelDbContext(options);
        _timeZoneService = new TestTimeZoneService();
        _logger = Substitute.For<ILogger<TaskSnapshotService>>();
        _service = new TaskSnapshotService(_context, _timeZoneService, _logger);
    }

    public void Dispose()
    {
        _context?.Dispose();
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

        // Act
        await _service.PrepareTaskSnapshotAsync(taskId, task, occurredAt);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var snapshot = await _context.TaskHistories
            .FirstOrDefaultAsync(h => h.TaskId == taskId, TestContext.Current.CancellationToken);

        Assert.NotNull(snapshot);
        Assert.Equal(taskId, snapshot.TaskId);
        Assert.Equal(projectId, snapshot.ProjectId);
        Assert.Equal("テストタスク", snapshot.Title);
        Assert.Equal("説明", snapshot.Description);
        Assert.Equal(TaskStatus.InProgress, snapshot.Status);
        Assert.Equal(8, snapshot.EstimatedHours);
        Assert.Equal(4, snapshot.ActualHours);
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

        // 既存スナップショットをDBに追加
        _context.TaskHistories.Add(existingSnapshot);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

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
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert - DBから再取得して確認
        var updatedSnapshot = await _context.TaskHistories
            .FirstOrDefaultAsync(h => h.TaskId == taskId, TestContext.Current.CancellationToken);

        Assert.NotNull(updatedSnapshot);
        Assert.Equal("新タイトル", updatedSnapshot.Title);
        Assert.Equal("新説明", updatedSnapshot.Description);
        Assert.Equal(TaskStatus.InProgress, updatedSnapshot.Status);
        Assert.Equal(8, updatedSnapshot.EstimatedHours);
        Assert.Equal(4, updatedSnapshot.ActualHours);

        // スナップショットは1つのみ
        var snapshotCount = await _context.TaskHistories.CountAsync(h => h.TaskId == taskId, TestContext.Current.CancellationToken);
        Assert.Equal(1, snapshotCount);
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

        // Act
        await _service.PrepareTaskSnapshotAsync(taskId, task, occurredAt);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var snapshot = await _context.TaskHistories
            .FirstOrDefaultAsync(h => h.TaskId == taskId, TestContext.Current.CancellationToken);

        Assert.NotNull(snapshot);
        Assert.Equal(taskId, snapshot.TaskId);
        Assert.Null(snapshot.EstimatedHours);
        Assert.Null(snapshot.ActualHours);
        Assert.Null(snapshot.ScheduledStartDate);
        Assert.Null(snapshot.ScheduledEndDate);
        Assert.Null(snapshot.ActualStartDate);
        Assert.Null(snapshot.ActualEndDate);
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

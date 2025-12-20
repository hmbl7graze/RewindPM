using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RewindPM.Infrastructure.Read.Entities;
using RewindPM.Infrastructure.Read.Persistence;
using RewindPM.Infrastructure.Read.Services;
using RewindPM.Projection.Services;
using TaskStatus = RewindPM.Domain.ValueObjects.TaskStatus;

namespace RewindPM.Projection.Test.Services;

/// <summary>
/// TaskSnapshotServiceのテスト
/// </summary>
public class TaskSnapshotServiceTest : IDisposable
{
    private readonly ReadModelDbContext _context;
    private readonly ILogger<TaskSnapshotService> _logger;
    private readonly TaskSnapshotService _service;

    public TaskSnapshotServiceTest()
    {
        var options = new DbContextOptionsBuilder<ReadModelDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ReadModelDbContext(options);
        _logger = new LoggerFactory().CreateLogger<TaskSnapshotService>();
        var timeZoneService = new TestTimeZoneService();
        _service = new TaskSnapshotService(_context, timeZoneService, _logger);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
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

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

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

    [Fact]
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

        _context.TaskHistories.Add(existingSnapshot);

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

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var occurredAt = new DateTimeOffset(2025, 12, 15, 14, 0, 0, TimeSpan.Zero); // 同じ日

        // Act
        await _service.PrepareTaskSnapshotAsync(taskId, task, occurredAt);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var snapshots = await _context.TaskHistories
            .Where(h => h.TaskId == taskId)
            .ToListAsync(TestContext.Current.CancellationToken);

        // 1つだけ存在することを確認（新規作成されず、更新されたこと）
        Assert.Single(snapshots);

        var updatedSnapshot = snapshots[0];
        Assert.Equal("新タイトル", updatedSnapshot.Title);
        Assert.Equal("新説明", updatedSnapshot.Description);
        Assert.Equal(TaskStatus.InProgress, updatedSnapshot.Status);
        Assert.Equal(8, updatedSnapshot.EstimatedHours);
        Assert.Equal(4, updatedSnapshot.ActualHours);
    }

    [Fact]
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

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var occurredAt = new DateTimeOffset(2025, 12, 15, 10, 0, 0, TimeSpan.Zero);

        // Act
        await _service.PrepareTaskSnapshotAsync(taskId, task, occurredAt);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var snapshot = await _context.TaskHistories
            .FirstOrDefaultAsync(h => h.TaskId == taskId, TestContext.Current.CancellationToken);

        Assert.NotNull(snapshot);
        Assert.Null(snapshot.EstimatedHours);
        Assert.Null(snapshot.ActualHours);
        Assert.Null(snapshot.ScheduledStartDate);
        Assert.Null(snapshot.ScheduledEndDate);
        Assert.Null(snapshot.ActualStartDate);
        Assert.Null(snapshot.ActualEndDate);
    }

    [Fact]
    public async Task PrepareTaskSnapshotAsync_Should_Convert_TimeZone_Correctly()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var task = new TaskEntity
        {
            Id = taskId,
            ProjectId = projectId,
            Title = "タイムゾーンテスト",
            Description = "説明",
            Status = TaskStatus.Todo,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "test-user",
            UpdatedBy = "test-user"
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // JST 2025-12-15 23:30 (UTC 2025-12-15 14:30)
        var occurredAtJst = new DateTimeOffset(2025, 12, 15, 23, 30, 0, TimeSpan.FromHours(9));

        // Act
        await _service.PrepareTaskSnapshotAsync(taskId, task, occurredAtJst);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var snapshot = await _context.TaskHistories
            .FirstOrDefaultAsync(h => h.TaskId == taskId, TestContext.Current.CancellationToken);

        Assert.NotNull(snapshot);
        // TestTimeZoneServiceはUTCをそのまま返すので、日付は2025-12-15になる
        Assert.Equal(new DateTimeOffset(2025, 12, 15, 14, 30, 0, TimeSpan.Zero).Date, snapshot.SnapshotDate.Date);
    }

    [Fact]
    public async Task PrepareTaskSnapshotAsync_Should_Throw_When_CurrentState_Is_Null()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var occurredAt = DateTimeOffset.UtcNow;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _service.PrepareTaskSnapshotAsync(taskId, null!, occurredAt));
    }

    [Fact]
    public async Task PrepareTaskSnapshotAsync_Should_Create_New_Snapshot_On_Different_Date()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        // 初日のスナップショット
        var firstSnapshot = new TaskHistoryEntity
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            ProjectId = projectId,
            SnapshotDate = new DateTimeOffset(2025, 12, 15, 0, 0, 0, TimeSpan.Zero),
            Title = "初日のタイトル",
            Description = "説明",
            Status = TaskStatus.Todo,
            EstimatedHours = 10,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "test-user",
            UpdatedBy = "test-user",
            SnapshotCreatedAt = DateTimeOffset.UtcNow
        };

        _context.TaskHistories.Add(firstSnapshot);

        var task = new TaskEntity
        {
            Id = taskId,
            ProjectId = projectId,
            Title = "2日目のタイトル",
            Description = "新しい説明",
            Status = TaskStatus.InProgress,
            EstimatedHours = 8,
            ActualHours = 4,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "test-user",
            UpdatedBy = "test-user"
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // 翌日のイベント
        var occurredAt = new DateTimeOffset(2025, 12, 16, 10, 0, 0, TimeSpan.Zero);

        // Act
        await _service.PrepareTaskSnapshotAsync(taskId, task, occurredAt);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        var snapshots = await _context.TaskHistories
            .Where(h => h.TaskId == taskId)
            .OrderBy(h => h.SnapshotDate)
            .ToListAsync(TestContext.Current.CancellationToken);

        // 2つのスナップショットが存在することを確認
        Assert.Equal(2, snapshots.Count);

        // 初日のスナップショットは変更されていないことを確認
        Assert.Equal("初日のタイトル", snapshots[0].Title);
        Assert.Equal(TaskStatus.Todo, snapshots[0].Status);
        Assert.Equal(new DateTimeOffset(2025, 12, 15, 0, 0, 0, TimeSpan.Zero).Date, snapshots[0].SnapshotDate.Date);

        // 2日目のスナップショットが新規作成されたことを確認
        Assert.Equal("2日目のタイトル", snapshots[1].Title);
        Assert.Equal(TaskStatus.InProgress, snapshots[1].Status);
        Assert.Equal(8, snapshots[1].EstimatedHours);
        Assert.Equal(4, snapshots[1].ActualHours);
        Assert.Equal(new DateTimeOffset(2025, 12, 16, 0, 0, 0, TimeSpan.Zero).Date, snapshots[1].SnapshotDate.Date);
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

        public DateTimeOffset ConvertToLocalDate(DateTimeOffset utcDateTime)
        {
            return new DateTimeOffset(utcDateTime.UtcDateTime.Date, TimeSpan.Zero);
        }
    }
}

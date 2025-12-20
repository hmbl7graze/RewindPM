using Microsoft.EntityFrameworkCore;
using RewindPM.Domain.Common;
using RewindPM.Domain.Events;
using RewindPM.Domain.ValueObjects;
using RewindPM.Infrastructure.Write.SQLite.EventStore;
using RewindPM.Infrastructure.Write.SQLite.Persistence;
using RewindPM.Infrastructure.Write.Serialization;
using TaskStatus = RewindPM.Domain.ValueObjects.TaskStatus;

namespace RewindPM.Infrastructure.Write.Test.EventStore;

/// <summary>
/// SqliteEventStoreのテスト
/// インメモリSQLiteデータベースを使用してイベントストアの動作を検証する
/// </summary>
public class SqliteEventStoreTests : IDisposable
{
    private readonly EventStoreDbContext _context;
    private readonly SqliteEventStore _eventStore;
    private readonly DomainEventSerializer _serializer;

    public SqliteEventStoreTests()
    {
        // インメモリSQLiteデータベースを使用
        var options = new DbContextOptionsBuilder<EventStoreDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        _context = new EventStoreDbContext(options);
        _context.Database.OpenConnection();
        _context.Database.EnsureCreated();

        _serializer = new DomainEventSerializer();
        _eventStore = new SqliteEventStore(_context, _serializer);
    }

    public void Dispose()
    {
        _context.Database.CloseConnection();
        _context.Dispose();
    }

    [Fact(DisplayName = "イベントを保存して取得できる")]
    public async Task SaveAndGetEvents_SavesAndRetrievesEvents()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var events = new List<IDomainEvent>
        {
            new TaskCreated
            {
                AggregateId = aggregateId,
                ProjectId = projectId,
                Title = "Test Task",
                Description = "Description",
                ScheduledPeriod = new ScheduledPeriod(now, now.AddDays(7), 40),
                CreatedBy = "user1"
            }
        };

        // Act
        await _eventStore.SaveEventsAsync(aggregateId, events, -1);
        var retrievedEvents = await _eventStore.GetEventsAsync(aggregateId);

        // Assert
        Assert.Single(retrievedEvents);
        var taskCreated = Assert.IsType<TaskCreated>(retrievedEvents[0]);
        Assert.Equal(aggregateId, taskCreated.AggregateId);
        Assert.Equal("Test Task", taskCreated.Title);
    }

    [Fact(DisplayName = "複数のイベントを順番に保存して取得できる")]
    public async Task SaveMultipleEvents_SavesAndRetrievesInOrder()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var event1 = new TaskCreated
        {
            AggregateId = aggregateId,
            ProjectId = projectId,
            Title = "Test Task",
            Description = "Description",
            ScheduledPeriod = new ScheduledPeriod(now, now.AddDays(7), 40),
            CreatedBy = "user1"
        };

        var event2 = new TaskStatusChanged
        {
            AggregateId = aggregateId,
            OldStatus = TaskStatus.Todo,
            NewStatus = TaskStatus.InProgress,
            ChangedBy = "user1"
        };

        // Act
        await _eventStore.SaveEventsAsync(aggregateId, new[] { event1 }, -1);
        await _eventStore.SaveEventsAsync(aggregateId, new[] { event2 }, 0);
        var retrievedEvents = await _eventStore.GetEventsAsync(aggregateId);

        // Assert
        Assert.Equal(2, retrievedEvents.Count);
        Assert.IsType<TaskCreated>(retrievedEvents[0]);
        Assert.IsType<TaskStatusChanged>(retrievedEvents[1]);
    }

    [Fact(DisplayName = "楽観的同時実行制御が正しく動作する")]
    public async Task SaveEvents_WithWrongVersion_ThrowsConcurrencyException()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var event1 = new TaskCreated
        {
            AggregateId = aggregateId,
            ProjectId = projectId,
            Title = "Test Task",
            Description = "Description",
            ScheduledPeriod = new ScheduledPeriod(now, now.AddDays(7), 40),
            CreatedBy = "user1"
        };

        var event2 = new TaskStatusChanged
        {
            AggregateId = aggregateId,
            OldStatus = TaskStatus.Todo,
            NewStatus = TaskStatus.InProgress,
            ChangedBy = "user1"
        };

        // Act
        await _eventStore.SaveEventsAsync(aggregateId, new[] { event1 }, -1);

        // Assert - 間違ったバージョンで保存しようとすると例外がスローされる
        var exception = await Assert.ThrowsAsync<ConcurrencyException>(
            async () => await _eventStore.SaveEventsAsync(aggregateId, new[] { event2 }, -1));

        Assert.Equal(aggregateId, exception.AggregateId);
        Assert.Equal(-1, exception.ExpectedVersion);
        Assert.Equal(0, exception.ActualVersion);
    }

    [Fact(DisplayName = "指定時点までのイベントを取得できる（タイムトラベル）")]
    public async Task GetEventsUntil_ReturnsEventsBeforeTime()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var time1 = DateTimeOffset.UtcNow;

        var event1 = new TaskCreated
        {
            AggregateId = aggregateId,
            ProjectId = projectId,
            Title = "Test Task",
            Description = "Description",
            ScheduledPeriod = new ScheduledPeriod(time1, time1.AddDays(7), 40),
            CreatedBy = "user1",
            OccurredAt = time1
        };

        await _eventStore.SaveEventsAsync(aggregateId, new[] { event1 }, -1);

        // 少し待ってから2つ目のイベントを作成
        await Task.Delay(100, TestContext.Current.CancellationToken);
        var time2 = DateTimeOffset.UtcNow;
        var cutoffTime = time2;

        await Task.Delay(100, TestContext.Current.CancellationToken);
        var time3 = DateTimeOffset.UtcNow;

        var event2 = new TaskStatusChanged
        {
            AggregateId = aggregateId,
            OldStatus = TaskStatus.Todo,
            NewStatus = TaskStatus.InProgress,
            ChangedBy = "user1",
            OccurredAt = time3
        };

        await _eventStore.SaveEventsAsync(aggregateId, new[] { event2 }, 0);

        // Act - cutoffTimeまでのイベントを取得
        var events = await _eventStore.GetEventsUntilAsync(aggregateId, cutoffTime);

        // Assert - event1のみが取得される
        Assert.Single(events);
        Assert.IsType<TaskCreated>(events[0]);
    }

    [Fact(DisplayName = "イベント種別で検索できる")]
    public async Task GetEventsByType_ReturnsMatchingEvents()
    {
        // Arrange
        var aggregateId1 = Guid.NewGuid();
        var aggregateId2 = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var event1 = new TaskCreated
        {
            AggregateId = aggregateId1,
            ProjectId = projectId,
            Title = "Task 1",
            Description = "Description",
            ScheduledPeriod = new ScheduledPeriod(now, now.AddDays(7), 40),
            CreatedBy = "user1"
        };

        var event2 = new TaskCreated
        {
            AggregateId = aggregateId2,
            ProjectId = projectId,
            Title = "Task 2",
            Description = "Description",
            ScheduledPeriod = new ScheduledPeriod(now, now.AddDays(7), 40),
            CreatedBy = "user1"
        };

        var event3 = new TaskStatusChanged
        {
            AggregateId = aggregateId1,
            OldStatus = TaskStatus.Todo,
            NewStatus = TaskStatus.InProgress,
            ChangedBy = "user1"
        };

        await _eventStore.SaveEventsAsync(aggregateId1, new[] { event1 }, -1);
        await _eventStore.SaveEventsAsync(aggregateId2, new[] { event2 }, -1);
        await _eventStore.SaveEventsAsync(aggregateId1, new[] { event3 }, 0);

        // Act - TaskCreatedイベントのみを取得
        var events = await _eventStore.GetEventsByTypeAsync("TaskCreated");

        // Assert
        Assert.Equal(2, events.Count);
        Assert.All(events, e => Assert.IsType<TaskCreated>(e));
    }

    [Fact(DisplayName = "イベント種別と期間で検索できる")]
    public async Task GetEventsByTypeAndTimeRange_ReturnsMatchingEvents()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var time1 = DateTimeOffset.UtcNow.AddHours(-2);
        var time2 = DateTimeOffset.UtcNow.AddHours(-1);
        var time3 = DateTimeOffset.UtcNow;

        var event1 = new TaskCreated
        {
            AggregateId = aggregateId,
            ProjectId = projectId,
            Title = "Task 1",
            Description = "Description",
            ScheduledPeriod = new ScheduledPeriod(time1, time1.AddDays(7), 40),
            CreatedBy = "user1",
            OccurredAt = time1
        };

        await _eventStore.SaveEventsAsync(aggregateId, new[] { event1 }, -1);

        var event2 = new TaskStatusChanged
        {
            AggregateId = aggregateId,
            OldStatus = TaskStatus.Todo,
            NewStatus = TaskStatus.InProgress,
            ChangedBy = "user1",
            OccurredAt = time2
        };

        await _eventStore.SaveEventsAsync(aggregateId, new[] { event2 }, 0);

        // Act - time1からtime2までのTaskStatusChangedイベントを取得
        var events = await _eventStore.GetEventsByTypeAsync("TaskStatusChanged", time1, time3);

        // Assert
        Assert.Single(events);
        Assert.IsType<TaskStatusChanged>(events[0]);
    }

    [Fact(DisplayName = "空のイベントリストを保存しても例外が発生しない")]
    public async Task SaveEvents_WithEmptyList_DoesNotThrow()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var events = new List<IDomainEvent>();

        // Act & Assert
        await _eventStore.SaveEventsAsync(aggregateId, events, -1);

        var retrievedEvents = await _eventStore.GetEventsAsync(aggregateId);
        Assert.Empty(retrievedEvents);
    }

    [Fact(DisplayName = "存在しないAggregateのイベントを取得すると空のリストが返される")]
    public async Task GetEvents_ForNonExistentAggregate_ReturnsEmptyList()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();

        // Act
        var events = await _eventStore.GetEventsAsync(aggregateId);

        // Assert
        Assert.Empty(events);
    }

    [Fact(DisplayName = "プロジェクトIDで関連タスクのIDリストを取得できる")]
    public async Task GetTaskIdsByProjectIdAsync_ReturnsTaskIds()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var taskId1 = Guid.NewGuid();
        var taskId2 = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var taskCreated1 = new TaskCreated
        {
            AggregateId = taskId1,
            ProjectId = projectId,
            Title = "Task 1",
            Description = "Description 1",
            ScheduledPeriod = new ScheduledPeriod(now, now.AddDays(7), 40),
            CreatedBy = "user1",
            OccurredAt = now
        };

        var taskCreated2 = new TaskCreated
        {
            AggregateId = taskId2,
            ProjectId = projectId,
            Title = "Task 2",
            Description = "Description 2",
            ScheduledPeriod = new ScheduledPeriod(now, now.AddDays(14), 80),
            CreatedBy = "user1",
            OccurredAt = now
        };

        await _eventStore.SaveEventsAsync(taskId1, new[] { taskCreated1 }, -1);
        await _eventStore.SaveEventsAsync(taskId2, new[] { taskCreated2 }, -1);

        // Act
        var taskIds = await _eventStore.GetTaskIdsByProjectIdAsync(projectId);

        // Assert
        Assert.Equal(2, taskIds.Count);
        Assert.Contains(taskId1, taskIds);
        Assert.Contains(taskId2, taskIds);
    }

    [Fact(DisplayName = "削除されたタスクはタスクIDリストに含まれない")]
    public async Task GetTaskIdsByProjectIdAsync_ExcludesDeletedTasks()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var taskId1 = Guid.NewGuid();
        var taskId2 = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var taskCreated1 = new TaskCreated
        {
            AggregateId = taskId1,
            ProjectId = projectId,
            Title = "Task 1",
            Description = "Description 1",
            ScheduledPeriod = new ScheduledPeriod(now, now.AddDays(7), 40),
            CreatedBy = "user1",
            OccurredAt = now
        };

        var taskCreated2 = new TaskCreated
        {
            AggregateId = taskId2,
            ProjectId = projectId,
            Title = "Task 2",
            Description = "Description 2",
            ScheduledPeriod = new ScheduledPeriod(now, now.AddDays(14), 80),
            CreatedBy = "user1",
            OccurredAt = now
        };

        var taskDeleted1 = new TaskDeleted
        {
            AggregateId = taskId1,
            ProjectId = projectId,
            DeletedBy = "user1",
            OccurredAt = now.AddHours(1)
        };

        await _eventStore.SaveEventsAsync(taskId1, new[] { taskCreated1 }, -1);
        await _eventStore.SaveEventsAsync(taskId2, new[] { taskCreated2 }, -1);
        await _eventStore.SaveEventsAsync(taskId1, new[] { taskDeleted1 }, 0);

        // Act
        var taskIds = await _eventStore.GetTaskIdsByProjectIdAsync(projectId);

        // Assert
        Assert.Single(taskIds);
        Assert.Contains(taskId2, taskIds);
        Assert.DoesNotContain(taskId1, taskIds);
    }

    [Fact(DisplayName = "異なるプロジェクトのタスクは含まれない")]
    public async Task GetTaskIdsByProjectIdAsync_FiltersTasksByProjectId()
    {
        // Arrange
        var projectId1 = Guid.NewGuid();
        var projectId2 = Guid.NewGuid();
        var taskId1 = Guid.NewGuid();
        var taskId2 = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var taskCreated1 = new TaskCreated
        {
            AggregateId = taskId1,
            ProjectId = projectId1,
            Title = "Task 1",
            Description = "Description 1",
            ScheduledPeriod = new ScheduledPeriod(now, now.AddDays(7), 40),
            CreatedBy = "user1",
            OccurredAt = now
        };

        var taskCreated2 = new TaskCreated
        {
            AggregateId = taskId2,
            ProjectId = projectId2,
            Title = "Task 2",
            Description = "Description 2",
            ScheduledPeriod = new ScheduledPeriod(now, now.AddDays(14), 80),
            CreatedBy = "user1",
            OccurredAt = now
        };

        await _eventStore.SaveEventsAsync(taskId1, new[] { taskCreated1 }, -1);
        await _eventStore.SaveEventsAsync(taskId2, new[] { taskCreated2 }, -1);

        // Act
        var taskIdsForProject1 = await _eventStore.GetTaskIdsByProjectIdAsync(projectId1);

        // Assert
        Assert.Single(taskIdsForProject1);
        Assert.Contains(taskId1, taskIdsForProject1);
        Assert.DoesNotContain(taskId2, taskIdsForProject1);
    }

    [Fact(DisplayName = "タスクがないプロジェクトは空のリストを返す")]
    public async Task GetTaskIdsByProjectIdAsync_ReturnsEmptyListWhenNoTasks()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        // Act
        var taskIds = await _eventStore.GetTaskIdsByProjectIdAsync(projectId);

        // Assert
        Assert.Empty(taskIds);
    }

    [Fact(DisplayName = "HasEventsAsync - イベントが存在しない場合はfalseを返す")]
    public async Task HasEventsAsync_WhenNoEvents_ReturnsFalse()
    {
        // Act
        var hasEvents = await _eventStore.HasEventsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.False(hasEvents);
    }

    [Fact(DisplayName = "HasEventsAsync - イベントが存在する場合はtrueを返す")]
    public async Task HasEventsAsync_WhenEventsExist_ReturnsTrue()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var event1 = new TaskCreated
        {
            AggregateId = aggregateId,
            ProjectId = projectId,
            Title = "Test Task",
            Description = "Description",
            ScheduledPeriod = new ScheduledPeriod(now, now.AddDays(7), 40),
            CreatedBy = "user1"
        };

        await _eventStore.SaveEventsAsync(aggregateId, new[] { event1 }, -1);

        // Act
        var hasEvents = await _eventStore.HasEventsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(hasEvents);
    }

    [Fact(DisplayName = "GetAllEventsAsync - 全イベントを時系列順に取得できる")]
    public async Task GetAllEventsAsync_ReturnsAllEventsInChronologicalOrder()
    {
        // Arrange
        var aggregateId1 = Guid.NewGuid();
        var aggregateId2 = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var time1 = DateTimeOffset.UtcNow.AddHours(-2);
        var time2 = DateTimeOffset.UtcNow.AddHours(-1);
        var time3 = DateTimeOffset.UtcNow;

        var event1 = new TaskCreated
        {
            AggregateId = aggregateId1,
            ProjectId = projectId,
            Title = "Task 1",
            Description = "Description",
            ScheduledPeriod = new ScheduledPeriod(time1, time1.AddDays(7), 40),
            CreatedBy = "user1",
            OccurredAt = time1
        };

        var event2 = new TaskCreated
        {
            AggregateId = aggregateId2,
            ProjectId = projectId,
            Title = "Task 2",
            Description = "Description",
            ScheduledPeriod = new ScheduledPeriod(time2, time2.AddDays(7), 40),
            CreatedBy = "user1",
            OccurredAt = time2
        };

        var event3 = new TaskStatusChanged
        {
            AggregateId = aggregateId1,
            OldStatus = TaskStatus.Todo,
            NewStatus = TaskStatus.InProgress,
            ChangedBy = "user1",
            OccurredAt = time3
        };

        await _eventStore.SaveEventsAsync(aggregateId1, new[] { event1 }, -1);
        await _eventStore.SaveEventsAsync(aggregateId2, new[] { event2 }, -1);
        await _eventStore.SaveEventsAsync(aggregateId1, new[] { event3 }, 0);

        // Act
        var allEvents = await _eventStore.GetAllEventsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3, allEvents.Count);

        // デシリアライズ済みのイベントとして返されることを確認
        Assert.IsType<TaskCreated>(allEvents[0]);
        Assert.IsType<TaskCreated>(allEvents[1]);
        Assert.IsType<TaskStatusChanged>(allEvents[2]);

        // 時系列順であることを確認
        Assert.True(allEvents[0].OccurredAt <= allEvents[1].OccurredAt);
        Assert.True(allEvents[1].OccurredAt <= allEvents[2].OccurredAt);
    }

    [Fact(DisplayName = "GetAllEventsAsync - イベントがない場合は空のリストを返す")]
    public async Task GetAllEventsAsync_WhenNoEvents_ReturnsEmptyList()
    {
        // Act
        var allEvents = await _eventStore.GetAllEventsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(allEvents);
    }
}

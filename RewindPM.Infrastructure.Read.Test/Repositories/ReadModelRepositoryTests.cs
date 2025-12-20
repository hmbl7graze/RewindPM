using Microsoft.EntityFrameworkCore;
using RewindPM.Infrastructure.Read.SQLite.Entities;
using RewindPM.Infrastructure.Read.SQLite.Persistence;
using RewindPM.Infrastructure.Read.SQLite.Repositories;
using TaskStatus = RewindPM.Domain.ValueObjects.TaskStatus;

namespace RewindPM.Infrastructure.Read.Test.Repositories;

public class ReadModelRepositoryTests : IDisposable
{
    private readonly ReadModelDbContext _context;
    private readonly ReadModelRepository _repository;

    public ReadModelRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ReadModelDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ReadModelDbContext(options);
        _repository = new ReadModelRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact(DisplayName = "全プロジェクトを取得できること")]
    public async Task GetAllProjectsAsync_ShouldReturnAllProjects()
    {
        // Arrange
        var project1 = new ProjectEntity
        {
            Id = Guid.NewGuid(),
            Title = "Project 1",
            Description = "Description 1",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = null,
            CreatedBy = "user1"
        };
        var project2 = new ProjectEntity
        {
            Id = Guid.NewGuid(),
            Title = "Project 2",
            Description = "Description 2",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = null,
            CreatedBy = "user1"
        };

        _context.Projects.AddRange(project1, project2);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetAllProjectsAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.Id == project1.Id);
        Assert.Contains(result, p => p.Id == project2.Id);
    }

    [Fact(DisplayName = "指定されたIDのプロジェクトを取得できること")]
    public async Task GetProjectByIdAsync_ExistingProject_ShouldReturnProject()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var project = new ProjectEntity
        {
            Id = projectId,
            Title = "Test Project",
            Description = "Test Description",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = null,
            CreatedBy = "user1"
        };

        _context.Projects.Add(project);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetProjectByIdAsync(projectId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(projectId, result.Id);
        Assert.Equal("Test Project", result.Title);
    }

    [Fact(DisplayName = "存在しないプロジェクトの場合はnullを返すこと")]
    public async Task GetProjectByIdAsync_NonExistentProject_ShouldReturnNull()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        // Act
        var result = await _repository.GetProjectByIdAsync(projectId);

        // Assert
        Assert.Null(result);
    }

    [Fact(DisplayName = "指定されたプロジェクトの全タスクを取得できること")]
    public async Task GetTasksByProjectIdAsync_ShouldReturnTasksByProjectId()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var task1 = new TaskEntity
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Title = "Task 1",
            Description = "Description 1",
            Status = TaskStatus.Todo,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = null,
            CreatedBy = "user1"
        };
        var task2 = new TaskEntity
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Title = "Task 2",
            Description = "Description 2",
            Status = TaskStatus.InProgress,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = null,
            CreatedBy = "user1"
        };

        _context.Tasks.AddRange(task1, task2);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetTasksByProjectIdAsync(projectId);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, t => Assert.Equal(projectId, t.ProjectId));
    }

    [Fact(DisplayName = "指定されたIDのタスクを取得できること")]
    public async Task GetTaskByIdAsync_ExistingTask_ShouldReturnTask()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = new TaskEntity
        {
            Id = taskId,
            ProjectId = Guid.NewGuid(),
            Title = "Test Task",
            Description = "Test Description",
            Status = TaskStatus.Todo,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = null,
            CreatedBy = "user1"
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetTaskByIdAsync(taskId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(taskId, result.Id);
        Assert.Equal("Test Task", result.Title);
    }

    [Fact(DisplayName = "存在しないタスクの場合はnullを返すこと")]
    public async Task GetTaskByIdAsync_NonExistentTask_ShouldReturnNull()
    {
        // Arrange
        var taskId = Guid.NewGuid();

        // Act
        var result = await _repository.GetTaskByIdAsync(taskId);

        // Assert
        Assert.Null(result);
    }

    [Fact(DisplayName = "指定された時点のプロジェクト状態を取得できること")]
    public async Task GetProjectAtTimeAsync_ShouldReturnProjectAtSpecificTime()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var baseDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // 1月1日のスナップショット
        var history1 = new ProjectHistoryEntity
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            SnapshotDate = baseDate,
            Title = "Project v1",
            Description = "Version 1",
            CreatedAt = baseDate,
            UpdatedAt = baseDate,
            CreatedBy = "user1",
            UpdatedBy = "user1",
            SnapshotCreatedAt = baseDate
        };

        // 1月3日のスナップショット
        var history2 = new ProjectHistoryEntity
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            SnapshotDate = baseDate.AddDays(2),
            Title = "Project v2",
            Description = "Version 2",
            CreatedAt = baseDate,
            UpdatedAt = baseDate.AddDays(2),
            CreatedBy = "user1",
            UpdatedBy = "user2",
            SnapshotCreatedAt = baseDate.AddDays(2)
        };

        _context.ProjectHistories.AddRange(history1, history2);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act - 1月2日の時点を取得（1月1日のスナップショットが返るはず）
        var result = await _repository.GetProjectAtTimeAsync(projectId, baseDate.AddDays(1));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(projectId, result.Id);
        Assert.Equal("Project v1", result.Title);
    }

    [Fact(DisplayName = "指定された時点以降の最新のスナップショットを取得できること")]
    public async Task GetProjectAtTimeAsync_ShouldReturnLatestSnapshotBeforeTime()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var baseDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var history1 = new ProjectHistoryEntity
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            SnapshotDate = baseDate,
            Title = "Project v1",
            Description = "Version 1",
            CreatedAt = baseDate,
            UpdatedAt = baseDate,
            CreatedBy = "user1",
            SnapshotCreatedAt = baseDate
        };

        var history2 = new ProjectHistoryEntity
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            SnapshotDate = baseDate.AddDays(2),
            Title = "Project v2",
            Description = "Version 2",
            CreatedAt = baseDate,
            UpdatedAt = baseDate.AddDays(2),
            CreatedBy = "user1",
            UpdatedBy = "user2",
            SnapshotCreatedAt = baseDate.AddDays(2)
        };

        _context.ProjectHistories.AddRange(history1, history2);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act - 1月5日の時点を取得（1月3日のスナップショットが返るはず）
        var result = await _repository.GetProjectAtTimeAsync(projectId, baseDate.AddDays(4));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(projectId, result.Id);
        Assert.Equal("Project v2", result.Title);
    }

    [Fact(DisplayName = "指定された時点にプロジェクトが存在しない場合はnullを返すこと")]
    public async Task GetProjectAtTimeAsync_NoHistoryBeforeTime_ShouldReturnNull()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var baseDate = new DateTimeOffset(2024, 1, 5, 0, 0, 0, TimeSpan.Zero);

        var history = new ProjectHistoryEntity
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            SnapshotDate = baseDate,
            Title = "Project v1",
            Description = "Version 1",
            CreatedAt = baseDate,
            UpdatedAt = baseDate,
            CreatedBy = "user1",
            SnapshotCreatedAt = baseDate
        };

        _context.ProjectHistories.Add(history);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act - 1月3日の時点を取得（1月5日より前なのでnull）
        var result = await _repository.GetProjectAtTimeAsync(projectId, baseDate.AddDays(-2));

        // Assert
        Assert.Null(result);
    }

    [Fact(DisplayName = "指定された時点のタスク状態を取得できること")]
    public async Task GetTaskAtTimeAsync_ShouldReturnTaskAtSpecificTime()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var baseDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var history = new TaskHistoryEntity
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            ProjectId = projectId,
            SnapshotDate = baseDate,
            Title = "Task v1",
            Description = "Version 1",
            Status = TaskStatus.Todo,
            CreatedAt = baseDate,
            UpdatedAt = baseDate,
            CreatedBy = "user1",
            SnapshotCreatedAt = baseDate
        };

        _context.TaskHistories.Add(history);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetTaskAtTimeAsync(taskId, baseDate.AddDays(1));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(taskId, result.Id);
        Assert.Equal("Task v1", result.Title);
        Assert.Equal(TaskStatus.Todo, result.Status);
    }

    [Fact(DisplayName = "指定された時点のプロジェクトの全タスクを取得できること")]
    public async Task GetTasksByProjectIdAtTimeAsync_ShouldReturnTasksAtSpecificTime()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var task1Id = Guid.NewGuid();
        var task2Id = Guid.NewGuid();
        var baseDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var history1 = new TaskHistoryEntity
        {
            Id = Guid.NewGuid(),
            TaskId = task1Id,
            ProjectId = projectId,
            SnapshotDate = baseDate,
            Title = "Task 1 v1",
            Description = "Version 1",
            Status = TaskStatus.Todo,
            CreatedAt = baseDate,
            UpdatedAt = baseDate,
            CreatedBy = "user1",
            SnapshotCreatedAt = baseDate
        };

        var history2 = new TaskHistoryEntity
        {
            Id = Guid.NewGuid(),
            TaskId = task2Id,
            ProjectId = projectId,
            SnapshotDate = baseDate,
            Title = "Task 2 v1",
            Description = "Version 1",
            Status = TaskStatus.InProgress,
            CreatedAt = baseDate,
            UpdatedAt = baseDate,
            CreatedBy = "user1",
            SnapshotCreatedAt = baseDate
        };

        _context.TaskHistories.AddRange(history1, history2);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetTasksByProjectIdAtTimeAsync(projectId, baseDate.AddDays(1));

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, t => t.Id == task1Id && t.Title == "Task 1 v1");
        Assert.Contains(result, t => t.Id == task2Id && t.Title == "Task 2 v1");
    }

    [Fact(DisplayName = "指定された時点にタスクが存在しない場合は空のリストを返すこと")]
    public async Task GetTasksByProjectIdAtTimeAsync_NoHistoryBeforeTime_ShouldReturnEmptyList()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var baseDate = new DateTimeOffset(2024, 1, 5, 0, 0, 0, TimeSpan.Zero);

        var history = new TaskHistoryEntity
        {
            Id = Guid.NewGuid(),
            TaskId = Guid.NewGuid(),
            ProjectId = projectId,
            SnapshotDate = baseDate,
            Title = "Task v1",
            Description = "Version 1",
            Status = TaskStatus.Todo,
            CreatedAt = baseDate,
            UpdatedAt = baseDate,
            CreatedBy = "user1",
            SnapshotCreatedAt = baseDate
        };

        _context.TaskHistories.Add(history);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act - 1月3日の時点を取得（1月5日より前なので空リスト）
        var result = await _repository.GetTasksByProjectIdAtTimeAsync(projectId, baseDate.AddDays(-2));

        // Assert
        Assert.Empty(result);
    }

    [Fact(DisplayName = "プロジェクトの編集日一覧を降順（新しい順）で取得できること")]
    public async Task GetProjectEditDatesAsync_Descending_ShouldReturnEditDatesInDescendingOrder()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var baseDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // 複数の日付でタスク履歴を作成
        var history1 = new TaskHistoryEntity
        {
            Id = Guid.NewGuid(),
            TaskId = Guid.NewGuid(),
            ProjectId = projectId,
            SnapshotDate = baseDate, // 1月1日
            Title = "Task on Jan 1",
            Description = "Description",
            Status = TaskStatus.Todo,
            CreatedAt = baseDate,
            CreatedBy = "user1",
            SnapshotCreatedAt = baseDate
        };

        var history2 = new TaskHistoryEntity
        {
            Id = Guid.NewGuid(),
            TaskId = Guid.NewGuid(),
            ProjectId = projectId,
            SnapshotDate = baseDate.AddDays(2), // 1月3日
            Title = "Task on Jan 3",
            Description = "Description",
            Status = TaskStatus.Todo,
            CreatedAt = baseDate.AddDays(2),
            CreatedBy = "user1",
            SnapshotCreatedAt = baseDate.AddDays(2)
        };

        var history3 = new TaskHistoryEntity
        {
            Id = Guid.NewGuid(),
            TaskId = Guid.NewGuid(),
            ProjectId = projectId,
            SnapshotDate = baseDate.AddDays(4), // 1月5日
            Title = "Task on Jan 5",
            Description = "Description",
            Status = TaskStatus.Todo,
            CreatedAt = baseDate.AddDays(4),
            CreatedBy = "user1",
            SnapshotCreatedAt = baseDate.AddDays(4)
        };

        _context.TaskHistories.AddRange(history1, history2, history3);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetProjectEditDatesAsync(projectId, ascending: false, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(baseDate.AddDays(4), result[0]); // 1月5日
        Assert.Equal(baseDate.AddDays(2), result[1]); // 1月3日
        Assert.Equal(baseDate, result[2]); // 1月1日
    }

    [Fact(DisplayName = "プロジェクトの編集日一覧を昇順（古い順）で取得できること")]
    public async Task GetProjectEditDatesAsync_Ascending_ShouldReturnEditDatesInAscendingOrder()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var baseDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        var history1 = new TaskHistoryEntity
        {
            Id = Guid.NewGuid(),
            TaskId = Guid.NewGuid(),
            ProjectId = projectId,
            SnapshotDate = baseDate,
            Title = "Task on Jan 1",
            Description = "Description",
            Status = TaskStatus.Todo,
            CreatedAt = baseDate,
            CreatedBy = "user1",
            SnapshotCreatedAt = baseDate
        };

        var history2 = new TaskHistoryEntity
        {
            Id = Guid.NewGuid(),
            TaskId = Guid.NewGuid(),
            ProjectId = projectId,
            SnapshotDate = baseDate.AddDays(2),
            Title = "Task on Jan 3",
            Description = "Description",
            Status = TaskStatus.Todo,
            CreatedAt = baseDate.AddDays(2),
            CreatedBy = "user1",
            SnapshotCreatedAt = baseDate.AddDays(2)
        };

        _context.TaskHistories.AddRange(history1, history2);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetProjectEditDatesAsync(projectId, ascending: true, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(baseDate, result[0]); // 1月1日
        Assert.Equal(baseDate.AddDays(2), result[1]); // 1月3日
    }

    [Fact(DisplayName = "同じ日に複数のタスク履歴がある場合、日付は重複せずに1つだけ返すこと")]
    public async Task GetProjectEditDatesAsync_MultipleTasks_ShouldReturnDistinctDates()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var baseDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // 同じ日付（1月1日）に3つのタスク履歴を作成
        var history1 = new TaskHistoryEntity
        {
            Id = Guid.NewGuid(),
            TaskId = Guid.NewGuid(),
            ProjectId = projectId,
            SnapshotDate = baseDate,
            Title = "Task 1",
            Description = "Description",
            Status = TaskStatus.Todo,
            CreatedAt = baseDate,
            CreatedBy = "user1",
            SnapshotCreatedAt = baseDate
        };

        var history2 = new TaskHistoryEntity
        {
            Id = Guid.NewGuid(),
            TaskId = Guid.NewGuid(),
            ProjectId = projectId,
            SnapshotDate = baseDate, // 同じ日付
            Title = "Task 2",
            Description = "Description",
            Status = TaskStatus.Todo,
            CreatedAt = baseDate,
            CreatedBy = "user1",
            SnapshotCreatedAt = baseDate
        };

        var history3 = new TaskHistoryEntity
        {
            Id = Guid.NewGuid(),
            TaskId = Guid.NewGuid(),
            ProjectId = projectId,
            SnapshotDate = baseDate, // 同じ日付
            Title = "Task 3",
            Description = "Description",
            Status = TaskStatus.Todo,
            CreatedAt = baseDate,
            CreatedBy = "user1",
            SnapshotCreatedAt = baseDate
        };

        _context.TaskHistories.AddRange(history1, history2, history3);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetProjectEditDatesAsync(projectId, ascending: false, TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(result); // 重複せず1つだけ
        Assert.Equal(baseDate, result[0]);
    }

    [Fact(DisplayName = "編集履歴が存在しない場合は空のリストを返すこと")]
    public async Task GetProjectEditDatesAsync_NoHistory_ShouldReturnEmptyList()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        // Act
        var result = await _repository.GetProjectEditDatesAsync(projectId, ascending: false, TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(result);
    }

    [Fact(DisplayName = "他のプロジェクトの編集日は含まれないこと")]
    public async Task GetProjectEditDatesAsync_ShouldFilterByProjectId()
    {
        // Arrange
        var projectId1 = Guid.NewGuid();
        var projectId2 = Guid.NewGuid();
        var baseDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // プロジェクト1のタスク履歴
        var history1 = new TaskHistoryEntity
        {
            Id = Guid.NewGuid(),
            TaskId = Guid.NewGuid(),
            ProjectId = projectId1,
            SnapshotDate = baseDate,
            Title = "Project 1 Task",
            Description = "Description",
            Status = TaskStatus.Todo,
            CreatedAt = baseDate,
            CreatedBy = "user1",
            SnapshotCreatedAt = baseDate
        };

        // プロジェクト2のタスク履歴（別の日付）
        var history2 = new TaskHistoryEntity
        {
            Id = Guid.NewGuid(),
            TaskId = Guid.NewGuid(),
            ProjectId = projectId2,
            SnapshotDate = baseDate.AddDays(5),
            Title = "Project 2 Task",
            Description = "Description",
            Status = TaskStatus.Todo,
            CreatedAt = baseDate.AddDays(5),
            CreatedBy = "user1",
            SnapshotCreatedAt = baseDate.AddDays(5)
        };

        _context.TaskHistories.AddRange(history1, history2);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetProjectEditDatesAsync(projectId1, ascending: false, TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(result);
        Assert.Equal(baseDate, result[0]); // プロジェクト1の日付のみ
    }

    [Fact(DisplayName = "同じ日付の異なる時刻の履歴がある場合、日付部分のみで重複を除外すること")]
    public async Task GetProjectEditDatesAsync_SameDay_DifferentTimes_ShouldReturnOneDatePerDay()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var baseDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        // 同じ日（1月1日）の異なる時刻に3つのタスク履歴を作成
        var history1 = new TaskHistoryEntity
        {
            Id = Guid.NewGuid(),
            TaskId = Guid.NewGuid(),
            ProjectId = projectId,
            SnapshotDate = baseDate.AddHours(9), // 9:00
            Title = "Task 1",
            Description = "Description",
            Status = TaskStatus.Todo,
            CreatedAt = baseDate,
            CreatedBy = "user1",
            SnapshotCreatedAt = baseDate.AddHours(9)
        };

        var history2 = new TaskHistoryEntity
        {
            Id = Guid.NewGuid(),
            TaskId = Guid.NewGuid(),
            ProjectId = projectId,
            SnapshotDate = baseDate.AddHours(14), // 14:00（同じ日）
            Title = "Task 2",
            Description = "Description",
            Status = TaskStatus.Todo,
            CreatedAt = baseDate,
            CreatedBy = "user1",
            SnapshotCreatedAt = baseDate.AddHours(14)
        };

        var history3 = new TaskHistoryEntity
        {
            Id = Guid.NewGuid(),
            TaskId = Guid.NewGuid(),
            ProjectId = projectId,
            SnapshotDate = baseDate.AddHours(18), // 18:00（同じ日）
            Title = "Task 3",
            Description = "Description",
            Status = TaskStatus.Todo,
            CreatedAt = baseDate,
            CreatedBy = "user1",
            SnapshotCreatedAt = baseDate.AddHours(18)
        };

        _context.TaskHistories.AddRange(history1, history2, history3);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetProjectEditDatesAsync(projectId, ascending: false, TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(result); // 時刻が異なっても日付部分で重複除外されるため1つだけ
        Assert.Equal(baseDate.Date, result[0].Date);
    }
}

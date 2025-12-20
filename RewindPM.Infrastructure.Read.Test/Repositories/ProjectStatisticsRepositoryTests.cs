using Microsoft.EntityFrameworkCore;
using RewindPM.Infrastructure.Read.SQLite.Entities;
using RewindPM.Infrastructure.Read.SQLite.Persistence;
using RewindPM.Infrastructure.Read.SQLite.Repositories;
using TaskStatus = RewindPM.Domain.ValueObjects.TaskStatus;
using Xunit;

namespace RewindPM.Infrastructure.Read.Test.Repositories;

public class ProjectStatisticsRepositoryTests : IDisposable
{
    private readonly ReadModelDbContext _context;
    private readonly ProjectStatisticsRepository _repository;

    public ProjectStatisticsRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ReadModelDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ReadModelDbContext(options);
        _repository = new ProjectStatisticsRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact(DisplayName = "タスクがないプロジェクトの統計を正しく取得する")]
    public async Task GetProjectStatisticsSummaryAsync_NoTasks_ReturnsZeroStatistics()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        // Act
        var result = await _repository.GetProjectStatisticsSummaryAsync(projectId, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(projectId, result.ProjectId);
        Assert.Equal(0, result.TotalTasks);
        Assert.Equal(0, result.CompletedTasks);
        Assert.Equal(0, result.InProgressTasks);
        Assert.Equal(0, result.InReviewTasks);
        Assert.Equal(0, result.TodoTasks);
        Assert.Equal(0, result.CompletionRate);
    }

    [Fact(DisplayName = "複数のタスクを持つプロジェクトの統計を正しく計算する")]
    public async Task GetProjectStatisticsSummaryAsync_WithTasks_CalculatesStatisticsCorrectly()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var tasks = new[]
        {
            CreateTask(projectId, TaskStatus.Done),
            CreateTask(projectId, TaskStatus.Done),
            CreateTask(projectId, TaskStatus.Done),
            CreateTask(projectId, TaskStatus.InProgress),
            CreateTask(projectId, TaskStatus.InProgress),
            CreateTask(projectId, TaskStatus.InReview),
            CreateTask(projectId, TaskStatus.Todo),
        };

        _context.Tasks.AddRange(tasks);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetProjectStatisticsSummaryAsync(projectId, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(projectId, result.ProjectId);
        Assert.Equal(7, result.TotalTasks);
        Assert.Equal(3, result.CompletedTasks);
        Assert.Equal(2, result.InProgressTasks);
        Assert.Equal(1, result.InReviewTasks);
        Assert.Equal(1, result.TodoTasks);
        Assert.Equal(42.9, result.CompletionRate); // 3/7 = 42.857... -> 42.9
    }

    [Fact(DisplayName = "すべてのタスクが完了している場合は完了率100%を返す")]
    public async Task GetProjectStatisticsSummaryAsync_AllCompleted_Returns100Percent()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var tasks = new[]
        {
            CreateTask(projectId, TaskStatus.Done),
            CreateTask(projectId, TaskStatus.Done),
            CreateTask(projectId, TaskStatus.Done),
        };

        _context.Tasks.AddRange(tasks);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetProjectStatisticsSummaryAsync(projectId, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3, result.TotalTasks);
        Assert.Equal(3, result.CompletedTasks);
        Assert.Equal(100.0, result.CompletionRate);
    }

    [Fact(DisplayName = "他のプロジェクトのタスクは統計に含まない")]
    public async Task GetProjectStatisticsSummaryAsync_ExcludesOtherProjectTasks()
    {
        // Arrange
        var projectId1 = Guid.NewGuid();
        var projectId2 = Guid.NewGuid();

        var tasks = new[]
        {
            CreateTask(projectId1, TaskStatus.Done),
            CreateTask(projectId1, TaskStatus.InProgress),
            CreateTask(projectId2, TaskStatus.Done),
            CreateTask(projectId2, TaskStatus.Done),
            CreateTask(projectId2, TaskStatus.Done),
        };

        _context.Tasks.AddRange(tasks);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetProjectStatisticsSummaryAsync(projectId1, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result.TotalTasks);
        Assert.Equal(1, result.CompletedTasks);
        Assert.Equal(1, result.InProgressTasks);
        Assert.Equal(0, result.InReviewTasks);
        Assert.Equal(0, result.TodoTasks);
    }

    [Fact(DisplayName = "CancellationTokenが渡された場合も正しく動作する")]
    public async Task GetProjectStatisticsSummaryAsync_WithCancellationToken_WorksCorrectly()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var tasks = new[]
        {
            CreateTask(projectId, TaskStatus.Done),
            CreateTask(projectId, TaskStatus.Todo),
        };

        _context.Tasks.AddRange(tasks);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var cancellationToken = TestContext.Current.CancellationToken;

        // Act
        var result = await _repository.GetProjectStatisticsSummaryAsync(projectId, cancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalTasks);
        Assert.Equal(1, result.CompletedTasks);
        Assert.Equal(1, result.TodoTasks);
    }

    private TaskEntity CreateTask(Guid projectId, TaskStatus status)
    {
        return new TaskEntity
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Title = $"Task {Guid.NewGuid()}",
            Description = "Test Description",
            Status = status,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "test-user"
        };
    }
}

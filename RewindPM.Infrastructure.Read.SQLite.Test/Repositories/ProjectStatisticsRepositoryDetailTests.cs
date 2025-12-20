using Microsoft.EntityFrameworkCore;
using RewindPM.Infrastructure.Read.SQLite.Entities;
using RewindPM.Infrastructure.Read.SQLite.Persistence;
using RewindPM.Infrastructure.Read.SQLite.Repositories;
using TaskStatus = RewindPM.Domain.ValueObjects.TaskStatus;

namespace RewindPM.Infrastructure.Read.SQLite.Test.Repositories;

public class ProjectStatisticsRepositoryDetailTests : IDisposable
{
    private readonly ReadModelDbContext _context;
    private readonly ProjectStatisticsRepository _repository;

    public ProjectStatisticsRepositoryDetailTests()
    {
        var options = new DbContextOptionsBuilder<ReadModelDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ReadModelDbContext(options);
        _repository = new ProjectStatisticsRepository(_context);
    }

    [Fact(DisplayName = "詳細統計リポジトリ: 有効なプロジェクトの場合、正しい統計情報を返す")]
    public async Task GetProjectStatisticsDetailAsync_WithValidProject_ReturnsStatistics()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var asOfDate = DateTimeOffset.UtcNow;

        _context.Projects.Add(new ProjectEntity
        {
            Id = projectId,
            Title = "Test Project",
            CreatedAt = asOfDate.AddDays(-10),
            CreatedBy = "test"
        });

        var tasks = new List<TaskEntity>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Title = "Task 1",
                Status = TaskStatus.Done,
                EstimatedHours = 10,
                ActualHours = 8,
                ScheduledEndDate = asOfDate.AddDays(-2),
                ActualEndDate = asOfDate.AddDays(-3), // 期限内完了
                CreatedAt = asOfDate.AddDays(-5),
                CreatedBy = "test"
            },
            new()
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Title = "Task 2",
                Status = TaskStatus.Done,
                EstimatedHours = 20,
                ActualHours = 25,
                ScheduledEndDate = asOfDate.AddDays(-1),
                ActualEndDate = asOfDate.AddDays(1), // 2日遅延
                CreatedAt = asOfDate.AddDays(-5),
                CreatedBy = "test"
            },
            new()
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Title = "Task 3",
                Status = TaskStatus.InProgress,
                EstimatedHours = 15,
                ActualHours = 10,
                CreatedAt = asOfDate.AddDays(-3),
                CreatedBy = "test"
            },
            new()
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Title = "Task 4",
                Status = TaskStatus.InReview,
                EstimatedHours = 5,
                CreatedAt = asOfDate.AddDays(-2),
                CreatedBy = "test"
            },
            new()
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Title = "Task 5",
                Status = TaskStatus.Todo,
                EstimatedHours = 30,
                CreatedAt = asOfDate.AddDays(-1),
                CreatedBy = "test"
            }
        };

        _context.Tasks.AddRange(tasks);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetProjectStatisticsDetailAsync(
            projectId,
            asOfDate,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(5, result.TotalTasks);
        Assert.Equal(2, result.CompletedTasks);
        Assert.Equal(1, result.InProgressTasks);
        Assert.Equal(1, result.InReviewTasks);
        Assert.Equal(1, result.TodoTasks);

        // 工数統計
        Assert.Equal(80, result.TotalEstimatedHours); // 10 + 20 + 15 + 5 + 30
        Assert.Equal(43, result.TotalActualHours); // 8 + 25 + 10
        Assert.Equal(40, result.RemainingEstimatedHours); // (15-10) + (5-0) + (30-0) = 5 + 5 + 30 = 40

        // スケジュール統計
        Assert.Equal(1, result.OnTimeTasks); // Task 1のみ期限内
        Assert.Equal(1, result.DelayedTasks); // Task 2が遅延
        Assert.Equal(2.0, result.AverageDelayDays); // Task 2の遅延が2日

        // 計算プロパティ
        Assert.Equal(40.0, result.CompletionRate); // 2/5 * 100
        Assert.Equal(-37, result.HoursOverrun); // 43 - 80
        Assert.Equal(53.8, result.HoursConsumptionRate, precision: 1); // 43/80 * 100 = 53.8%
        Assert.Equal(50.0, result.OnTimeRate); // 1/2 * 100
    }

    [Fact(DisplayName = "詳細統計リポジトリ: 存在しないプロジェクトの場合、nullを返す")]
    public async Task GetProjectStatisticsDetailAsync_WithNonExistentProject_ReturnsNull()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var asOfDate = DateTimeOffset.UtcNow;

        // Act
        var result = await _repository.GetProjectStatisticsDetailAsync(
            projectId,
            asOfDate,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
    }

    [Fact(DisplayName = "詳細統計リポジトリ: 基準日が指定された場合、作成日でタスクをフィルタする")]
    public async Task GetProjectStatisticsDetailAsync_WithAsOfDate_FiltersTasksByCreatedDate()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var asOfDate = new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.Zero);

        _context.Projects.Add(new ProjectEntity
        {
            Id = projectId,
            Title = "Test Project",
            CreatedAt = asOfDate.AddDays(-10),
            CreatedBy = "test"
        });

        var tasks = new List<TaskEntity>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Title = "Old Task",
                Status = TaskStatus.Done,
                EstimatedHours = 10,
                ActualHours = 10,
                CreatedAt = asOfDate.AddDays(-5), // 基準日より前
                CreatedBy = "test"
            },
            new()
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Title = "Future Task",
                Status = TaskStatus.Todo,
                EstimatedHours = 20,
                CreatedAt = asOfDate.AddDays(5), // 基準日より後
                CreatedBy = "test"
            }
        };

        _context.Tasks.AddRange(tasks);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetProjectStatisticsDetailAsync(
            projectId,
            asOfDate,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalTasks); // Future Taskは除外
        Assert.Equal(1, result.CompletedTasks);
        Assert.Equal(10, result.TotalEstimatedHours);
    }

    [Fact(DisplayName = "詳細統計リポジトリ: 遅延タスクがない場合、平均遅延日数は0を返す")]
    public async Task GetProjectStatisticsDetailAsync_WithNoDelayedTasks_ReturnsZeroAverageDelay()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var asOfDate = DateTimeOffset.UtcNow;

        _context.Projects.Add(new ProjectEntity
        {
            Id = projectId,
            Title = "Test Project",
            CreatedAt = asOfDate.AddDays(-10),
            CreatedBy = "test"
        });

        var tasks = new List<TaskEntity>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Title = "On-time Task",
                Status = TaskStatus.Done,
                EstimatedHours = 10,
                ActualHours = 10,
                ScheduledEndDate = asOfDate.AddDays(-1),
                ActualEndDate = asOfDate.AddDays(-2), // 期限内
                CreatedAt = asOfDate.AddDays(-5),
                CreatedBy = "test"
            }
        };

        _context.Tasks.AddRange(tasks);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetProjectStatisticsDetailAsync(
            projectId,
            asOfDate,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.OnTimeTasks);
        Assert.Equal(0, result.DelayedTasks);
        Assert.Equal(0, result.AverageDelayDays);
    }

    [Fact(DisplayName = "詳細統計リポジトリ: 完了タスクがない場合、スケジュール統計は0を返す")]
    public async Task GetProjectStatisticsDetailAsync_WithNoCompletedTasks_ReturnsZeroScheduleStats()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var asOfDate = DateTimeOffset.UtcNow;

        _context.Projects.Add(new ProjectEntity
        {
            Id = projectId,
            Title = "Test Project",
            CreatedAt = asOfDate.AddDays(-10),
            CreatedBy = "test"
        });

        var tasks = new List<TaskEntity>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Title = "Todo Task",
                Status = TaskStatus.Todo,
                EstimatedHours = 10,
                CreatedAt = asOfDate.AddDays(-5),
                CreatedBy = "test"
            }
        };

        _context.Tasks.AddRange(tasks);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetProjectStatisticsDetailAsync(
            projectId,
            asOfDate,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.CompletedTasks);
        Assert.Equal(0, result.OnTimeTasks);
        Assert.Equal(0, result.DelayedTasks);
        Assert.Equal(0, result.AverageDelayDays);
        Assert.Equal(0, result.OnTimeRate);
    }

    [Fact(DisplayName = "詳細統計リポジトリ: 複数の遅延タスクがある場合、平均遅延日数を正しく計算する")]
    public async Task GetProjectStatisticsDetailAsync_WithMultipleDelayedTasks_CalculatesAverageCorrectly()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var asOfDate = DateTimeOffset.UtcNow;

        _context.Projects.Add(new ProjectEntity
        {
            Id = projectId,
            Title = "Test Project",
            CreatedAt = asOfDate.AddDays(-10),
            CreatedBy = "test"
        });

        var tasks = new List<TaskEntity>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Title = "Delayed Task 1",
                Status = TaskStatus.Done,
                EstimatedHours = 10,
                ActualHours = 12,
                ScheduledEndDate = asOfDate.AddDays(-5),
                ActualEndDate = asOfDate.AddDays(-3), // 2日遅延
                CreatedAt = asOfDate.AddDays(-10),
                CreatedBy = "test"
            },
            new()
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Title = "Delayed Task 2",
                Status = TaskStatus.Done,
                EstimatedHours = 15,
                ActualHours = 20,
                ScheduledEndDate = asOfDate.AddDays(-4),
                ActualEndDate = asOfDate, // 4日遅延
                CreatedAt = asOfDate.AddDays(-9),
                CreatedBy = "test"
            }
        };

        _context.Tasks.AddRange(tasks);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetProjectStatisticsDetailAsync(
            projectId,
            asOfDate,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.DelayedTasks);
        Assert.Equal(3.0, result.AverageDelayDays); // (2 + 4) / 2
    }

    [Fact(DisplayName = "詳細統計リポジトリ: 見積もり精度 - 誤差率が正確に10%の場合、正確なタスクとしてカウントされる")]
    public async Task GetProjectStatisticsDetailAsync_EstimateAccuracy_ExactlyTenPercentError_CountsAsAccurate()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var asOfDate = DateTimeOffset.UtcNow;

        _context.Projects.Add(new ProjectEntity
        {
            Id = projectId,
            Title = "Test Project",
            CreatedAt = asOfDate.AddDays(-10),
            CreatedBy = "test"
        });

        var tasks = new List<TaskEntity>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Title = "Exact 10% Error Task",
                Status = TaskStatus.Done,
                EstimatedHours = 10,
                ActualHours = 11,
                ScheduledStartDate = asOfDate.AddDays(-10),
                ScheduledEndDate = asOfDate.AddDays(-5), // 予定期間: 5日
                ActualStartDate = asOfDate.AddDays(-10),
                ActualEndDate = asOfDate.AddDays(-4.5), // 実績期間: 5.5日 (誤差率 10%)
                CreatedAt = asOfDate.AddDays(-15),
                CreatedBy = "test"
            }
        };

        _context.Tasks.AddRange(tasks);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetProjectStatisticsDetailAsync(
            projectId,
            asOfDate,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.AccurateEstimateTasks); // 10%ちょうどなので正確
        Assert.Equal(0, result.OverEstimateTasks);
        Assert.Equal(0, result.UnderEstimateTasks);
    }

    [Fact(DisplayName = "詳細統計リポジトリ: 見積もり精度 - 誤差が正確に1日の場合、正確なタスクとしてカウントされる")]
    public async Task GetProjectStatisticsDetailAsync_EstimateAccuracy_ExactlyOneDayError_CountsAsAccurate()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var asOfDate = DateTimeOffset.UtcNow;

        _context.Projects.Add(new ProjectEntity
        {
            Id = projectId,
            Title = "Test Project",
            CreatedAt = asOfDate.AddDays(-10),
            CreatedBy = "test"
        });

        var tasks = new List<TaskEntity>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Title = "Exact 1 Day Error Task",
                Status = TaskStatus.Done,
                EstimatedHours = 10,
                ActualHours = 12,
                ScheduledStartDate = asOfDate.AddDays(-10),
                ScheduledEndDate = asOfDate.AddDays(-5), // 予定期間: 5日
                ActualStartDate = asOfDate.AddDays(-10),
                ActualEndDate = asOfDate.AddDays(-4), // 実績期間: 6日 (誤差 1日、誤差率 20%)
                CreatedAt = asOfDate.AddDays(-15),
                CreatedBy = "test"
            }
        };

        _context.Tasks.AddRange(tasks);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetProjectStatisticsDetailAsync(
            projectId,
            asOfDate,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.AccurateEstimateTasks); // 1日ちょうどなので正確
        Assert.Equal(0, result.OverEstimateTasks);
        Assert.Equal(0, result.UnderEstimateTasks);
    }

    [Fact(DisplayName = "詳細統計リポジトリ: 見積もり精度 - 誤差率と誤差日数の両方が境界値を超える場合、過大/過小見積もりとしてカウントされる")]
    public async Task GetProjectStatisticsDetailAsync_EstimateAccuracy_BothThresholdsExceeded_CountsAsOverOrUnderEstimate()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var asOfDate = DateTimeOffset.UtcNow;

        _context.Projects.Add(new ProjectEntity
        {
            Id = projectId,
            Title = "Test Project",
            CreatedAt = asOfDate.AddDays(-10),
            CreatedBy = "test"
        });

        var tasks = new List<TaskEntity>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Title = "Over Estimate Task",
                Status = TaskStatus.Done,
                EstimatedHours = 20,
                ActualHours = 8,
                ScheduledStartDate = asOfDate.AddDays(-10),
                ScheduledEndDate = asOfDate.AddDays(-5), // 予定期間: 5日
                ActualStartDate = asOfDate.AddDays(-10),
                ActualEndDate = asOfDate.AddDays(-7), // 実績期間: 3日 (誤差 2日、誤差率 40%)
                CreatedAt = asOfDate.AddDays(-15),
                CreatedBy = "test"
            },
            new()
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Title = "Under Estimate Task",
                Status = TaskStatus.Done,
                EstimatedHours = 10,
                ActualHours = 25,
                ScheduledStartDate = asOfDate.AddDays(-10),
                ScheduledEndDate = asOfDate.AddDays(-5), // 予定期間: 5日
                ActualStartDate = asOfDate.AddDays(-10),
                ActualEndDate = asOfDate.AddDays(-2), // 実績期間: 8日 (誤差 3日、誤差率 60%)
                CreatedAt = asOfDate.AddDays(-15),
                CreatedBy = "test"
            }
        };

        _context.Tasks.AddRange(tasks);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetProjectStatisticsDetailAsync(
            projectId,
            asOfDate,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.AccurateEstimateTasks);
        Assert.Equal(1, result.OverEstimateTasks);
        Assert.Equal(1, result.UnderEstimateTasks);
        Assert.Equal(0.5, result.AverageEstimateErrorDays); // (-2 + 3) / 2 = 0.5
    }

    [Fact(DisplayName = "詳細統計リポジトリ: 見積もり精度 - 日付情報が不完全なタスクは計算から除外される")]
    public async Task GetProjectStatisticsDetailAsync_EstimateAccuracy_IncompleteDateInfo_ExcludedFromCalculation()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var asOfDate = DateTimeOffset.UtcNow;

        _context.Projects.Add(new ProjectEntity
        {
            Id = projectId,
            Title = "Test Project",
            CreatedAt = asOfDate.AddDays(-10),
            CreatedBy = "test"
        });

        var tasks = new List<TaskEntity>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Title = "Complete Task",
                Status = TaskStatus.Done,
                EstimatedHours = 10,
                ActualHours = 10,
                ScheduledStartDate = asOfDate.AddDays(-10),
                ScheduledEndDate = asOfDate.AddDays(-5),
                ActualStartDate = asOfDate.AddDays(-10),
                ActualEndDate = asOfDate.AddDays(-5),
                CreatedAt = asOfDate.AddDays(-15),
                CreatedBy = "test"
            },
            new()
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Title = "Incomplete Dates Task",
                Status = TaskStatus.Done,
                EstimatedHours = 15,
                ActualHours = 20,
                ScheduledStartDate = asOfDate.AddDays(-10),
                // ScheduledEndDate がない
                ActualStartDate = asOfDate.AddDays(-10),
                ActualEndDate = asOfDate.AddDays(-3),
                CreatedAt = asOfDate.AddDays(-15),
                CreatedBy = "test"
            }
        };

        _context.Tasks.AddRange(tasks);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetProjectStatisticsDetailAsync(
            projectId,
            asOfDate,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.AccurateEstimateTasks); // 完全な日付情報を持つタスクのみカウント
        Assert.Equal(0, result.OverEstimateTasks);
        Assert.Equal(0, result.UnderEstimateTasks);
        Assert.Equal(0, result.AverageEstimateErrorDays);
    }

    [Fact(DisplayName = "詳細統計リポジトリ: 見積もり精度 - 完了タスクがない場合、すべて0を返す")]
    public async Task GetProjectStatisticsDetailAsync_EstimateAccuracy_NoCompletedTasks_ReturnsZero()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var asOfDate = DateTimeOffset.UtcNow;

        _context.Projects.Add(new ProjectEntity
        {
            Id = projectId,
            Title = "Test Project",
            CreatedAt = asOfDate.AddDays(-10),
            CreatedBy = "test"
        });

        var tasks = new List<TaskEntity>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Title = "Todo Task",
                Status = TaskStatus.Todo,
                EstimatedHours = 10,
                CreatedAt = asOfDate.AddDays(-5),
                CreatedBy = "test"
            }
        };

        _context.Tasks.AddRange(tasks);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _repository.GetProjectStatisticsDetailAsync(
            projectId,
            asOfDate,
            TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0, result.AccurateEstimateTasks);
        Assert.Equal(0, result.OverEstimateTasks);
        Assert.Equal(0, result.UnderEstimateTasks);
        Assert.Equal(0, result.AverageEstimateErrorDays);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

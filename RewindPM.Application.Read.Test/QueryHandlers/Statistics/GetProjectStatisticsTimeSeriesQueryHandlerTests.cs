using NSubstitute;
using RewindPM.Application.Read.DTOs;
using RewindPM.Application.Read.Queries.Statistics;
using RewindPM.Application.Read.QueryHandlers.Statistics;
using RewindPM.Application.Read.Repositories;

namespace RewindPM.Application.Read.Test.QueryHandlers.Statistics;

public class GetProjectStatisticsTimeSeriesQueryHandlerTests
{
    private readonly IProjectStatisticsRepository _repository;
    private readonly GetProjectStatisticsTimeSeriesQueryHandler _handler;

    public GetProjectStatisticsTimeSeriesQueryHandlerTests()
    {
        _repository = Substitute.For<IProjectStatisticsRepository>();
        _handler = new GetProjectStatisticsTimeSeriesQueryHandler(_repository);
    }

    [Fact(DisplayName = "有効なプロジェクトIDで時系列データを返す")]
    public async Task Handle_WithValidProjectId_ShouldReturnTimeSeries()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var startDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var endDate = new DateTimeOffset(2024, 1, 5, 0, 0, 0, TimeSpan.Zero);

        var expectedDto = new ProjectStatisticsTimeSeriesDto
        {
            ProjectId = projectId,
            DailySnapshots = new List<DailyStatisticsSnapshot>
            {
                new DailyStatisticsSnapshot
                {
                    Date = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
                    TotalTasks = 5,
                    CompletedTasks = 2,
                    InProgressTasks = 1,
                    InReviewTasks = 1,
                    TodoTasks = 1
                },
                new DailyStatisticsSnapshot
                {
                    Date = new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero),
                    TotalTasks = 5,
                    CompletedTasks = 3,
                    InProgressTasks = 1,
                    InReviewTasks = 0,
                    TodoTasks = 1
                }
            }
        };

        _repository.GetProjectStatisticsTimeSeriesAsync(
            projectId,
            startDate,
            endDate,
            Arg.Any<CancellationToken>())
            .Returns(expectedDto);

        var query = new GetProjectStatisticsTimeSeriesQuery(projectId, startDate, endDate);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(projectId, result.ProjectId);
        Assert.Equal(2, result.DailySnapshots.Count);
        Assert.Equal(5, result.DailySnapshots[0].TotalTasks);
        Assert.Equal(3, result.DailySnapshots[0].RemainingTasks); // 5 - 2

        await _repository.Received(1).GetProjectStatisticsTimeSeriesAsync(
            projectId,
            startDate,
            endDate,
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "存在しないプロジェクトIDでnullを返す")]
    public async Task Handle_WithNonExistentProjectId_ShouldReturnNull()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var startDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var endDate = new DateTimeOffset(2024, 1, 5, 0, 0, 0, TimeSpan.Zero);

        _repository.GetProjectStatisticsTimeSeriesAsync(
            projectId,
            startDate,
            endDate,
            Arg.Any<CancellationToken>())
            .Returns((ProjectStatisticsTimeSeriesDto?)null);

        var query = new GetProjectStatisticsTimeSeriesQuery(projectId, startDate, endDate);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);

        await _repository.Received(1).GetProjectStatisticsTimeSeriesAsync(
            projectId,
            startDate,
            endDate,
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "日付範囲がリポジトリに正しく渡される")]
    public async Task Handle_PassesDateRangeToRepository()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var startDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var endDate = new DateTimeOffset(2024, 1, 31, 0, 0, 0, TimeSpan.Zero);

        var query = new GetProjectStatisticsTimeSeriesQuery(projectId, startDate, endDate);

        // Act
        await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        await _repository.Received(1).GetProjectStatisticsTimeSeriesAsync(
            projectId,
            startDate,
            endDate,
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "RemainingTasksプロパティが正しく計算される")]
    public async Task Handle_RemainingTasksCalculatedCorrectly()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var startDate = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var endDate = new DateTimeOffset(2024, 1, 3, 0, 0, 0, TimeSpan.Zero);

        var expectedDto = new ProjectStatisticsTimeSeriesDto
        {
            ProjectId = projectId,
            DailySnapshots = new List<DailyStatisticsSnapshot>
            {
                new DailyStatisticsSnapshot
                {
                    Date = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero),
                    TotalTasks = 10,
                    CompletedTasks = 2,
                    InProgressTasks = 3,
                    InReviewTasks = 2,
                    TodoTasks = 3
                },
                new DailyStatisticsSnapshot
                {
                    Date = new DateTimeOffset(2024, 1, 2, 0, 0, 0, TimeSpan.Zero),
                    TotalTasks = 10,
                    CompletedTasks = 5,
                    InProgressTasks = 2,
                    InReviewTasks = 1,
                    TodoTasks = 2
                },
                new DailyStatisticsSnapshot
                {
                    Date = new DateTimeOffset(2024, 1, 3, 0, 0, 0, TimeSpan.Zero),
                    TotalTasks = 10,
                    CompletedTasks = 10,
                    InProgressTasks = 0,
                    InReviewTasks = 0,
                    TodoTasks = 0
                }
            }
        };

        _repository.GetProjectStatisticsTimeSeriesAsync(
            projectId,
            startDate,
            endDate,
            Arg.Any<CancellationToken>())
            .Returns(expectedDto);

        var query = new GetProjectStatisticsTimeSeriesQuery(projectId, startDate, endDate);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.DailySnapshots.Count);
        
        // 1日目: 10 - 2 = 8 残タスク
        Assert.Equal(8, result.DailySnapshots[0].RemainingTasks);
        
        // 2日目: 10 - 5 = 5 残タスク
        Assert.Equal(5, result.DailySnapshots[1].RemainingTasks);
        
        // 3日目: 10 - 10 = 0 残タスク
        Assert.Equal(0, result.DailySnapshots[2].RemainingTasks);
    }
}

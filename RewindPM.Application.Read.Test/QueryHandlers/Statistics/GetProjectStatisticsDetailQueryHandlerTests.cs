using NSubstitute;
using RewindPM.Application.Read.DTOs;
using RewindPM.Application.Read.Queries.Statistics;
using RewindPM.Application.Read.QueryHandlers.Statistics;
using RewindPM.Application.Read.Repositories;

namespace RewindPM.Application.Read.Test.QueryHandlers.Statistics;

public class GetProjectStatisticsDetailQueryHandlerTests
{
    private readonly IProjectStatisticsRepository _repository;
    private readonly GetProjectStatisticsDetailQueryHandler _handler;

    public GetProjectStatisticsDetailQueryHandlerTests()
    {
        _repository = Substitute.For<IProjectStatisticsRepository>();
        _handler = new GetProjectStatisticsDetailQueryHandler(_repository);
    }

    [Fact(DisplayName = "詳細統計クエリハンドラ: 有効なプロジェクトIDの場合、統計情報を返す")]
    public async Task Handle_WithValidProjectId_ShouldReturnStatistics()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var expectedDto = new ProjectStatisticsDetailDto
        {
            TotalTasks = 10,
            CompletedTasks = 5,
            InProgressTasks = 3,
            InReviewTasks = 1,
            TodoTasks = 1,
            TotalEstimatedHours = 100,
            TotalActualHours = 80,
            RemainingEstimatedHours = 60,
            OnTimeTasks = 4,
            DelayedTasks = 1,
            AverageDelayDays = 2.5,
            AccurateEstimateTasks = 0,
            OverEstimateTasks = 0,
            UnderEstimateTasks = 0,
            AverageEstimateErrorDays = 0,
            AsOfDate =  DateTimeOffset.UtcNow
        };

        _repository.GetProjectStatisticsDetailAsync(
            projectId,
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedDto);

        var query = new GetProjectStatisticsDetailQuery(projectId);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedDto.TotalTasks, result.TotalTasks);
        Assert.Equal(expectedDto.CompletedTasks, result.CompletedTasks);
        Assert.Equal(expectedDto.TotalEstimatedHours, result.TotalEstimatedHours);
        Assert.Equal(expectedDto.OnTimeTasks, result.OnTimeTasks);

        await _repository.Received(1).GetProjectStatisticsDetailAsync(
            projectId,
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "詳細統計クエリハンドラ: 基準日が指定された場合、リポジトリに日付を渡す")]
    public async Task Handle_WithSpecificAsOfDate_ShouldPassDateToRepository()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var asOfDate = new DateTimeOffset(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var expectedDto = new ProjectStatisticsDetailDto
        {
            TotalTasks = 5,
            CompletedTasks = 2,
            InProgressTasks = 2,
            InReviewTasks = 1,
            TodoTasks = 0,
            TotalEstimatedHours = 50,
            TotalActualHours = 40,
            RemainingEstimatedHours = 30,
            OnTimeTasks = 2,
            DelayedTasks = 0,
            AverageDelayDays = 0,
            AccurateEstimateTasks = 0,
            OverEstimateTasks = 0,
            UnderEstimateTasks = 0,
            AverageEstimateErrorDays = 0,
            AsOfDate = asOfDate
        };

        _repository.GetProjectStatisticsDetailAsync(
            projectId,
            asOfDate,
            Arg.Any<CancellationToken>())
            .Returns(expectedDto);

        var query = new GetProjectStatisticsDetailQuery(projectId, asOfDate);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(asOfDate, result.AsOfDate);

        await _repository.Received(1).GetProjectStatisticsDetailAsync(
            projectId,
            asOfDate,
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "詳細統計クエリハンドラ: 存在しないプロジェクトの場合、nullを返す")]
    public async Task Handle_WithNonExistentProject_ShouldReturnNull()
    {
        // Arrange
        var projectId = Guid.NewGuid();

        _repository.GetProjectStatisticsDetailAsync(
            projectId,
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>())
            .Returns((ProjectStatisticsDetailDto?)null);

        var query = new GetProjectStatisticsDetailQuery(projectId);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);

        await _repository.Received(1).GetProjectStatisticsDetailAsync(
            projectId,
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "詳細統計クエリハンドラ: 基準日が指定されない場合、現在日時を使用する")]
    public async Task Handle_WithoutAsOfDate_ShouldUseCurrentDate()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var beforeCall = DateTimeOffset.UtcNow;
        
        var expectedDto = new ProjectStatisticsDetailDto
        {
            TotalTasks = 10,
            CompletedTasks = 5,
            InProgressTasks = 3,
            InReviewTasks = 1,
            TodoTasks = 1,
            TotalEstimatedHours = 100,
            TotalActualHours = 80,
            RemainingEstimatedHours = 60,
            OnTimeTasks = 4,
            DelayedTasks = 1,
            AverageDelayDays = 2.5,
            AccurateEstimateTasks = 0,
            OverEstimateTasks = 0,
            UnderEstimateTasks = 0,
            AverageEstimateErrorDays = 0,
            AsOfDate = DateTimeOffset.UtcNow
        };

        _repository.GetProjectStatisticsDetailAsync(
            projectId,
            Arg.Any<DateTimeOffset>(),
            Arg.Any<CancellationToken>())
            .Returns(expectedDto);

        var query = new GetProjectStatisticsDetailQuery(projectId, null);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);
        var afterCall = DateTimeOffset.UtcNow;

        // Assert
        Assert.NotNull(result);

        await _repository.Received(1).GetProjectStatisticsDetailAsync(
            projectId,
            Arg.Is<DateTimeOffset>(d => d >= beforeCall && d <= afterCall),
            Arg.Any<CancellationToken>());
    }
}

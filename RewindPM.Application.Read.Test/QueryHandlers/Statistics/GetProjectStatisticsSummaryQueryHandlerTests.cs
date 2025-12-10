using NSubstitute;
using RewindPM.Application.Read.DTOs;
using RewindPM.Application.Read.Queries.Statistics;
using RewindPM.Application.Read.QueryHandlers.Statistics;
using RewindPM.Application.Read.Repositories;
using Xunit;

namespace RewindPM.Application.Read.Test.QueryHandlers.Statistics;

public class GetProjectStatisticsSummaryQueryHandlerTests
{
    [Fact(DisplayName = "Handle: リポジトリから統計情報を取得する")]
    public async Task Handle_FetchesStatisticsFromRepository()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var expectedDto = new ProjectStatisticsSummaryDto
        {
            ProjectId = projectId,
            TotalTasks = 10,
            CompletedTasks = 7,
            InProgressTasks = 2,
            InReviewTasks = 1,
            TodoTasks = 0
        };

        var mockRepository = Substitute.For<IProjectStatisticsRepository>();
        mockRepository
            .GetProjectStatisticsSummaryAsync(projectId, Arg.Any<CancellationToken>())
            .Returns(expectedDto);

        var handler = new GetProjectStatisticsSummaryQueryHandler(mockRepository);
        var query = new GetProjectStatisticsSummaryQuery(projectId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedDto.ProjectId, result.ProjectId);
        Assert.Equal(expectedDto.TotalTasks, result.TotalTasks);
        Assert.Equal(expectedDto.CompletedTasks, result.CompletedTasks);
        Assert.Equal(expectedDto.InProgressTasks, result.InProgressTasks);
        Assert.Equal(expectedDto.InReviewTasks, result.InReviewTasks);
        Assert.Equal(expectedDto.TodoTasks, result.TodoTasks);

        await mockRepository.Received(1)
            .GetProjectStatisticsSummaryAsync(projectId, Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Handle: CancellationTokenを正しく渡す")]
    public async Task Handle_PassesCancellationTokenCorrectly()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var cancellationToken = new CancellationToken();
        var expectedDto = new ProjectStatisticsSummaryDto
        {
            ProjectId = projectId,
            TotalTasks = 5,
            CompletedTasks = 3,
            InProgressTasks = 1,
            InReviewTasks = 1,
            TodoTasks = 0
        };

        var mockRepository = Substitute.For<IProjectStatisticsRepository>();
        mockRepository
            .GetProjectStatisticsSummaryAsync(projectId, cancellationToken)
            .Returns(expectedDto);

        var handler = new GetProjectStatisticsSummaryQueryHandler(mockRepository);
        var query = new GetProjectStatisticsSummaryQuery(projectId);

        // Act
        var result = await handler.Handle(query, cancellationToken);

        // Assert
        Assert.NotNull(result);
        await mockRepository.Received(1)
            .GetProjectStatisticsSummaryAsync(projectId, cancellationToken);
    }
}

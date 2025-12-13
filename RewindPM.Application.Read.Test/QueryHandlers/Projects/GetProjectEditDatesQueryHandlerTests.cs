using NSubstitute;
using RewindPM.Application.Read.Queries.Projects;
using RewindPM.Application.Read.QueryHandlers.Projects;
using RewindPM.Application.Read.Repositories;

namespace RewindPM.Application.Read.Test.QueryHandlers.Projects;

public class GetProjectEditDatesQueryHandlerTests
{
    private readonly IReadModelRepository _repository;
    private readonly GetProjectEditDatesQueryHandler _handler;

    public GetProjectEditDatesQueryHandlerTests()
    {
        _repository = Substitute.For<IReadModelRepository>();
        _handler = new GetProjectEditDatesQueryHandler(_repository);
    }

    [Fact(DisplayName = "プロジェクトの編集日一覧を降順（新しい順）で取得できること")]
    public async Task Handle_Descending_ShouldReturnEditDatesInDescendingOrder()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var editDates = new List<DateTimeOffset>
        {
            new DateTimeOffset(new DateTime(2025, 1, 5), TimeSpan.Zero),
            new DateTimeOffset(new DateTime(2025, 1, 3), TimeSpan.Zero),
            new DateTimeOffset(new DateTime(2025, 1, 2), TimeSpan.Zero)
        };

        _repository.GetProjectEditDatesAsync(projectId, false, Arg.Any<CancellationToken>())
            .Returns(editDates);
        var query = new GetProjectEditDatesQuery(projectId, Ascending: false);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(new DateTimeOffset(new DateTime(2025, 1, 5), TimeSpan.Zero), result[0]);
        Assert.Equal(new DateTimeOffset(new DateTime(2025, 1, 3), TimeSpan.Zero), result[1]);
        Assert.Equal(new DateTimeOffset(new DateTime(2025, 1, 2), TimeSpan.Zero), result[2]);
        await _repository.Received(1).GetProjectEditDatesAsync(
            projectId,
            false,
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "プロジェクトの編集日一覧を昇順（古い順）で取得できること")]
    public async Task Handle_Ascending_ShouldReturnEditDatesInAscendingOrder()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var editDates = new List<DateTimeOffset>
        {
            new DateTimeOffset(new DateTime(2025, 1, 2), TimeSpan.Zero),
            new DateTimeOffset(new DateTime(2025, 1, 3), TimeSpan.Zero),
            new DateTimeOffset(new DateTime(2025, 1, 5), TimeSpan.Zero)
        };

        _repository.GetProjectEditDatesAsync(projectId, true, Arg.Any<CancellationToken>())
            .Returns(editDates);
        var query = new GetProjectEditDatesQuery(projectId, Ascending: true);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(new DateTimeOffset(new DateTime(2025, 1, 2), TimeSpan.Zero), result[0]);
        Assert.Equal(new DateTimeOffset(new DateTime(2025, 1, 3), TimeSpan.Zero), result[1]);
        Assert.Equal(new DateTimeOffset(new DateTime(2025, 1, 5), TimeSpan.Zero), result[2]);
        await _repository.Received(1).GetProjectEditDatesAsync(
            projectId,
            true,
            Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "編集履歴が存在しない場合は空のリストを返すこと")]
    public async Task Handle_NoEditHistory_ShouldReturnEmptyList()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        _repository.GetProjectEditDatesAsync(projectId, false, Arg.Any<CancellationToken>())
            .Returns(new List<DateTimeOffset>());
        var query = new GetProjectEditDatesQuery(projectId, Ascending: false);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(result);
        await _repository.Received(1).GetProjectEditDatesAsync(
            projectId,
            false,
            Arg.Any<CancellationToken>());
    }
}

using NSubstitute;
using RewindPM.Application.Read.DTOs;
using RewindPM.Application.Read.Queries.Projects;
using RewindPM.Application.Read.QueryHandlers.Projects;
using RewindPM.Application.Read.Repositories;

namespace RewindPM.Application.Read.Test.QueryHandlers.Projects;

public class GetProjectAtTimeQueryHandlerTests
{
    private readonly IReadModelRepository _repository;
    private readonly GetProjectAtTimeQueryHandler _handler;

    public GetProjectAtTimeQueryHandlerTests()
    {
        _repository = Substitute.For<IReadModelRepository>();
        _handler = new GetProjectAtTimeQueryHandler(_repository);
    }

    [Fact(DisplayName = "指定された時点のプロジェクトを取得できること")]
    public async Task Handle_ExistingProjectAtTime_ShouldReturnProject()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var pointInTime = DateTimeOffset.UtcNow.AddDays(-1);
        var project = new ProjectDto
        {
            Id = projectId,
            Title = "Past Project",
            Description = "Past Description",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-2),
            UpdatedAt = pointInTime,
            CreatedBy = "user1",
            UpdatedBy = "user2"
        };

        _repository.GetProjectAtTimeAsync(projectId, pointInTime).Returns(project);
        var query = new GetProjectAtTimeQuery(projectId, pointInTime);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(projectId, result.Id);
        Assert.Equal("Past Project", result.Title);
        await _repository.Received(1).GetProjectAtTimeAsync(projectId, pointInTime);
    }

    [Fact(DisplayName = "指定された時点にプロジェクトが存在しない場合はnullを返すこと")]
    public async Task Handle_NonExistentProjectAtTime_ShouldReturnNull()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var pointInTime = DateTimeOffset.UtcNow.AddDays(-1);
        _repository.GetProjectAtTimeAsync(projectId, pointInTime).Returns((ProjectDto?)null);
        var query = new GetProjectAtTimeQuery(projectId, pointInTime);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
        await _repository.Received(1).GetProjectAtTimeAsync(projectId, pointInTime);
    }
}

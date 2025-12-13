using NSubstitute;
using RewindPM.Application.Read.DTOs;
using RewindPM.Application.Read.Queries.Projects;
using RewindPM.Application.Read.QueryHandlers.Projects;
using RewindPM.Application.Read.Repositories;

namespace RewindPM.Application.Read.Test.QueryHandlers.Projects;

public class GetAllProjectsQueryHandlerTests
{
    private readonly IReadModelRepository _repository;
    private readonly GetAllProjectsQueryHandler _handler;

    public GetAllProjectsQueryHandlerTests()
    {
        _repository = Substitute.For<IReadModelRepository>();
        _handler = new GetAllProjectsQueryHandler(_repository);
    }

    [Fact(DisplayName = "全プロジェクトを取得できること")]
    public async Task Handle_ShouldReturnAllProjects()
    {
        // Arrange
        var projects = new List<ProjectDto>
        {
            new ProjectDto
            {
                Id = Guid.NewGuid(),
                Title = "Project 1",
                Description = "Description 1",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = null,
                CreatedBy = "user1"
            },
            new ProjectDto
            {
                Id = Guid.NewGuid(),
                Title = "Project 2",
                Description = "Description 2",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = null,
                CreatedBy = "user2"
            }
        };

        _repository.GetAllProjectsAsync().Returns(projects);
        var query = new GetAllProjectsQuery();

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal(projects[0].Id, result[0].Id);
        Assert.Equal(projects[1].Id, result[1].Id);
        await _repository.Received(1).GetAllProjectsAsync();
    }

    [Fact(DisplayName = "プロジェクトが存在しない場合は空のリストを返すこと")]
    public async Task Handle_NoProjects_ShouldReturnEmptyList()
    {
        // Arrange
        _repository.GetAllProjectsAsync().Returns(new List<ProjectDto>());
        var query = new GetAllProjectsQuery();

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(result);
        await _repository.Received(1).GetAllProjectsAsync();
    }
}

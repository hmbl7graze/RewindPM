using NSubstitute;
using RewindPM.Application.Read.DTOs;
using RewindPM.Application.Read.Queries.Projects;
using RewindPM.Application.Read.QueryHandlers.Projects;
using RewindPM.Application.Read.Repositories;

namespace RewindPM.Application.Read.Test.QueryHandlers.Projects;

public class GetProjectByIdQueryHandlerTests
{
    private readonly IReadModelRepository _repository;
    private readonly GetProjectByIdQueryHandler _handler;

    public GetProjectByIdQueryHandlerTests()
    {
        _repository = Substitute.For<IReadModelRepository>();
        _handler = new GetProjectByIdQueryHandler(_repository);
    }

    [Fact(DisplayName = "指定されたIDのプロジェクトを取得できること")]
    public async Task Handle_ExistingProject_ShouldReturnProject()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var project = new ProjectDto
        {
            Id = projectId,
            Title = "Test Project",
            Description = "Test Description",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = null,
            CreatedBy = "user1"
        };

        _repository.GetProjectByIdAsync(projectId).Returns(project);
        var query = new GetProjectByIdQuery(projectId);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(projectId, result.Id);
        Assert.Equal("Test Project", result.Title);
        await _repository.Received(1).GetProjectByIdAsync(projectId);
    }

    [Fact(DisplayName = "存在しないプロジェクトの場合はnullを返すこと")]
    public async Task Handle_NonExistentProject_ShouldReturnNull()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        _repository.GetProjectByIdAsync(projectId).Returns((ProjectDto?)null);
        var query = new GetProjectByIdQuery(projectId);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
        await _repository.Received(1).GetProjectByIdAsync(projectId);
    }
}

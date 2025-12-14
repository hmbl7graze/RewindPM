using NSubstitute;
using RewindPM.Application.Read.DTOs;
using RewindPM.Application.Read.Queries.Tasks;
using RewindPM.Application.Read.QueryHandlers.Tasks;
using RewindPM.Application.Read.Repositories;
using TaskStatus = RewindPM.Domain.ValueObjects.TaskStatus;

namespace RewindPM.Application.Read.Test.QueryHandlers.Tasks;

public class GetTasksByProjectIdQueryHandlerTests
{
    private readonly IReadModelRepository _repository;
    private readonly GetTasksByProjectIdQueryHandler _handler;

    public GetTasksByProjectIdQueryHandlerTests()
    {
        _repository = Substitute.For<IReadModelRepository>();
        _handler = new GetTasksByProjectIdQueryHandler(_repository);
    }

    [Fact(DisplayName = "指定されたプロジェクトの全タスクを取得できること")]
    public async Task Handle_ShouldReturnTasksByProjectId()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var tasks = new List<TaskDto>
        {
            new TaskDto
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Title = "Task 1",
                Description = "Description 1",
                Status = TaskStatus.Todo,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = null,
                CreatedBy = "user1"
            },
            new TaskDto
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Title = "Task 2",
                Description = "Description 2",
                Status = TaskStatus.InProgress,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = null,
                CreatedBy = "user1"
            }
        };

        _repository.GetTasksByProjectIdAsync(projectId).Returns(tasks);
        var query = new GetTasksByProjectIdQuery(projectId);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, t => Assert.Equal(projectId, t.ProjectId));
        await _repository.Received(1).GetTasksByProjectIdAsync(projectId);
    }

    [Fact(DisplayName = "タスクが存在しない場合は空のリストを返すこと")]
    public async Task Handle_NoTasks_ShouldReturnEmptyList()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        _repository.GetTasksByProjectIdAsync(projectId).Returns(new List<TaskDto>());
        var query = new GetTasksByProjectIdQuery(projectId);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(result);
        await _repository.Received(1).GetTasksByProjectIdAsync(projectId);
    }
}

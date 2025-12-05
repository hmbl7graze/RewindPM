using NSubstitute;
using RewindPM.Application.Read.DTOs;
using RewindPM.Application.Read.Queries.Tasks;
using RewindPM.Application.Read.QueryHandlers.Tasks;
using RewindPM.Application.Read.Repositories;
using TaskStatus = RewindPM.Domain.ValueObjects.TaskStatus;

namespace RewindPM.Application.Read.Test.QueryHandlers.Tasks;

public class GetTasksByProjectIdAtTimeQueryHandlerTests
{
    private readonly IReadModelRepository _repository;
    private readonly GetTasksByProjectIdAtTimeQueryHandler _handler;

    public GetTasksByProjectIdAtTimeQueryHandlerTests()
    {
        _repository = Substitute.For<IReadModelRepository>();
        _handler = new GetTasksByProjectIdAtTimeQueryHandler(_repository);
    }

    [Fact(DisplayName = "指定された時点のプロジェクトの全タスクを取得できること")]
    public async Task Handle_ShouldReturnTasksByProjectIdAtTime()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var pointInTime = DateTime.UtcNow.AddDays(-1);
        var tasks = new List<TaskDto>
        {
            new TaskDto
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Title = "Past Task 1",
                Description = "Description 1",
                Status = TaskStatus.Todo,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                UpdatedAt = pointInTime,
                CreatedBy = "user1"
            },
            new TaskDto
            {
                Id = Guid.NewGuid(),
                ProjectId = projectId,
                Title = "Past Task 2",
                Description = "Description 2",
                Status = TaskStatus.Done,
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                UpdatedAt = pointInTime,
                CreatedBy = "user1"
            }
        };

        _repository.GetTasksByProjectIdAtTimeAsync(projectId, pointInTime).Returns(tasks);
        var query = new GetTasksByProjectIdAtTimeQuery(projectId, pointInTime);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, t => Assert.Equal(projectId, t.ProjectId));
        await _repository.Received(1).GetTasksByProjectIdAtTimeAsync(projectId, pointInTime);
    }

    [Fact(DisplayName = "指定された時点にタスクが存在しない場合は空のリストを返すこと")]
    public async Task Handle_NoTasksAtTime_ShouldReturnEmptyList()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var pointInTime = DateTime.UtcNow.AddDays(-1);
        _repository.GetTasksByProjectIdAtTimeAsync(projectId, pointInTime).Returns(new List<TaskDto>());
        var query = new GetTasksByProjectIdAtTimeQuery(projectId, pointInTime);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(result);
        await _repository.Received(1).GetTasksByProjectIdAtTimeAsync(projectId, pointInTime);
    }
}

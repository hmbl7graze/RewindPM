using NSubstitute;
using RewindPM.Application.Read.DTOs;
using RewindPM.Application.Read.Queries.Tasks;
using RewindPM.Application.Read.QueryHandlers.Tasks;
using RewindPM.Application.Read.Repositories;
using TaskStatus = RewindPM.Domain.ValueObjects.TaskStatus;

namespace RewindPM.Application.Read.Test.QueryHandlers.Tasks;

public class GetTaskByIdQueryHandlerTests
{
    private readonly IReadModelRepository _repository;
    private readonly GetTaskByIdQueryHandler _handler;

    public GetTaskByIdQueryHandlerTests()
    {
        _repository = Substitute.For<IReadModelRepository>();
        _handler = new GetTaskByIdQueryHandler(_repository);
    }

    [Fact(DisplayName = "指定されたIDのタスクを取得できること")]
    public async Task Handle_ExistingTask_ShouldReturnTask()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = new TaskDto
        {
            Id = taskId,
            ProjectId = Guid.NewGuid(),
            Title = "Test Task",
            Description = "Test Description",
            Status = TaskStatus.Todo,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = null,
            CreatedBy = "user1"
        };

        _repository.GetTaskByIdAsync(taskId).Returns(task);
        var query = new GetTaskByIdQuery(taskId);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(taskId, result.Id);
        Assert.Equal("Test Task", result.Title);
        await _repository.Received(1).GetTaskByIdAsync(taskId);
    }

    [Fact(DisplayName = "存在しないタスクの場合はnullを返すこと")]
    public async Task Handle_NonExistentTask_ShouldReturnNull()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        _repository.GetTaskByIdAsync(taskId).Returns((TaskDto?)null);
        var query = new GetTaskByIdQuery(taskId);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
        await _repository.Received(1).GetTaskByIdAsync(taskId);
    }
}

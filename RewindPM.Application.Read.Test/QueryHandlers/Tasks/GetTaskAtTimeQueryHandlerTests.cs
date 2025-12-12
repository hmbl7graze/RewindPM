using NSubstitute;
using RewindPM.Application.Read.DTOs;
using RewindPM.Application.Read.Queries.Tasks;
using RewindPM.Application.Read.QueryHandlers.Tasks;
using RewindPM.Application.Read.Repositories;
using TaskStatus = RewindPM.Domain.ValueObjects.TaskStatus;

namespace RewindPM.Application.Read.Test.QueryHandlers.Tasks;

public class GetTaskAtTimeQueryHandlerTests
{
    private readonly IReadModelRepository _repository;
    private readonly GetTaskAtTimeQueryHandler _handler;

    public GetTaskAtTimeQueryHandlerTests()
    {
        _repository = Substitute.For<IReadModelRepository>();
        _handler = new GetTaskAtTimeQueryHandler(_repository);
    }

    [Fact(DisplayName = "指定された時点のタスクを取得できること")]
    public async Task Handle_ExistingTaskAtTime_ShouldReturnTask()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var pointInTime = DateTimeOffset.UtcNow.AddDays(-1);
        var task = new TaskDto
        {
            Id = taskId,
            ProjectId = Guid.NewGuid(),
            Title = "Past Task",
            Description = "Past Description",
            Status = TaskStatus.InProgress,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-2),
            UpdatedAt = pointInTime,
            CreatedBy = "user1",
            UpdatedBy = "user2"
        };

        _repository.GetTaskAtTimeAsync(taskId, pointInTime).Returns(task);
        var query = new GetTaskAtTimeQuery(taskId, pointInTime);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(taskId, result.Id);
        Assert.Equal("Past Task", result.Title);
        Assert.Equal(TaskStatus.InProgress, result.Status);
        await _repository.Received(1).GetTaskAtTimeAsync(taskId, pointInTime);
    }

    [Fact(DisplayName = "指定された時点にタスクが存在しない場合はnullを返すこと")]
    public async Task Handle_NonExistentTaskAtTime_ShouldReturnNull()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var pointInTime = DateTimeOffset.UtcNow.AddDays(-1);
        _repository.GetTaskAtTimeAsync(taskId, pointInTime).Returns((TaskDto?)null);
        var query = new GetTaskAtTimeQuery(taskId, pointInTime);

        // Act
        var result = await _handler.Handle(query, TestContext.Current.CancellationToken);

        // Assert
        Assert.Null(result);
        await _repository.Received(1).GetTaskAtTimeAsync(taskId, pointInTime);
    }
}

using NSubstitute;
using RewindPM.Application.Write.CommandHandlers.Tasks;
using RewindPM.Application.Write.Commands.Tasks;
using RewindPM.Application.Write.Repositories;
using RewindPM.Domain.Aggregates;
using RewindPM.Domain.ValueObjects;
using TaskStatus = RewindPM.Domain.ValueObjects.TaskStatus;

namespace RewindPM.Application.Write.Test.CommandHandlers.Tasks;

public class ChangeTaskStatusCommandHandlerTests
{
    private readonly IAggregateRepository _repository;
    private readonly ChangeTaskStatusCommandHandler _handler;

    public ChangeTaskStatusCommandHandlerTests()
    {
        _repository = Substitute.For<IAggregateRepository>();
        _handler = new ChangeTaskStatusCommandHandler(_repository);
    }

    [Fact(DisplayName = "既存のタスクのステータスを変更できること")]
    public async Task Handle_ExistingTask_ShouldChangeStatus()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var existingTask = TaskAggregate.Create(
            taskId,
            Guid.NewGuid(),
            "Test Task",
            "Description",
            new ScheduledPeriod(DateTime.UtcNow, DateTime.UtcNow.AddDays(7), 40),
            "user1"
        );

        _repository.GetByIdAsync<TaskAggregate>(taskId)
            .Returns(existingTask);

        var command = new ChangeTaskStatusCommand(
            taskId,
            TaskStatus.InProgress,
            "user1"
        );

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        await _repository.Received(1).GetByIdAsync<TaskAggregate>(taskId);
        await _repository.Received(1).SaveAsync(Arg.Is<TaskAggregate>(t =>
            t.Id == taskId &&
            t.Status == TaskStatus.InProgress
        ));
    }

    [Fact(DisplayName = "存在しないタスクのステータス変更時にInvalidOperationExceptionをスローすること")]
    public async Task Handle_NonExistentTask_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        _repository.GetByIdAsync<TaskAggregate>(taskId)
            .Returns((TaskAggregate?)null);

        var command = new ChangeTaskStatusCommand(
            taskId,
            TaskStatus.InProgress,
            "user1"
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _handler.Handle(command, TestContext.Current.CancellationToken)
        );

        Assert.Equal($"タスク（ID: {taskId}）が見つかりません", exception.Message);
        await _repository.DidNotReceive().SaveAsync(Arg.Any<TaskAggregate>());
    }
}

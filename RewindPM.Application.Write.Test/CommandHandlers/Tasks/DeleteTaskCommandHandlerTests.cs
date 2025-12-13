using NSubstitute;
using RewindPM.Application.Write.CommandHandlers.Tasks;
using RewindPM.Application.Write.Commands.Tasks;
using RewindPM.Application.Write.Repositories;
using RewindPM.Domain.Aggregates;
using RewindPM.Domain.Common;
using RewindPM.Domain.ValueObjects;

namespace RewindPM.Application.Write.Test.CommandHandlers.Tasks;

public class DeleteTaskCommandHandlerTests
{
    private readonly IAggregateRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly DeleteTaskCommandHandler _handler;

    public DeleteTaskCommandHandlerTests()
    {
        _repository = Substitute.For<IAggregateRepository>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);
        _handler = new DeleteTaskCommandHandler(_repository, _dateTimeProvider);
    }

    [Fact(DisplayName = "有効なコマンドでタスクを削除すること")]
    public async Task Handle_ValidCommand_ShouldDeleteTask()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var command = new DeleteTaskCommand(
            taskId,
            "user1"
        );

        var scheduledPeriod = new ScheduledPeriod(
            new DateTime(2025, 1, 1),
            new DateTime(2025, 1, 10),
            40
        );

        var existingTask = TaskAggregate.Create(
            taskId,
            projectId,
            "Test Task",
            "Test Description",
            scheduledPeriod,
            "user1",
            _dateTimeProvider
        );
        existingTask.ClearUncommittedEvents();

        _repository.GetByIdAsync<TaskAggregate>(taskId)
            .Returns(existingTask);

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        await _repository.Received(1).GetByIdAsync<TaskAggregate>(taskId);
        await _repository.Received(1).SaveAsync(Arg.Is<TaskAggregate>(t =>
            t.Id == taskId &&
            t.UncommittedEvents.Count == 1
        ));
    }

    [Fact(DisplayName = "存在しないタスクを削除しようとした場合に例外をスローすること")]
    public async Task Handle_NonExistentTask_ShouldThrowException()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var command = new DeleteTaskCommand(
            taskId,
            "user1"
        );

        _repository.GetByIdAsync<TaskAggregate>(taskId)
            .Returns((TaskAggregate?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _handler.Handle(command, TestContext.Current.CancellationToken)
        );

        await _repository.Received(1).GetByIdAsync<TaskAggregate>(taskId);
        await _repository.DidNotReceive().SaveAsync(Arg.Any<TaskAggregate>());
    }
}

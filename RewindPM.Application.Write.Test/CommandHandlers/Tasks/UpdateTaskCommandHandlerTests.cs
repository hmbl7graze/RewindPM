using NSubstitute;
using RewindPM.Application.Write.CommandHandlers.Tasks;
using RewindPM.Application.Write.Commands.Tasks;
using RewindPM.Application.Write.Repositories;
using RewindPM.Domain.Aggregates;
using RewindPM.Domain.Common;
using RewindPM.Domain.ValueObjects;

namespace RewindPM.Application.Write.Test.CommandHandlers.Tasks;

public class UpdateTaskCommandHandlerTests
{
    private readonly IAggregateRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly UpdateTaskCommandHandler _handler;

    public UpdateTaskCommandHandlerTests()
    {
        _repository = Substitute.For<IAggregateRepository>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);
        _handler = new UpdateTaskCommandHandler(_repository, _dateTimeProvider);
    }

    [Fact(DisplayName = "既存のタスクを更新できること")]
    public async Task Handle_ExistingTask_ShouldUpdateTask()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var task = TaskAggregate.Create(
            taskId,
            projectId,
            "Old Title",
            "Old Description",
            new ScheduledPeriod(DateTime.UtcNow, DateTime.UtcNow.AddDays(7), 40),
            "user1",
            _dateTimeProvider
        );

        _repository.GetByIdAsync<TaskAggregate>(taskId)
            .Returns(task);

        var command = new UpdateTaskCommand(
            taskId,
            "New Title",
            "New Description",
            "user2"
        );

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("New Title", task.Title);
        Assert.Equal("New Description", task.Description);

        await _repository.Received(1).SaveAsync(task);
    }

    [Fact(DisplayName = "存在しないタスクの更新時は例外をスローすること")]
    public async Task Handle_NonExistentTask_ShouldThrowException()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        _repository.GetByIdAsync<TaskAggregate>(taskId)
            .Returns((TaskAggregate?)null);

        var command = new UpdateTaskCommand(
            taskId,
            "New Title",
            "New Description",
            "user1"
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _handler.Handle(command, TestContext.Current.CancellationToken));

        Assert.Contains("タスク", exception.Message);
        Assert.Contains("見つかりません", exception.Message);
    }

    [Fact(DisplayName = "UpdateTaskCommandHandlerがIDateTimeProviderを使用すること")]
    public async Task Handle_ShouldUseDateTimeProvider()
    {
        // Arrange
        var fixedTime = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        _dateTimeProvider.UtcNow.Returns(fixedTime);

        var taskId = Guid.NewGuid();
        var task = TaskAggregate.Create(
            taskId,
            Guid.NewGuid(),
            "Old Title",
            "Old Description",
            new ScheduledPeriod(DateTime.UtcNow, DateTime.UtcNow.AddDays(7), 40),
            "user1",
            _dateTimeProvider
        );
        task.ClearUncommittedEvents();

        _repository.GetByIdAsync<TaskAggregate>(taskId).Returns(task);

        var command = new UpdateTaskCommand(
            taskId,
            "New Title",
            "New Description",
            "user1"
        );

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var updatedEvent = task.UncommittedEvents.First() as RewindPM.Domain.Events.TaskUpdated;
        Assert.NotNull(updatedEvent);
        Assert.Equal(fixedTime, updatedEvent.OccurredAt);
    }
}

using NSubstitute;
using RewindPM.Application.Write.CommandHandlers.Tasks;
using RewindPM.Application.Write.Commands.Tasks;
using RewindPM.Application.Write.Repositories;
using RewindPM.Domain.Aggregates;
using RewindPM.Domain.Common;
using RewindPM.Domain.Events;
using RewindPM.Domain.ValueObjects;
using TaskStatus = RewindPM.Domain.ValueObjects.TaskStatus;

namespace RewindPM.Application.Write.Test.CommandHandlers.Tasks;

public class UpdateTaskCompleteCommandHandlerTests
{
    private readonly IAggregateRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly UpdateTaskCompleteCommandHandler _handler;

    public UpdateTaskCompleteCommandHandlerTests()
    {
        _repository = Substitute.For<IAggregateRepository>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);
        _handler = new UpdateTaskCompleteCommandHandler(_repository, _dateTimeProvider);
    }

    [Fact(DisplayName = "既存のタスクを一括更新できること")]
    public async Task Handle_ExistingTask_ShouldUpdateTaskCompletely()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var oldScheduledPeriod = new ScheduledPeriod(
            new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2025, 1, 10, 0, 0, 0, TimeSpan.Zero),
            40);
        
        var task = TaskAggregate.Create(
            taskId,
            projectId,
            "Old Title",
            "Old Description",
            oldScheduledPeriod,
            "user1",
            _dateTimeProvider
        );
        task.ClearUncommittedEvents();

        _repository.GetByIdAsync<TaskAggregate>(taskId)
            .Returns(task);

        var newScheduledPeriod = new ScheduledPeriod(
            new DateTimeOffset(2025, 2, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2025, 2, 15, 0, 0, 0, TimeSpan.Zero),
            60);
        var newActualPeriod = new ActualPeriod(
            new DateTimeOffset(2025, 2, 2, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2025, 2, 10, 0, 0, 0, TimeSpan.Zero),
            40);

        var command = new UpdateTaskCompleteCommand(
            taskId,
            "New Title",
            "New Description",
            TaskStatus.InProgress,
            new DateTimeOffset(2025, 2, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2025, 2, 15, 0, 0, 0, TimeSpan.Zero),
            60,
            new DateTimeOffset(2025, 2, 2, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2025, 2, 10, 0, 0, 0, TimeSpan.Zero),
            40,
            "user2"
        );

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("New Title", task.Title);
        Assert.Equal("New Description", task.Description);
        Assert.Equal(TaskStatus.InProgress, task.Status);
        Assert.Equal(newScheduledPeriod, task.ScheduledPeriod);
        Assert.Equal(newActualPeriod, task.ActualPeriod);
        Assert.Equal("user2", task.UpdatedBy);

        await _repository.Received(1).SaveAsync(task);
    }

    [Fact(DisplayName = "存在しないタスクの更新時は例外をスローすること")]
    public async Task Handle_NonExistentTask_ShouldThrowException()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        _repository.GetByIdAsync<TaskAggregate>(taskId)
            .Returns((TaskAggregate?)null);

        var command = new UpdateTaskCompleteCommand(
            taskId,
            "New Title",
            "New Description",
            TaskStatus.InProgress,
            new DateTimeOffset(2025, 2, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2025, 2, 15, 0, 0, 0, TimeSpan.Zero),
            60,
            null,
            null,
            null,
            "user1"
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _handler.Handle(command, TestContext.Current.CancellationToken));

        Assert.Contains("タスク", exception.Message);
        Assert.Contains("見つかりません", exception.Message);
    }

    [Fact(DisplayName = "TaskCompletelyUpdatedイベントが発行されること")]
    public async Task Handle_ShouldRaiseTaskCompletelyUpdatedEvent()
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
            new ScheduledPeriod(
                new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2025, 1, 10, 0, 0, 0, TimeSpan.Zero),
                40),
            "user1",
            _dateTimeProvider
        );
        task.ClearUncommittedEvents();

        _repository.GetByIdAsync<TaskAggregate>(taskId).Returns(task);

        var command = new UpdateTaskCompleteCommand(
            taskId,
            "New Title",
            "New Description",
            TaskStatus.InProgress,
            new DateTimeOffset(2025, 2, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2025, 2, 15, 0, 0, 0, TimeSpan.Zero),
            60,
            new DateTimeOffset(2025, 2, 2, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2025, 2, 10, 0, 0, 0, TimeSpan.Zero),
            40,
            "user2"
        );

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.Single(task.UncommittedEvents);
        var updatedEvent = task.UncommittedEvents.First() as TaskCompletelyUpdated;
        Assert.NotNull(updatedEvent);
        Assert.Equal("New Title", updatedEvent.Title);
        Assert.Equal("New Description", updatedEvent.Description);
        Assert.Equal(TaskStatus.InProgress, updatedEvent.Status);
        Assert.Equal(fixedTime, updatedEvent.OccurredAt);
        Assert.Equal("user2", updatedEvent.UpdatedBy);
    }

    [Fact(DisplayName = "変更がない場合はイベントが発行されないこと")]
    public async Task Handle_WithNoChanges_ShouldNotRaiseEvent()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var scheduledPeriod = new ScheduledPeriod(
            new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2025, 1, 10, 0, 0, 0, TimeSpan.Zero),
            40);
        
        var task = TaskAggregate.Create(
            taskId,
            Guid.NewGuid(),
            "Title",
            "Description",
            scheduledPeriod,
            "user1",
            _dateTimeProvider
        );
        task.ClearUncommittedEvents();

        _repository.GetByIdAsync<TaskAggregate>(taskId).Returns(task);

        var command = new UpdateTaskCompleteCommand(
            taskId,
            "Title",
            "Description",
            TaskStatus.Todo,
            new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2025, 1, 10, 0, 0, 0, TimeSpan.Zero),
            40,
            null,
            null,
            null,
            "user2"
        );

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(task.UncommittedEvents);
        await _repository.Received(1).SaveAsync(task);
    }

    [Fact(DisplayName = "null値を含むActualPeriodを正しく処理できること")]
    public async Task Handle_WithNullActualPeriod_ShouldHandleCorrectly()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var scheduledPeriod = new ScheduledPeriod(
            new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2025, 1, 10, 0, 0, 0, TimeSpan.Zero),
            40);
        
        var task = TaskAggregate.Create(
            taskId,
            Guid.NewGuid(),
            "Old Title",
            "Old Description",
            scheduledPeriod,
            "user1",
            _dateTimeProvider
        );
        task.ClearUncommittedEvents();

        _repository.GetByIdAsync<TaskAggregate>(taskId).Returns(task);

        var command = new UpdateTaskCompleteCommand(
            taskId,
            "New Title",
            "New Description",
            TaskStatus.InProgress,
            new DateTimeOffset(2025, 2, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2025, 2, 15, 0, 0, 0, TimeSpan.Zero),
            60,
            null,
            null,
            null,
            "user2"
        );

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal("New Title", task.Title);
        Assert.NotNull(task.ActualPeriod);
        Assert.Null(task.ActualPeriod.StartDate);
        Assert.Null(task.ActualPeriod.EndDate);
        Assert.Null(task.ActualPeriod.ActualHours);

        await _repository.Received(1).SaveAsync(task);
    }
}

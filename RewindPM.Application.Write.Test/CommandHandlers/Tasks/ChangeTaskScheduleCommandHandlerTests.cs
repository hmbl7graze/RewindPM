using NSubstitute;
using RewindPM.Application.Write.CommandHandlers.Tasks;
using RewindPM.Application.Write.Commands.Tasks;
using RewindPM.Application.Write.Repositories;
using RewindPM.Domain.Aggregates;
using RewindPM.Domain.Common;
using RewindPM.Domain.ValueObjects;

namespace RewindPM.Application.Write.Test.CommandHandlers.Tasks;

public class ChangeTaskScheduleCommandHandlerTests
{
    private readonly IAggregateRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ChangeTaskScheduleCommandHandler _handler;

    public ChangeTaskScheduleCommandHandlerTests()
    {
        _repository = Substitute.For<IAggregateRepository>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);
        _handler = new ChangeTaskScheduleCommandHandler(_repository, _dateTimeProvider);
    }

    [Fact(DisplayName = "既存のタスクの予定期間を変更できること")]
    public async Task Handle_ExistingTask_ShouldChangeSchedule()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var oldSchedule = new ScheduledPeriod(DateTime.UtcNow, DateTime.UtcNow.AddDays(7), 40);
        var task = TaskAggregate.Create(
            taskId,
            projectId,
            "Test Task",
            "Description",
            oldSchedule,
            "user1",
            _dateTimeProvider
        );

        _repository.GetByIdAsync<TaskAggregate>(taskId)
            .Returns(task);

        var newStartDate = DateTimeOffset.UtcNow.AddDays(5);
        var newEndDate = DateTimeOffset.UtcNow.AddDays(15);
        var newEstimatedHours = 60;

        var command = new ChangeTaskScheduleCommand(
            taskId,
            newStartDate,
            newEndDate,
            newEstimatedHours,
            "user2"
        );

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(newStartDate, task.ScheduledPeriod.StartDate);
        Assert.Equal(newEndDate, task.ScheduledPeriod.EndDate);
        Assert.Equal(newEstimatedHours, task.ScheduledPeriod.EstimatedHours);

        await _repository.Received(1).SaveAsync(task);
    }

    [Fact(DisplayName = "存在しないタスクの予定期間変更時は例外をスローすること")]
    public async Task Handle_NonExistentTask_ShouldThrowException()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        _repository.GetByIdAsync<TaskAggregate>(taskId)
            .Returns((TaskAggregate?)null);

        var command = new ChangeTaskScheduleCommand(
            taskId,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(7),
            40,
            "user1"
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _handler.Handle(command, TestContext.Current.CancellationToken));

        Assert.Contains("タスク", exception.Message);
        Assert.Contains("見つかりません", exception.Message);
    }

    [Fact(DisplayName = "ChangeTaskScheduleCommandHandlerがIDateTimeProviderを使用すること")]
    public async Task Handle_ShouldUseDateTimeProvider()
    {
        // Arrange
        var fixedTime = new DateTimeOffset(new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc));
        _dateTimeProvider.UtcNow.Returns(fixedTime);

        var taskId = Guid.NewGuid();
        var task = TaskAggregate.Create(
            taskId,
            Guid.NewGuid(),
            "Test Task",
            "Description",
            new ScheduledPeriod(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(7), 40),
            "user1",
            _dateTimeProvider
        );
        task.ClearUncommittedEvents();

        _repository.GetByIdAsync<TaskAggregate>(taskId).Returns(task);

        var command = new ChangeTaskScheduleCommand(
            taskId,
            DateTimeOffset.UtcNow.AddDays(5),
            DateTimeOffset.UtcNow.AddDays(15),
            60,
            "user1"
        );

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var scheduledEvent = task.UncommittedEvents.First() as RewindPM.Domain.Events.TaskScheduledPeriodChanged;
        Assert.NotNull(scheduledEvent);
        Assert.Equal(fixedTime, scheduledEvent.OccurredAt);
    }
}

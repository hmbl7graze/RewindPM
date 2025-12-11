using NSubstitute;
using RewindPM.Application.Write.CommandHandlers.Tasks;
using RewindPM.Application.Write.Commands.Tasks;
using RewindPM.Application.Write.Repositories;
using RewindPM.Domain.Aggregates;
using RewindPM.Domain.Common;
using RewindPM.Domain.ValueObjects;

namespace RewindPM.Application.Write.Test.CommandHandlers.Tasks;

public class ChangeTaskActualPeriodCommandHandlerTests
{
    private readonly IAggregateRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ChangeTaskActualPeriodCommandHandler _handler;

    public ChangeTaskActualPeriodCommandHandlerTests()
    {
        _repository = Substitute.For<IAggregateRepository>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);
        _handler = new ChangeTaskActualPeriodCommandHandler(_repository, _dateTimeProvider);
    }

    [Fact(DisplayName = "既存のタスクの実績期間を変更できること")]
    public async Task Handle_ExistingTask_ShouldChangeActualPeriod()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var task = TaskAggregate.Create(
            taskId,
            projectId,
            "Test Task",
            "Description",
            new ScheduledPeriod(DateTime.UtcNow, DateTime.UtcNow.AddDays(7), 40),
            "user1",
            _dateTimeProvider
        );

        _repository.GetByIdAsync<TaskAggregate>(taskId)
            .Returns(task);

        var actualStartDate = DateTime.UtcNow;
        var actualEndDate = DateTime.UtcNow.AddDays(8);
        var actualHours = 50;

        var command = new ChangeTaskActualPeriodCommand(
            taskId,
            actualStartDate,
            actualEndDate,
            actualHours,
            "user2"
        );

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(task.ActualPeriod);
        Assert.Equal(actualStartDate, task.ActualPeriod.StartDate);
        Assert.Equal(actualEndDate, task.ActualPeriod.EndDate);
        Assert.Equal(actualHours, task.ActualPeriod.ActualHours);

        await _repository.Received(1).SaveAsync(task);
    }

    [Fact(DisplayName = "実績開始日のみ設定できること")]
    public async Task Handle_OnlyActualStartDate_ShouldSetStartDateOnly()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var task = TaskAggregate.Create(
            taskId,
            Guid.NewGuid(),
            "Test Task",
            "Description",
            new ScheduledPeriod(DateTime.UtcNow, DateTime.UtcNow.AddDays(7), 40),
            "user1",
            _dateTimeProvider
        );

        _repository.GetByIdAsync<TaskAggregate>(taskId).Returns(task);

        var actualStartDate = DateTime.UtcNow;
        var command = new ChangeTaskActualPeriodCommand(
            taskId,
            actualStartDate,
            null,
            null,
            "user1"
        );

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(task.ActualPeriod);
        Assert.Equal(actualStartDate, task.ActualPeriod.StartDate);
        Assert.Null(task.ActualPeriod.EndDate);
        Assert.Null(task.ActualPeriod.ActualHours);
    }

    [Fact(DisplayName = "存在しないタスクの実績期間変更時は例外をスローすること")]
    public async Task Handle_NonExistentTask_ShouldThrowException()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        _repository.GetByIdAsync<TaskAggregate>(taskId)
            .Returns((TaskAggregate?)null);

        var command = new ChangeTaskActualPeriodCommand(
            taskId,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(7),
            40,
            "user1"
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _handler.Handle(command, TestContext.Current.CancellationToken));

        Assert.Contains("タスク", exception.Message);
        Assert.Contains("見つかりません", exception.Message);
    }

    [Fact(DisplayName = "ChangeTaskActualPeriodCommandHandlerがIDateTimeProviderを使用すること")]
    public async Task Handle_ShouldUseDateTimeProvider()
    {
        // Arrange
        var fixedTime = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        _dateTimeProvider.UtcNow.Returns(fixedTime);

        var taskId = Guid.NewGuid();
        var task = TaskAggregate.Create(
            taskId,
            Guid.NewGuid(),
            "Test Task",
            "Description",
            new ScheduledPeriod(DateTime.UtcNow, DateTime.UtcNow.AddDays(7), 40),
            "user1",
            _dateTimeProvider
        );
        task.ClearUncommittedEvents();

        _repository.GetByIdAsync<TaskAggregate>(taskId).Returns(task);

        var command = new ChangeTaskActualPeriodCommand(
            taskId,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(8),
            50,
            "user1"
        );

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        var actualEvent = task.UncommittedEvents.First() as RewindPM.Domain.Events.TaskActualPeriodChanged;
        Assert.NotNull(actualEvent);
        Assert.Equal(fixedTime, actualEvent.OccurredAt);
    }
}

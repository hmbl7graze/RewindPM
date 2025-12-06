using NSubstitute;
using RewindPM.Application.Write.CommandHandlers.Tasks;
using RewindPM.Application.Write.Commands.Tasks;
using RewindPM.Application.Write.Repositories;
using RewindPM.Domain.Aggregates;

namespace RewindPM.Application.Write.Test.CommandHandlers.Tasks;

public class CreateTaskCommandHandlerTests
{
    private readonly IAggregateRepository _repository;
    private readonly CreateTaskCommandHandler _handler;

    public CreateTaskCommandHandlerTests()
    {
        _repository = Substitute.For<IAggregateRepository>();
        _handler = new CreateTaskCommandHandler(_repository);
    }

    [Fact(DisplayName = "有効なコマンドでタスクを作成し、IDを返すこと")]
    public async Task Handle_ValidCommand_ShouldCreateTaskAndReturnId()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var command = new CreateTaskCommand(
            taskId,
            projectId,
            "Test Task",
            "Test Description",
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(7),
            40,
            "user1"
        );

        // Act
        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(taskId, result);

        await _repository.Received(1).SaveAsync(Arg.Is<TaskAggregate>(t =>
            t.Id == taskId &&
            t.ProjectId == projectId &&
            t.Title == "Test Task" &&
            t.Description == "Test Description" &&
            t.CreatedBy == "user1"
        ));
    }

    [Fact(DisplayName = "有効なコマンドでタスクを作成し、予定期間が正しく設定されること")]
    public async Task Handle_ValidCommand_ShouldCreateTaskWithScheduledPeriod()
    {
        // Arrange
        var startDate = DateTime.UtcNow;
        var endDate = startDate.AddDays(7);
        var command = new CreateTaskCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Task",
            "Test Description",
            startDate,
            endDate,
            40,
            "user1"
        );

        TaskAggregate? savedTask = null;
        await _repository.SaveAsync(Arg.Do<TaskAggregate>(t => savedTask = t));

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(savedTask);
        Assert.NotNull(savedTask.ScheduledPeriod);
        Assert.Equal(startDate, savedTask.ScheduledPeriod.StartDate);
        Assert.Equal(endDate, savedTask.ScheduledPeriod.EndDate);
        Assert.Equal(40, savedTask.ScheduledPeriod.EstimatedHours);
    }
}

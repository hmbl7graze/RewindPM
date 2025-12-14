using NSubstitute;
using RewindPM.Application.Write.CommandHandlers.Projects;
using RewindPM.Application.Write.Commands.Projects;
using RewindPM.Application.Write.Repositories;
using RewindPM.Domain.Aggregates;
using RewindPM.Domain.Common;
using RewindPM.Domain.ValueObjects;

namespace RewindPM.Application.Write.Test.CommandHandlers.Projects;

public class DeleteProjectCommandHandlerTests
{
    private readonly IAggregateRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly DeleteProjectCommandHandler _handler;

    public DeleteProjectCommandHandlerTests()
    {
        _repository = Substitute.For<IAggregateRepository>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _dateTimeProvider.UtcNow.Returns(DateTimeOffset.UtcNow);
        _handler = new DeleteProjectCommandHandler(_repository, _dateTimeProvider);
    }

    [Fact(DisplayName = "有効なコマンドでプロジェクトを削除すること（タスクなし）")]
    public async Task Handle_ValidCommand_ShouldDeleteProject()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var command = new DeleteProjectCommand(
            projectId,
            "user1"
        );

        var existingProject = ProjectAggregate.Create(
            projectId,
            "Test Project",
            "Test Description",
            "user1",
            _dateTimeProvider
        );
        existingProject.ClearUncommittedEvents();

        _repository.GetByIdAsync<ProjectAggregate>(projectId)
            .Returns(existingProject);
        _repository.GetTaskIdsByProjectIdAsync(projectId)
            .Returns(new List<Guid>());

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        await _repository.Received(1).GetByIdAsync<ProjectAggregate>(projectId);
        await _repository.Received(1).GetTaskIdsByProjectIdAsync(projectId);
        await _repository.Received(1).SaveAsync(Arg.Is<ProjectAggregate>(p =>
            p.Id == projectId &&
            p.UncommittedEvents.Count == 1
        ));
    }

    [Fact(DisplayName = "カスケード削除：関連タスクも削除されること")]
    public async Task Handle_WithTasks_ShouldDeleteProjectAndTasks()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var task1Id = Guid.NewGuid();
        var task2Id = Guid.NewGuid();
        var command = new DeleteProjectCommand(
            projectId,
            "user1"
        );

        var existingProject = ProjectAggregate.Create(
            projectId,
            "Test Project",
            "Test Description",
            "user1",
            _dateTimeProvider
        );
        existingProject.ClearUncommittedEvents();

        var task1 = TaskAggregate.Create(
            task1Id,
            projectId,
            "Task 1",
            "Description 1",
            new ScheduledPeriod(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(7)),
            "user1",
            _dateTimeProvider
        );
        task1.ClearUncommittedEvents();

        var task2 = TaskAggregate.Create(
            task2Id,
            projectId,
            "Task 2",
            "Description 2",
            new ScheduledPeriod(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(14)),
            "user1",
            _dateTimeProvider
        );
        task2.ClearUncommittedEvents();

        var taskIds = new List<Guid> { task1Id, task2Id };

        _repository.GetByIdAsync<ProjectAggregate>(projectId)
            .Returns(existingProject);
        _repository.GetTaskIdsByProjectIdAsync(projectId)
            .Returns(taskIds);
        _repository.GetByIdAsync<TaskAggregate>(task1Id)
            .Returns(task1);
        _repository.GetByIdAsync<TaskAggregate>(task2Id)
            .Returns(task2);

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        await _repository.Received(1).GetTaskIdsByProjectIdAsync(projectId);
        await _repository.Received(1).GetByIdAsync<TaskAggregate>(task1Id);
        await _repository.Received(1).GetByIdAsync<TaskAggregate>(task2Id);
        await _repository.Received(2).SaveAsync(Arg.Any<TaskAggregate>());
        await _repository.Received(1).SaveAsync(Arg.Is<ProjectAggregate>(p =>
            p.Id == projectId
        ));
    }

    [Fact(DisplayName = "存在しないプロジェクトを削除しようとした場合に例外をスローすること")]
    public async Task Handle_NonExistentProject_ShouldThrowException()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var command = new DeleteProjectCommand(
            projectId,
            "user1"
        );

        _repository.GetByIdAsync<ProjectAggregate>(projectId)
            .Returns((ProjectAggregate?)null);
        _repository.GetTaskIdsByProjectIdAsync(projectId)
            .Returns(new List<Guid>());

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _handler.Handle(command, TestContext.Current.CancellationToken)
        );

        await _repository.Received(1).GetByIdAsync<ProjectAggregate>(projectId);
        await _repository.DidNotReceive().SaveAsync(Arg.Any<ProjectAggregate>());
    }
}

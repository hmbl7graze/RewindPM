using NSubstitute;
using RewindPM.Application.Write.CommandHandlers.Projects;
using RewindPM.Application.Write.Commands.Projects;
using RewindPM.Application.Write.Repositories;
using RewindPM.Domain.Aggregates;
using RewindPM.Domain.Common;

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

    [Fact(DisplayName = "有効なコマンドでプロジェクトを削除すること")]
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

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        await _repository.Received(1).GetByIdAsync<ProjectAggregate>(projectId);
        await _repository.Received(1).SaveAsync(Arg.Is<ProjectAggregate>(p =>
            p.Id == projectId &&
            p.UncommittedEvents.Count == 1
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

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _handler.Handle(command, TestContext.Current.CancellationToken)
        );

        await _repository.Received(1).GetByIdAsync<ProjectAggregate>(projectId);
        await _repository.DidNotReceive().SaveAsync(Arg.Any<ProjectAggregate>());
    }
}

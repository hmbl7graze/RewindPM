using NSubstitute;
using RewindPM.Application.Write.CommandHandlers.Projects;
using RewindPM.Application.Write.Commands.Projects;
using RewindPM.Application.Write.Repositories;
using RewindPM.Domain.Aggregates;
using RewindPM.Domain.Common;

namespace RewindPM.Application.Write.Test.CommandHandlers.Projects;

public class UpdateProjectCommandHandlerTests
{
    private readonly IAggregateRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly UpdateProjectCommandHandler _handler;

    public UpdateProjectCommandHandlerTests()
    {
        _repository = Substitute.For<IAggregateRepository>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);
        _handler = new UpdateProjectCommandHandler(_repository, _dateTimeProvider);
    }

    [Fact(DisplayName = "既存のプロジェクトを更新できること")]
    public async Task Handle_ExistingProject_ShouldUpdateProject()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var existingProject = ProjectAggregate.Create(
            projectId,
            "Original Title",
            "Original Description",
            "user1",
            _dateTimeProvider
        );

        _repository.GetByIdAsync<ProjectAggregate>(projectId)
            .Returns(existingProject);

        var command = new UpdateProjectCommand(
            projectId,
            "Updated Title",
            "Updated Description",
            "user2"
        );

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        await _repository.Received(1).GetByIdAsync<ProjectAggregate>(projectId);
        await _repository.Received(1).SaveAsync(Arg.Is<ProjectAggregate>(p =>
            p.Id == projectId &&
            p.Title == "Updated Title" &&
            p.Description == "Updated Description" &&
            p.UpdatedBy == "user2"
        ));
    }

    [Fact(DisplayName = "存在しないプロジェクトの更新時にInvalidOperationExceptionをスローすること")]
    public async Task Handle_NonExistentProject_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        _repository.GetByIdAsync<ProjectAggregate>(projectId)
            .Returns((ProjectAggregate?)null);

        var command = new UpdateProjectCommand(
            projectId,
            "Updated Title",
            "Updated Description",
            "user2"
        );

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _handler.Handle(command, TestContext.Current.CancellationToken)
        );

        Assert.Equal($"プロジェクト（ID: {projectId}）が見つかりません", exception.Message);
        await _repository.DidNotReceive().SaveAsync(Arg.Any<ProjectAggregate>());
    }
}

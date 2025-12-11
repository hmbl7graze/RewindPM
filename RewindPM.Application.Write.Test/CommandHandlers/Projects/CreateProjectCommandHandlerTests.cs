using NSubstitute;
using RewindPM.Application.Write.CommandHandlers.Projects;
using RewindPM.Application.Write.Commands.Projects;
using RewindPM.Application.Write.Repositories;
using RewindPM.Domain.Aggregates;
using RewindPM.Domain.Common;

namespace RewindPM.Application.Write.Test.CommandHandlers.Projects;

public class CreateProjectCommandHandlerTests
{
    private readonly IAggregateRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly CreateProjectCommandHandler _handler;

    public CreateProjectCommandHandlerTests()
    {
        _repository = Substitute.For<IAggregateRepository>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);
        _handler = new CreateProjectCommandHandler(_repository, _dateTimeProvider);
    }

    [Fact(DisplayName = "有効なコマンドでプロジェクトを作成し、IDを返すこと")]
    public async Task Handle_ValidCommand_ShouldCreateProjectAndReturnId()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var command = new CreateProjectCommand(
            projectId,
            "Test Project",
            "Test Description",
            "user1"
        );

        // Act
        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(projectId, result);

        // リポジトリのSaveAsyncが1回呼ばれたことを確認
        await _repository.Received(1).SaveAsync(Arg.Is<ProjectAggregate>(p =>
            p.Id == projectId &&
            p.Title == "Test Project" &&
            p.Description == "Test Description" &&
            p.CreatedBy == "user1"
        ));
    }

    [Fact(DisplayName = "有効なコマンドでAggregateを保存し、未コミットイベントが1件あること")]
    public async Task Handle_ValidCommand_ShouldSaveAggregateWithUncommittedEvents()
    {
        // Arrange
        var command = new CreateProjectCommand(
            Guid.NewGuid(),
            "Test Project",
            "Test Description",
            "user1"
        );

        ProjectAggregate? savedAggregate = null;
        await _repository.SaveAsync(Arg.Do<ProjectAggregate>(a => savedAggregate = a));

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(savedAggregate);
        Assert.Single(savedAggregate.UncommittedEvents);
    }
}

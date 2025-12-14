using RewindPM.Application.Write.Commands.Projects;
using RewindPM.Application.Write.Validators.Projects;

namespace RewindPM.Application.Write.Test.Validators.Projects;

public class DeleteProjectCommandValidatorTests
{
    private readonly DeleteProjectCommandValidator _validator;

    public DeleteProjectCommandValidatorTests()
    {
        _validator = new DeleteProjectCommandValidator();
    }

    [Fact(DisplayName = "有効なコマンドでバリデーションが成功すること")]
    public async Task Validate_ValidCommand_ShouldPass()
    {
        // Arrange
        var command = new DeleteProjectCommand(
            Guid.NewGuid(),
            "user1"
        );

        // Act
        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact(DisplayName = "プロジェクトIDが空の場合にバリデーションが失敗すること")]
    public async Task Validate_EmptyProjectId_ShouldFail()
    {
        // Arrange
        var command = new DeleteProjectCommand(
            Guid.Empty,
            "user1"
        );

        // Act
        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(DeleteProjectCommand.ProjectId));
    }

    [Fact(DisplayName = "削除者が空の場合にバリデーションが失敗すること")]
    public async Task Validate_EmptyDeletedBy_ShouldFail()
    {
        // Arrange
        var command = new DeleteProjectCommand(
            Guid.NewGuid(),
            ""
        );

        // Act
        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(DeleteProjectCommand.DeletedBy));
    }
}

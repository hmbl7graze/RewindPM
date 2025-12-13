using RewindPM.Application.Write.Commands.Tasks;
using RewindPM.Application.Write.Validators.Tasks;

namespace RewindPM.Application.Write.Test.Validators.Tasks;

public class DeleteTaskCommandValidatorTests
{
    private readonly DeleteTaskCommandValidator _validator;

    public DeleteTaskCommandValidatorTests()
    {
        _validator = new DeleteTaskCommandValidator();
    }

    [Fact(DisplayName = "有効なコマンドでバリデーションが成功すること")]
    public async Task Validate_ValidCommand_ShouldPass()
    {
        // Arrange
        var command = new DeleteTaskCommand(
            Guid.NewGuid(),
            "user1"
        );

        // Act
        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact(DisplayName = "タスクIDが空の場合にバリデーションが失敗すること")]
    public async Task Validate_EmptyTaskId_ShouldFail()
    {
        // Arrange
        var command = new DeleteTaskCommand(
            Guid.Empty,
            "user1"
        );

        // Act
        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(DeleteTaskCommand.TaskId));
    }

    [Fact(DisplayName = "削除者が空の場合にバリデーションが失敗すること")]
    public async Task Validate_EmptyDeletedBy_ShouldFail()
    {
        // Arrange
        var command = new DeleteTaskCommand(
            Guid.NewGuid(),
            ""
        );

        // Act
        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(DeleteTaskCommand.DeletedBy));
    }
}

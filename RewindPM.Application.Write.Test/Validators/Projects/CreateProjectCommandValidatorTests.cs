using RewindPM.Application.Write.Commands.Projects;
using RewindPM.Application.Write.Validators.Projects;

namespace RewindPM.Application.Write.Test.Validators.Projects;

public class CreateProjectCommandValidatorTests
{
    private readonly CreateProjectCommandValidator _validator;

    public CreateProjectCommandValidatorTests()
    {
        _validator = new CreateProjectCommandValidator();
    }

    [Fact(DisplayName = "有効なコマンドでバリデーションが成功すること")]
    public async Task Validate_ValidCommand_ShouldPass()
    {
        // Arrange
        var command = new CreateProjectCommand(
            Guid.NewGuid(),
            "Test Project",
            "Test Description",
            "user1"
        );

        // Act
        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact(DisplayName = "IDが空の場合にバリデーションが失敗すること")]
    public async Task Validate_EmptyId_ShouldFail()
    {
        // Arrange
        var command = new CreateProjectCommand(
            Guid.Empty,
            "Test Project",
            "Test Description",
            "user1"
        );

        // Act
        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateProjectCommand.Id));
    }

    [Fact(DisplayName = "タイトルが空の場合にバリデーションが失敗すること")]
    public async Task Validate_EmptyTitle_ShouldFail()
    {
        // Arrange
        var command = new CreateProjectCommand(
            Guid.NewGuid(),
            "",
            "Test Description",
            "user1"
        );

        // Act
        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateProjectCommand.Title));
    }

    [Fact(DisplayName = "タイトルが200文字を超える場合にバリデーションが失敗すること")]
    public async Task Validate_TitleTooLong_ShouldFail()
    {
        // Arrange
        var command = new CreateProjectCommand(
            Guid.NewGuid(),
            new string('あ', 201),
            "Test Description",
            "user1"
        );

        // Act
        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateProjectCommand.Title));
    }

    [Fact(DisplayName = "作成者が空の場合にバリデーションが失敗すること")]
    public async Task Validate_EmptyCreatedBy_ShouldFail()
    {
        // Arrange
        var command = new CreateProjectCommand(
            Guid.NewGuid(),
            "Test Project",
            "Test Description",
            ""
        );

        // Act
        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateProjectCommand.CreatedBy));
    }
}

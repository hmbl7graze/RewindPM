using RewindPM.Application.Write.Commands.Tasks;
using RewindPM.Application.Write.Validators.Tasks;

namespace RewindPM.Application.Write.Test.Validators.Tasks;

public class CreateTaskCommandValidatorTests
{
    private readonly CreateTaskCommandValidator _validator;

    public CreateTaskCommandValidatorTests()
    {
        _validator = new CreateTaskCommandValidator();
    }

    [Fact(DisplayName = "有効なコマンドでバリデーションが成功すること")]
    public async Task Validate_ValidCommand_ShouldPass()
    {
        // Arrange
        var command = new CreateTaskCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Task",
            "Test Description",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(7),
            40,
            null,
            null,
            null,
            "user1"
        );

        // Act
        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact(DisplayName = "予定終了日が予定開始日より前の場合にバリデーションが失敗すること")]
    public async Task Validate_EndDateBeforeStartDate_ShouldFail()
    {
        // Arrange
        var command = new CreateTaskCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Task",
            "Test Description",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(-1),
            40,
            null,
            null,
            null,
            "user1"
        );

        // Act
        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
    }

    [Fact(DisplayName = "見積工数が0以下の場合にバリデーションが失敗すること")]
    public async Task Validate_EstimatedHoursZeroOrNegative_ShouldFail()
    {
        // Arrange
        var command = new CreateTaskCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Task",
            "Test Description",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(7),
            0,
            null,
            null,
            null,
            "user1"
        );

        // Act
        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTaskCommand.EstimatedHours));
    }

    [Fact(DisplayName = "タイトルが空の場合にバリデーションが失敗すること")]
    public async Task Validate_EmptyTitle_ShouldFail()
    {
        // Arrange
        var command = new CreateTaskCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "",
            "Test Description",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(7),
            40,
            null,
            null,
            null,
            "user1"
        );

        // Act
        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTaskCommand.Title));
    }

    [Fact(DisplayName = "実績終了日が実績開始日より前の場合にバリデーションが失敗すること")]
    public async Task Validate_ActualEndDateBeforeStartDate_ShouldFail()
    {
        // Arrange
        var command = new CreateTaskCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Task",
            "Test Description",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(7),
            40,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(-1),
            30,
            "user1"
        );

        // Act
        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
    }

    [Fact(DisplayName = "実績工数が0以下の場合にバリデーションが失敗すること")]
    public async Task Validate_ActualHoursZeroOrNegative_ShouldFail()
    {
        // Arrange
        var command = new CreateTaskCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Task",
            "Test Description",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(7),
            40,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(5),
            0,
            "user1"
        );

        // Act
        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTaskCommand.ActualHours));
    }

    [Fact(DisplayName = "実績データがnullの場合にバリデーションが成功すること")]
    public async Task Validate_NullActualData_ShouldPass()
    {
        // Arrange
        var command = new CreateTaskCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Task",
            "Test Description",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(7),
            40,
            null,
            null,
            null,
            "user1"
        );

        // Act
        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact(DisplayName = "予定データがnullの場合にバリデーションが成功すること")]
    public async Task Validate_NullScheduledData_ShouldPass()
    {
        // Arrange
        var command = new CreateTaskCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Task",
            "Test Description",
            null,
            null,
            null,
            null,
            null,
            null,
            "user1"
        );

        // Act
        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }
}

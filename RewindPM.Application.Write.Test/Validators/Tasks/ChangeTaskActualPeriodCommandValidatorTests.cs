using RewindPM.Application.Write.Commands.Tasks;
using RewindPM.Application.Write.Validators.Tasks;

namespace RewindPM.Application.Write.Test.Validators.Tasks;

public class ChangeTaskActualPeriodCommandValidatorTests
{
    private readonly ChangeTaskActualPeriodCommandValidator _validator;

    public ChangeTaskActualPeriodCommandValidatorTests()
    {
        _validator = new ChangeTaskActualPeriodCommandValidator();
    }

    [Fact(DisplayName = "有効なコマンドでバリデーションが成功すること")]
    public async Task Validate_ValidCommand_ShouldPass()
    {
        // Arrange
        var command = new ChangeTaskActualPeriodCommand(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(7),
            40,
            "user1"
        );

        // Act
        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact(DisplayName = "実績終了日が実績開始日より前の場合にバリデーションが失敗すること")]
    public async Task Validate_EndDateBeforeStartDate_ShouldFail()
    {
        // Arrange
        var command = new ChangeTaskActualPeriodCommand(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(-1),
            40,
            "user1"
        );

        // Act
        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ChangeTaskActualPeriodCommand.ActualEndDate));
    }

    [Fact(DisplayName = "実績工数が0以下の場合にバリデーションが失敗すること")]
    public async Task Validate_ActualHoursZeroOrNegative_ShouldFail()
    {
        // Arrange
        var command = new ChangeTaskActualPeriodCommand(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(7),
            0,
            "user1"
        );

        // Act
        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ChangeTaskActualPeriodCommand.ActualHours));
    }

    [Fact(DisplayName = "実績期間と工数がnullの場合でもバリデーションが成功すること")]
    public async Task Validate_AllNullValues_ShouldPass()
    {
        // Arrange
        var command = new ChangeTaskActualPeriodCommand(
            Guid.NewGuid(),
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

using RewindPM.Application.Write.Commands.Tasks;
using RewindPM.Application.Write.Validators.Tasks;

namespace RewindPM.Application.Write.Test.Validators.Tasks;

public class ChangeTaskScheduleCommandValidatorTests
{
    private readonly ChangeTaskScheduleCommandValidator _validator;

    public ChangeTaskScheduleCommandValidatorTests()
    {
        _validator = new ChangeTaskScheduleCommandValidator();
    }

    [Fact(DisplayName = "有効なコマンド（工数あり）でバリデーションが成功すること")]
    public async Task Validate_ValidCommandWithEstimatedHours_ShouldPass()
    {
        // Arrange
        var command = new ChangeTaskScheduleCommand(
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

    [Fact(DisplayName = "有効なコマンド（工数なし）でバリデーションが成功すること")]
    public async Task Validate_ValidCommandWithoutEstimatedHours_ShouldPass()
    {
        // Arrange
        var command = new ChangeTaskScheduleCommand(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(7),
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
        var command = new ChangeTaskScheduleCommand(
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
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ChangeTaskScheduleCommand.ScheduledEndDate));
        Assert.Contains(result.Errors, e => e.ErrorMessage == "予定終了日は予定開始日より後でなければなりません");
    }

    [Fact(DisplayName = "工数が0の場合にバリデーションが失敗すること")]
    public async Task Validate_EstimatedHoursZero_ShouldFail()
    {
        // Arrange
        var command = new ChangeTaskScheduleCommand(
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
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ChangeTaskScheduleCommand.EstimatedHours));
        Assert.Contains(result.Errors, e => e.ErrorMessage == "見積工数は正の数でなければなりません");
    }

    [Fact(DisplayName = "工数が負の値の場合にバリデーションが失敗すること")]
    public async Task Validate_EstimatedHoursNegative_ShouldFail()
    {
        // Arrange
        var command = new ChangeTaskScheduleCommand(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(7),
            -10,
            "user1"
        );

        // Act
        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ChangeTaskScheduleCommand.EstimatedHours));
        Assert.Contains(result.Errors, e => e.ErrorMessage == "見積工数は正の数でなければなりません");
    }

    [Fact(DisplayName = "TaskIdが空の場合にバリデーションが失敗すること")]
    public async Task Validate_EmptyTaskId_ShouldFail()
    {
        // Arrange
        var command = new ChangeTaskScheduleCommand(
            Guid.Empty,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(7),
            40,
            "user1"
        );

        // Act
        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ChangeTaskScheduleCommand.TaskId));
        Assert.Contains(result.Errors, e => e.ErrorMessage == "タスクIDは必須です");
    }

    [Fact(DisplayName = "ChangedByが空の場合にバリデーションが失敗すること")]
    public async Task Validate_EmptyChangedBy_ShouldFail()
    {
        // Arrange
        var command = new ChangeTaskScheduleCommand(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(7),
            40,
            ""
        );

        // Act
        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ChangeTaskScheduleCommand.ChangedBy));
        Assert.Contains(result.Errors, e => e.ErrorMessage == "変更者のユーザーIDは必須です");
    }

    [Fact(DisplayName = "予定終了日が予定開始日と同じ場合にバリデーションが失敗すること")]
    public async Task Validate_EndDateEqualsStartDate_ShouldFail()
    {
        // Arrange
        var sameDate = DateTimeOffset.UtcNow;
        var command = new ChangeTaskScheduleCommand(
            Guid.NewGuid(),
            sameDate,
            sameDate,
            40,
            "user1"
        );

        // Act
        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(ChangeTaskScheduleCommand.ScheduledEndDate));
        Assert.Contains(result.Errors, e => e.ErrorMessage == "予定終了日は予定開始日より後でなければなりません");
    }
}

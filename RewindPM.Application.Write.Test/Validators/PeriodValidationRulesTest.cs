using FluentValidation.TestHelper;
using RewindPM.Application.Write.Commands.Tasks;
using RewindPM.Application.Write.Validators.Tasks;

namespace RewindPM.Application.Write.Test.Validators;

/// <summary>
/// PeriodValidationRulesの共通バリデーションルールのテスト
/// </summary>
public class PeriodValidationRulesTest
{
    private readonly CreateTaskCommandValidator _createValidator;
    private readonly ChangeTaskScheduleCommandValidator _scheduleValidator;
    private readonly ChangeTaskActualPeriodCommandValidator _actualPeriodValidator;

    public PeriodValidationRulesTest()
    {
        _createValidator = new CreateTaskCommandValidator();
        _scheduleValidator = new ChangeTaskScheduleCommandValidator();
        _actualPeriodValidator = new ChangeTaskActualPeriodCommandValidator();
    }

    #region EndDateMustBeAfterStartDate Tests

    [Fact]
    public void EndDateMustBeAfterStartDate_Should_Pass_When_Both_Dates_Are_Null()
    {
        // Arrange
        var command = new CreateTaskCommand(
            Id: Guid.NewGuid(),
            ProjectId: Guid.NewGuid(),
            Title: "テスト",
            Description: "",
            ScheduledStartDate: null,
            ScheduledEndDate: null,
            EstimatedHours: null,
            ActualStartDate: null,
            ActualEndDate: null,
            ActualHours: null,
            CreatedBy: "test-user"
        );

        // Act
        var result = _createValidator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ScheduledEndDate);
    }

    [Fact]
    public void EndDateMustBeAfterStartDate_Should_Pass_When_StartDate_Is_Null()
    {
        // Arrange
        var command = new CreateTaskCommand(
            Id: Guid.NewGuid(),
            ProjectId: Guid.NewGuid(),
            Title: "テスト",
            Description: "",
            ScheduledStartDate: null,
            ScheduledEndDate: new DateTimeOffset(2025, 12, 31, 0, 0, 0, TimeSpan.Zero),
            EstimatedHours: null,
            ActualStartDate: null,
            ActualEndDate: null,
            ActualHours: null,
            CreatedBy: "test-user"
        );

        // Act
        var result = _createValidator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ScheduledEndDate);
    }

    [Fact]
    public void EndDateMustBeAfterStartDate_Should_Pass_When_EndDate_Is_Null()
    {
        // Arrange
        var command = new CreateTaskCommand(
            Id: Guid.NewGuid(),
            ProjectId: Guid.NewGuid(),
            Title: "テスト",
            Description: "",
            ScheduledStartDate: new DateTimeOffset(2025, 12, 1, 0, 0, 0, TimeSpan.Zero),
            ScheduledEndDate: null,
            EstimatedHours: null,
            ActualStartDate: null,
            ActualEndDate: null,
            ActualHours: null,
            CreatedBy: "test-user"
        );

        // Act
        var result = _createValidator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ScheduledEndDate);
    }

    [Fact]
    public void EndDateMustBeAfterStartDate_Should_Pass_When_EndDate_Is_After_StartDate()
    {
        // Arrange
        var command = new CreateTaskCommand(
            Id: Guid.NewGuid(),
            ProjectId: Guid.NewGuid(),
            Title: "テスト",
            Description: "",
            ScheduledStartDate: new DateTimeOffset(2025, 12, 1, 0, 0, 0, TimeSpan.Zero),
            ScheduledEndDate: new DateTimeOffset(2025, 12, 31, 0, 0, 0, TimeSpan.Zero),
            EstimatedHours: null,
            ActualStartDate: null,
            ActualEndDate: null,
            ActualHours: null,
            CreatedBy: "test-user"
        );

        // Act
        var result = _createValidator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ScheduledEndDate);
    }

    [Fact]
    public void EndDateMustBeAfterStartDate_Should_Fail_When_EndDate_Is_Before_StartDate()
    {
        // Arrange
        var command = new CreateTaskCommand(
            Id: Guid.NewGuid(),
            ProjectId: Guid.NewGuid(),
            Title: "テスト",
            Description: "",
            ScheduledStartDate: new DateTimeOffset(2025, 12, 31, 0, 0, 0, TimeSpan.Zero),
            ScheduledEndDate: new DateTimeOffset(2025, 12, 1, 0, 0, 0, TimeSpan.Zero),
            EstimatedHours: null,
            ActualStartDate: null,
            ActualEndDate: null,
            ActualHours: null,
            CreatedBy: "test-user"
        );

        // Act
        var result = _createValidator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ScheduledEndDate);
    }

    [Fact]
    public void EndDateMustBeAfterStartDate_Should_Fail_When_EndDate_Equals_StartDate()
    {
        // Arrange
        var command = new CreateTaskCommand(
            Id: Guid.NewGuid(),
            ProjectId: Guid.NewGuid(),
            Title: "テスト",
            Description: "",
            ScheduledStartDate: new DateTimeOffset(2025, 12, 15, 0, 0, 0, TimeSpan.Zero),
            ScheduledEndDate: new DateTimeOffset(2025, 12, 15, 0, 0, 0, TimeSpan.Zero),
            EstimatedHours: null,
            ActualStartDate: null,
            ActualEndDate: null,
            ActualHours: null,
            CreatedBy: "test-user"
        );

        // Act
        var result = _createValidator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ScheduledEndDate);
    }

    [Fact]
    public void EndDateMustBeAfterStartDate_Should_Work_With_NonNullable_Dates()
    {
        // Arrange
        var command = new ChangeTaskScheduleCommand(
            TaskId: Guid.NewGuid(),
            ScheduledStartDate: new DateTimeOffset(2025, 12, 1, 0, 0, 0, TimeSpan.Zero),
            ScheduledEndDate: new DateTimeOffset(2025, 12, 31, 0, 0, 0, TimeSpan.Zero),
            EstimatedHours: null,
            ChangedBy: "test-user"
        );

        // Act
        var result = _scheduleValidator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ScheduledEndDate);
    }

    [Fact]
    public void EndDateMustBeAfterStartDate_Should_Fail_With_NonNullable_Dates_When_Invalid()
    {
        // Arrange
        var command = new ChangeTaskScheduleCommand(
            TaskId: Guid.NewGuid(),
            ScheduledStartDate: new DateTimeOffset(2025, 12, 31, 0, 0, 0, TimeSpan.Zero),
            ScheduledEndDate: new DateTimeOffset(2025, 12, 1, 0, 0, 0, TimeSpan.Zero),
            EstimatedHours: null,
            ChangedBy: "test-user"
        );

        // Act
        var result = _scheduleValidator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ScheduledEndDate);
    }

    #endregion

    #region MustBePositiveWhenHasValue Tests

    [Fact]
    public void MustBePositiveWhenHasValue_Should_Pass_When_Value_Is_Null()
    {
        // Arrange
        var command = new CreateTaskCommand(
            Id: Guid.NewGuid(),
            ProjectId: Guid.NewGuid(),
            Title: "テスト",
            Description: "",
            ScheduledStartDate: null,
            ScheduledEndDate: null,
            EstimatedHours: null,
            ActualStartDate: null,
            ActualEndDate: null,
            ActualHours: null,
            CreatedBy: "test-user"
        );

        // Act
        var result = _createValidator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.EstimatedHours);
    }

    [Fact]
    public void MustBePositiveWhenHasValue_Should_Pass_When_Value_Is_Positive()
    {
        // Arrange
        var command = new CreateTaskCommand(
            Id: Guid.NewGuid(),
            ProjectId: Guid.NewGuid(),
            Title: "テスト",
            Description: "",
            ScheduledStartDate: null,
            ScheduledEndDate: null,
            EstimatedHours: 8,
            ActualStartDate: null,
            ActualEndDate: null,
            ActualHours: null,
            CreatedBy: "test-user"
        );

        // Act
        var result = _createValidator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.EstimatedHours);
    }

    [Fact]
    public void MustBePositiveWhenHasValue_Should_Fail_When_Value_Is_Zero()
    {
        // Arrange
        var command = new CreateTaskCommand(
            Id: Guid.NewGuid(),
            ProjectId: Guid.NewGuid(),
            Title: "テスト",
            Description: "",
            ScheduledStartDate: null,
            ScheduledEndDate: null,
            EstimatedHours: 0,
            ActualStartDate: null,
            ActualEndDate: null,
            ActualHours: null,
            CreatedBy: "test-user"
        );

        // Act
        var result = _createValidator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EstimatedHours);
    }

    [Fact]
    public void MustBePositiveWhenHasValue_Should_Fail_When_Value_Is_Negative()
    {
        // Arrange
        var command = new CreateTaskCommand(
            Id: Guid.NewGuid(),
            ProjectId: Guid.NewGuid(),
            Title: "テスト",
            Description: "",
            ScheduledStartDate: null,
            ScheduledEndDate: null,
            EstimatedHours: -5,
            ActualStartDate: null,
            ActualEndDate: null,
            ActualHours: null,
            CreatedBy: "test-user"
        );

        // Act
        var result = _createValidator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EstimatedHours);
    }

    [Fact]
    public void MustBePositiveWhenHasValue_Should_Work_With_ActualHours()
    {
        // Arrange
        var command = new ChangeTaskActualPeriodCommand(
            TaskId: Guid.NewGuid(),
            ActualStartDate: new DateTimeOffset(2025, 12, 1, 0, 0, 0, TimeSpan.Zero),
            ActualEndDate: new DateTimeOffset(2025, 12, 15, 0, 0, 0, TimeSpan.Zero),
            ActualHours: 40,
            ChangedBy: "test-user"
        );

        // Act
        var result = _actualPeriodValidator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ActualHours);
    }

    [Fact]
    public void MustBePositiveWhenHasValue_Should_Fail_For_ActualHours_When_Zero()
    {
        // Arrange
        var command = new ChangeTaskActualPeriodCommand(
            TaskId: Guid.NewGuid(),
            ActualStartDate: new DateTimeOffset(2025, 12, 1, 0, 0, 0, TimeSpan.Zero),
            ActualEndDate: new DateTimeOffset(2025, 12, 15, 0, 0, 0, TimeSpan.Zero),
            ActualHours: 0,
            ChangedBy: "test-user"
        );

        // Act
        var result = _actualPeriodValidator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ActualHours);
    }

    #endregion
}

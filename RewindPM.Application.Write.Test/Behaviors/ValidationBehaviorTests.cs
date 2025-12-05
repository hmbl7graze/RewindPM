using FluentValidation;
using FluentValidation.Results;
using MediatR;
using NSubstitute;
using RewindPM.Application.Write.Behaviors;

namespace RewindPM.Application.Write.Test.Behaviors;

public class ValidationBehaviorTests
{
    public record TestRequest(string Value) : IRequest<string>;

    [Fact(DisplayName = "バリデーターが存在しない場合は次のパイプラインに進むこと")]
    public async Task Handle_NoValidators_ShouldCallNext()
    {
        // Arrange
        var validators = new List<IValidator<TestRequest>>();
        var behavior = new ValidationBehavior<TestRequest, string>(validators);
        var request = new TestRequest("test");
        var nextCalled = false;

        RequestHandlerDelegate<string> next = () =>
        {
            nextCalled = true;
            return Task.FromResult("result");
        };

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal("result", result);
    }

    [Fact(DisplayName = "バリデーションが成功した場合は次のパイプラインに進むこと")]
    public async Task Handle_ValidationSuccess_ShouldCallNext()
    {
        // Arrange
        var validator = Substitute.For<IValidator<TestRequest>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var validators = new List<IValidator<TestRequest>> { validator };
        var behavior = new ValidationBehavior<TestRequest, string>(validators);
        var request = new TestRequest("test");
        var nextCalled = false;

        RequestHandlerDelegate<string> next = () =>
        {
            nextCalled = true;
            return Task.FromResult("result");
        };

        // Act
        var result = await behavior.Handle(request, next, CancellationToken.None);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal("result", result);
    }

    [Fact(DisplayName = "バリデーションが失敗した場合はValidationExceptionをスローすること")]
    public async Task Handle_ValidationFailure_ShouldThrowValidationException()
    {
        // Arrange
        var validator = Substitute.For<IValidator<TestRequest>>();
        var validationFailure = new ValidationFailure("Value", "Value is required");
        var validationResult = new ValidationResult(new[] { validationFailure });

        validator.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(validationResult);

        var validators = new List<IValidator<TestRequest>> { validator };
        var behavior = new ValidationBehavior<TestRequest, string>(validators);
        var request = new TestRequest("");

        RequestHandlerDelegate<string> next = () => Task.FromResult("result");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            async () => await behavior.Handle(request, next, CancellationToken.None)
        );

        Assert.Single(exception.Errors);
        Assert.Equal("Value", exception.Errors.First().PropertyName);
    }

    [Fact(DisplayName = "複数のバリデーターのエラーを全て収集してValidationExceptionをスローすること")]
    public async Task Handle_MultipleValidators_ShouldCollectAllErrors()
    {
        // Arrange
        var validator1 = Substitute.For<IValidator<TestRequest>>();
        var validator2 = Substitute.For<IValidator<TestRequest>>();

        var failure1 = new ValidationFailure("Value", "Error 1");
        var failure2 = new ValidationFailure("Value", "Error 2");

        validator1.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new[] { failure1 }));

        validator2.ValidateAsync(Arg.Any<ValidationContext<TestRequest>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new[] { failure2 }));

        var validators = new List<IValidator<TestRequest>> { validator1, validator2 };
        var behavior = new ValidationBehavior<TestRequest, string>(validators);
        var request = new TestRequest("");

        RequestHandlerDelegate<string> next = () => Task.FromResult("result");

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            async () => await behavior.Handle(request, next, CancellationToken.None)
        );

        Assert.Equal(2, exception.Errors.Count());
    }
}

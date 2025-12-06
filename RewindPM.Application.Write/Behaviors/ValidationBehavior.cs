using FluentValidation;
using MediatR;

namespace RewindPM.Application.Write.Behaviors;

/// <summary>
/// MediatRパイプライン用のバリデーション動作
/// コマンド実行前にFluentValidationによるバリデーションを実行する
/// </summary>
/// <typeparam name="TRequest">リクエストの型</typeparam>
/// <typeparam name="TResponse">レスポンスの型</typeparam>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // バリデーターが存在しない場合はスキップ
        if (!_validators.Any())
        {
            return await next();
        }

        // バリデーション実行
        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken))
        );

        // エラーを収集
        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        // エラーがある場合は例外をスロー
        if (failures.Count != 0)
        {
            throw new ValidationException(failures);
        }

        // 次のパイプラインへ
        return await next();
    }
}

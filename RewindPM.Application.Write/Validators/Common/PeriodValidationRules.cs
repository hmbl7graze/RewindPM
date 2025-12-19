using FluentValidation;

namespace RewindPM.Application.Write.Validators.Common;

/// <summary>
/// 期間と工数のバリデーションルールを提供する拡張メソッド
/// </summary>
public static class PeriodValidationRules
{
    /// <summary>
    /// 終了日が開始日より後であることを検証（NULL許容型）
    /// </summary>
    public static IRuleBuilderOptions<T, DateTimeOffset?> EndDateMustBeAfterStartDate<T>(
        this IRuleBuilder<T, DateTimeOffset?> ruleBuilder,
        Func<T, DateTimeOffset?> startDateSelector,
        string? errorMessage = null)
    {
        return ruleBuilder
            .Must((model, endDate) =>
            {
                var startDate = startDateSelector(model);
                return !startDate.HasValue || !endDate.HasValue || endDate.Value > startDate.Value;
            })
            .WithMessage(errorMessage ?? "終了日は開始日より後でなければなりません");
    }

    /// <summary>
    /// 終了日が開始日より後であることを検証（非NULL値型）
    /// </summary>
    public static IRuleBuilderOptions<T, DateTimeOffset> EndDateMustBeAfterStartDate<T>(
        this IRuleBuilder<T, DateTimeOffset> ruleBuilder,
        Func<T, DateTimeOffset> startDateSelector,
        string? errorMessage = null)
    {
        return ruleBuilder
            .Must((model, endDate) =>
            {
                var startDate = startDateSelector(model);
                return endDate > startDate;
            })
            .WithMessage(errorMessage ?? "終了日は開始日より後でなければなりません");
    }

    /// <summary>
    /// 工数が正の数であることを検証（値が設定されている場合のみ）
    /// </summary>
    public static IRuleBuilderOptions<T, int?> MustBePositiveWhenHasValue<T>(
        this IRuleBuilder<T, int?> ruleBuilder,
        string? errorMessage = null)
    {
        return ruleBuilder
            .Must(value => !value.HasValue || value.Value > 0)
            .WithMessage(errorMessage ?? "工数は正の数でなければなりません");
    }
}

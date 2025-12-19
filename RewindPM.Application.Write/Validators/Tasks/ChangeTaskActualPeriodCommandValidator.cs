using FluentValidation;
using RewindPM.Application.Write.Commands.Tasks;
using RewindPM.Application.Write.Validators.Common;

namespace RewindPM.Application.Write.Validators.Tasks;

/// <summary>
/// ChangeTaskActualPeriodCommandのバリデーター
/// </summary>
public class ChangeTaskActualPeriodCommandValidator : AbstractValidator<ChangeTaskActualPeriodCommand>
{
    public ChangeTaskActualPeriodCommandValidator()
    {
        RuleFor(x => x.TaskId)
            .NotEmpty()
            .WithMessage("タスクIDは必須です");

        // 実績終了日が設定されている場合、実績開始日より後でなければならない
        RuleFor(x => x.ActualEndDate)
            .EndDateMustBeAfterStartDate(x => x.ActualStartDate);

        // 実績工数が設定されている場合、正の数でなければならない
        RuleFor(x => x.ActualHours)
            .MustBePositiveWhenHasValue();

        RuleFor(x => x.ChangedBy)
            .NotEmpty()
            .WithMessage("変更者のユーザーIDは必須です");
    }
}

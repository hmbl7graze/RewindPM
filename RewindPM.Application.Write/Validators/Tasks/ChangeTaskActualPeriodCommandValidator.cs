using FluentValidation;
using RewindPM.Application.Write.Commands.Tasks;

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
            .GreaterThan(x => x.ActualStartDate)
            .When(x => x.ActualStartDate.HasValue && x.ActualEndDate.HasValue)
            .WithMessage("実績終了日は実績開始日より後でなければなりません");

        // 実績工数が設定されている場合、正の数でなければならない
        RuleFor(x => x.ActualHours)
            .GreaterThan(0)
            .When(x => x.ActualHours.HasValue)
            .WithMessage("実績工数は正の数でなければなりません");

        RuleFor(x => x.ChangedBy)
            .NotEmpty()
            .WithMessage("変更者のユーザーIDは必須です");
    }
}

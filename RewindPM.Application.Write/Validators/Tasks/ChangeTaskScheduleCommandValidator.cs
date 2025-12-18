using FluentValidation;
using RewindPM.Application.Write.Commands.Tasks;

namespace RewindPM.Application.Write.Validators.Tasks;

/// <summary>
/// ChangeTaskScheduleCommandのバリデーター
/// </summary>
public class ChangeTaskScheduleCommandValidator : AbstractValidator<ChangeTaskScheduleCommand>
{
    public ChangeTaskScheduleCommandValidator()
    {
        RuleFor(x => x.TaskId)
            .NotEmpty()
            .WithMessage("タスクIDは必須です");

        RuleFor(x => x.ScheduledEndDate)
            .GreaterThan(x => x.ScheduledStartDate)
            .WithMessage("予定終了日は予定開始日より後でなければなりません");

        RuleFor(x => x.EstimatedHours)
            .GreaterThan(0)
            .When(x => x.EstimatedHours.HasValue)
            .WithMessage("見積工数は正の数でなければなりません");

        RuleFor(x => x.ChangedBy)
            .NotEmpty()
            .WithMessage("変更者のユーザーIDは必須です");
    }
}

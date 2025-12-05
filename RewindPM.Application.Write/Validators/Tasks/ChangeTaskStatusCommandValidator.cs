using FluentValidation;
using RewindPM.Application.Write.Commands.Tasks;

namespace RewindPM.Application.Write.Validators.Tasks;

/// <summary>
/// ChangeTaskStatusCommandのバリデーター
/// </summary>
public class ChangeTaskStatusCommandValidator : AbstractValidator<ChangeTaskStatusCommand>
{
    public ChangeTaskStatusCommandValidator()
    {
        RuleFor(x => x.TaskId)
            .NotEmpty()
            .WithMessage("タスクIDは必須です");

        RuleFor(x => x.NewStatus)
            .IsInEnum()
            .WithMessage("有効なタスクステータスを指定してください");

        RuleFor(x => x.ChangedBy)
            .NotEmpty()
            .WithMessage("変更者のユーザーIDは必須です");
    }
}

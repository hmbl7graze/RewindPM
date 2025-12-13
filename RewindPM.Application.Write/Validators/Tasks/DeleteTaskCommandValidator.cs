using FluentValidation;
using RewindPM.Application.Write.Commands.Tasks;

namespace RewindPM.Application.Write.Validators.Tasks;

/// <summary>
/// DeleteTaskCommandのバリデーター
/// </summary>
public class DeleteTaskCommandValidator : AbstractValidator<DeleteTaskCommand>
{
    public DeleteTaskCommandValidator()
    {
        RuleFor(x => x.TaskId)
            .NotEmpty()
            .WithMessage("タスクIDは必須です");

        RuleFor(x => x.DeletedBy)
            .NotEmpty()
            .WithMessage("削除者のユーザーIDは必須です");
    }
}

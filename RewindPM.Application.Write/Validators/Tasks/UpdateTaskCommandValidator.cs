using FluentValidation;
using RewindPM.Application.Write.Commands.Tasks;

namespace RewindPM.Application.Write.Validators.Tasks;

/// <summary>
/// UpdateTaskCommandのバリデーター
/// </summary>
public class UpdateTaskCommandValidator : AbstractValidator<UpdateTaskCommand>
{
    public UpdateTaskCommandValidator()
    {
        RuleFor(x => x.TaskId)
            .NotEmpty()
            .WithMessage("タスクIDは必須です");

        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("タスクのタイトルは必須です")
            .MaximumLength(200)
            .WithMessage("タスクのタイトルは200文字以内で入力してください");

        RuleFor(x => x.Description)
            .NotNull()
            .WithMessage("タスクの説明は必須です（空文字列は可）");

        RuleFor(x => x.UpdatedBy)
            .NotEmpty()
            .WithMessage("更新者のユーザーIDは必須です");
    }
}

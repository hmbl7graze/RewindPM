using FluentValidation;
using RewindPM.Application.Write.Commands.Projects;

namespace RewindPM.Application.Write.Validators.Projects;

/// <summary>
/// DeleteProjectCommandのバリデーター
/// タスク存在チェックはUI層で実装
/// </summary>
public class DeleteProjectCommandValidator : AbstractValidator<DeleteProjectCommand>
{
    public DeleteProjectCommandValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty()
            .WithMessage("プロジェクトIDは必須です");

        RuleFor(x => x.DeletedBy)
            .NotEmpty()
            .WithMessage("削除者のユーザーIDは必須です");
    }
}

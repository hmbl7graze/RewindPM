using FluentValidation;
using RewindPM.Application.Write.Commands.Projects;

namespace RewindPM.Application.Write.Validators.Projects;

/// <summary>
/// UpdateProjectCommandのバリデーター
/// </summary>
public class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectCommandValidator()
    {
        RuleFor(x => x.ProjectId)
            .NotEmpty()
            .WithMessage("プロジェクトIDは必須です");

        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("プロジェクトのタイトルは必須です")
            .MaximumLength(200)
            .WithMessage("プロジェクトのタイトルは200文字以内で入力してください");

        RuleFor(x => x.Description)
            .NotNull()
            .WithMessage("プロジェクトの説明は必須です（空文字列は可）");

        RuleFor(x => x.UpdatedBy)
            .NotEmpty()
            .WithMessage("更新者のユーザーIDは必須です");
    }
}

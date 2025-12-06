using FluentValidation;
using RewindPM.Application.Write.Commands.Projects;

namespace RewindPM.Application.Write.Validators.Projects;

/// <summary>
/// CreateProjectCommandのバリデーター
/// </summary>
public class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator()
    {
        RuleFor(x => x.Id)
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

        RuleFor(x => x.CreatedBy)
            .NotEmpty()
            .WithMessage("作成者のユーザーIDは必須です");
    }
}

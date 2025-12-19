using FluentValidation;
using RewindPM.Application.Write.Commands.Tasks;
using RewindPM.Application.Write.Validators.Common;

namespace RewindPM.Application.Write.Validators.Tasks;

/// <summary>
/// CreateTaskCommandのバリデーター
/// </summary>
public class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("タスクIDは必須です");

        RuleFor(x => x.ProjectId)
            .NotEmpty()
            .WithMessage("プロジェクトIDは必須です");

        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("タスクのタイトルは必須です")
            .MaximumLength(200)
            .WithMessage("タスクのタイトルは200文字以内で入力してください");

        RuleFor(x => x.Description)
            .NotNull()
            .WithMessage("タスクの説明は必須です（空文字列は可）");

        // 予定期間のバリデーション
        RuleFor(x => x.ScheduledEndDate)
            .EndDateMustBeAfterStartDate(x => x.ScheduledStartDate);

        RuleFor(x => x.EstimatedHours)
            .MustBePositiveWhenHasValue();

        // 実績期間のバリデーション
        RuleFor(x => x.ActualEndDate)
            .EndDateMustBeAfterStartDate(x => x.ActualStartDate);

        RuleFor(x => x.ActualHours)
            .MustBePositiveWhenHasValue();

        RuleFor(x => x.CreatedBy)
            .NotEmpty()
            .WithMessage("作成者のユーザーIDは必須です");
    }
}

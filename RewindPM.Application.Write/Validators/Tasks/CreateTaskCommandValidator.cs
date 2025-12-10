using FluentValidation;
using RewindPM.Application.Write.Commands.Tasks;

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

        // 予定期間のバリデーション（両方設定されている場合のみ）
        RuleFor(x => x)
            .Must(x => !x.ScheduledStartDate.HasValue || !x.ScheduledEndDate.HasValue || 
                       x.ScheduledEndDate.Value > x.ScheduledStartDate.Value)
            .WithMessage("予定終了日は予定開始日より後でなければなりません");

        RuleFor(x => x.EstimatedHours)
            .GreaterThan(0)
            .When(x => x.EstimatedHours.HasValue)
            .WithMessage("見積工数は正の数でなければなりません");

        // 実績期間のバリデーション（両方設定されている場合のみ）
        RuleFor(x => x)
            .Must(x => !x.ActualStartDate.HasValue || !x.ActualEndDate.HasValue || 
                       x.ActualEndDate.Value > x.ActualStartDate.Value)
            .WithMessage("実績終了日は実績開始日より後でなければなりません");

        RuleFor(x => x.ActualHours)
            .GreaterThan(0)
            .When(x => x.ActualHours.HasValue)
            .WithMessage("実績工数は正の数でなければなりません");

        RuleFor(x => x.CreatedBy)
            .NotEmpty()
            .WithMessage("作成者のユーザーIDは必須です");
    }
}

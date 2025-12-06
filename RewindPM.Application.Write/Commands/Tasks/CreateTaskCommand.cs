using MediatR;

namespace RewindPM.Application.Write.Commands.Tasks;

/// <summary>
/// タスク作成コマンド
/// </summary>
/// <param name="Id">タスクID</param>
/// <param name="ProjectId">所属するプロジェクトのID</param>
/// <param name="Title">タスクのタイトル</param>
/// <param name="Description">タスクの説明</param>
/// <param name="ScheduledStartDate">予定開始日</param>
/// <param name="ScheduledEndDate">予定終了日</param>
/// <param name="EstimatedHours">見積工数（時間）</param>
/// <param name="CreatedBy">作成者のユーザーID</param>
public record CreateTaskCommand(
    Guid Id,
    Guid ProjectId,
    string Title,
    string Description,
    DateTime ScheduledStartDate,
    DateTime ScheduledEndDate,
    int EstimatedHours,
    string CreatedBy
) : IRequest<Guid>;

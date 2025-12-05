using MediatR;

namespace RewindPM.Application.Write.Commands.Tasks;

/// <summary>
/// タスク予定期間変更コマンド
/// </summary>
/// <param name="TaskId">変更するタスクのID</param>
/// <param name="ScheduledStartDate">新しい予定開始日</param>
/// <param name="ScheduledEndDate">新しい予定終了日</param>
/// <param name="EstimatedHours">新しい見積工数（時間）</param>
/// <param name="ChangedBy">変更者のユーザーID</param>
public record ChangeTaskScheduleCommand(
    Guid TaskId,
    DateTime ScheduledStartDate,
    DateTime ScheduledEndDate,
    int EstimatedHours,
    string ChangedBy
) : IRequest;

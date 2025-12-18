using MediatR;
using TaskStatus = RewindPM.Domain.ValueObjects.TaskStatus;

namespace RewindPM.Application.Write.Commands.Tasks;

/// <summary>
/// タスク完全更新コマンド（全プロパティを一括更新）
/// </summary>
/// <param name="TaskId">更新するタスクのID</param>
/// <param name="Title">新しいタイトル</param>
/// <param name="Description">新しい説明</param>
/// <param name="Status">新しいステータス</param>
/// <param name="ScheduledStartDate">予定開始日</param>
/// <param name="ScheduledEndDate">予定終了日</param>
/// <param name="EstimatedHours">見積工数（時間、nullの場合は未設定）</param>
/// <param name="ActualStartDate">実績開始日（nullの場合は未設定）</param>
/// <param name="ActualEndDate">実績終了日（nullの場合は未設定）</param>
/// <param name="ActualHours">実績工数（時間、nullの場合は未設定）</param>
/// <param name="UpdatedBy">更新者のユーザーID</param>
public record UpdateTaskCompleteCommand(
    Guid TaskId,
    string Title,
    string Description,
    TaskStatus Status,
    DateTimeOffset? ScheduledStartDate,
    DateTimeOffset? ScheduledEndDate,
    int? EstimatedHours,
    DateTimeOffset? ActualStartDate,
    DateTimeOffset? ActualEndDate,
    int? ActualHours,
    string UpdatedBy
) : IRequest;

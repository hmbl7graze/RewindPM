using MediatR;

namespace RewindPM.Application.Write.Commands.Tasks;

/// <summary>
/// タスク実績期間変更コマンド
/// </summary>
/// <param name="TaskId">変更するタスクのID</param>
/// <param name="ActualStartDate">実績開始日（nullの場合は未設定）</param>
/// <param name="ActualEndDate">実績終了日（nullの場合は未設定）</param>
/// <param name="ActualHours">実績工数（時間、nullの場合は未設定）</param>
/// <param name="ChangedBy">変更者のユーザーID</param>
public record ChangeTaskActualPeriodCommand(
    Guid TaskId,
    DateTimeOffset? ActualStartDate,
    DateTimeOffset? ActualEndDate,
    int? ActualHours,
    string ChangedBy
) : IRequest;

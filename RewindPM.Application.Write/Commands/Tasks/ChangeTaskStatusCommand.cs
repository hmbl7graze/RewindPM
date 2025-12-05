using MediatR;
using TaskStatus = RewindPM.Domain.ValueObjects.TaskStatus;

namespace RewindPM.Application.Write.Commands.Tasks;

/// <summary>
/// タスクステータス変更コマンド
/// </summary>
/// <param name="TaskId">変更するタスクのID</param>
/// <param name="NewStatus">新しいステータス</param>
/// <param name="ChangedBy">変更者のユーザーID</param>
public record ChangeTaskStatusCommand(
    Guid TaskId,
    TaskStatus NewStatus,
    string ChangedBy
) : IRequest;

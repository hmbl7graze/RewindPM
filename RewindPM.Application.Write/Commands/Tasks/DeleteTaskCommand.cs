using MediatR;

namespace RewindPM.Application.Write.Commands.Tasks;

/// <summary>
/// タスクを削除するコマンド
/// </summary>
public record DeleteTaskCommand(
    Guid TaskId,
    string DeletedBy
) : IRequest;

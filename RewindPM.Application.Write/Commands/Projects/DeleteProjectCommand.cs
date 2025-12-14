using MediatR;

namespace RewindPM.Application.Write.Commands.Projects;

/// <summary>
/// プロジェクトを削除するコマンド
/// </summary>
public record DeleteProjectCommand(
    Guid ProjectId,
    string DeletedBy
) : IRequest;

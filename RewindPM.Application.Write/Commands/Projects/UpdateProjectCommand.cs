using MediatR;

namespace RewindPM.Application.Write.Commands.Projects;

/// <summary>
/// プロジェクト更新コマンド
/// </summary>
/// <param name="ProjectId">更新するプロジェクトのID</param>
/// <param name="Title">新しいタイトル</param>
/// <param name="Description">新しい説明</param>
/// <param name="UpdatedBy">更新者のユーザーID</param>
public record UpdateProjectCommand(
    Guid ProjectId,
    string Title,
    string Description,
    string UpdatedBy
) : IRequest;

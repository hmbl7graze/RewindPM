using MediatR;

namespace RewindPM.Application.Write.Commands.Projects;

/// <summary>
/// プロジェクト作成コマンド
/// </summary>
/// <param name="Id">プロジェクトID</param>
/// <param name="Title">プロジェクトのタイトル</param>
/// <param name="Description">プロジェクトの説明</param>
/// <param name="CreatedBy">作成者のユーザーID</param>
public record CreateProjectCommand(
    Guid Id,
    string Title,
    string Description,
    string CreatedBy
) : IRequest<Guid>;

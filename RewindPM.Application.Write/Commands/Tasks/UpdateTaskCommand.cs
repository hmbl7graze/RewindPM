using MediatR;

namespace RewindPM.Application.Write.Commands.Tasks;

/// <summary>
/// タスク更新コマンド（タイトルと説明のみ）
/// </summary>
/// <param name="TaskId">更新するタスクのID</param>
/// <param name="Title">新しいタイトル</param>
/// <param name="Description">新しい説明</param>
/// <param name="UpdatedBy">更新者のユーザーID</param>
public record UpdateTaskCommand(
    Guid TaskId,
    string Title,
    string Description,
    string UpdatedBy
) : IRequest;

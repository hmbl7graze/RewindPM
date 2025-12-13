using MediatR;
using RewindPM.Application.Write.Commands.Projects;
using RewindPM.Application.Write.Repositories;
using RewindPM.Domain.Aggregates;
using RewindPM.Domain.Common;

namespace RewindPM.Application.Write.CommandHandlers.Projects;

/// <summary>
/// プロジェクト削除コマンドのハンドラ
/// カスケード削除: 関連するタスクも削除する
/// </summary>
public class DeleteProjectCommandHandler : IRequestHandler<DeleteProjectCommand>
{
    private readonly IAggregateRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeleteProjectCommandHandler(
        IAggregateRepository repository,
        IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        // プロジェクトを取得
        var project = await _repository.GetByIdAsync<ProjectAggregate>(request.ProjectId);

        if (project == null)
        {
            throw new InvalidOperationException($"プロジェクト（ID: {request.ProjectId}）が見つかりません");
        }

        // カスケード削除: 関連タスクを先に削除
        var taskIds = await _repository.GetTaskIdsByProjectIdAsync(request.ProjectId);
        
        // 各タスクを削除（並列処理はせず順次処理でトランザクションの整合性を保つ）
        var deleteTasks = taskIds.Select(async taskId =>
        {
            var task = await _repository.GetByIdAsync<TaskAggregate>(taskId);
            if (task != null)
            {
                task.Delete(request.DeletedBy, _dateTimeProvider);
                await _repository.SaveAsync(task);
            }
        });

        // すべてのタスク削除を実行
        await Task.WhenAll(deleteTasks);

        // プロジェクトを削除
        project.Delete(request.DeletedBy, _dateTimeProvider);

        // 保存
        await _repository.SaveAsync(project);
    }
}

using MediatR;
using RewindPM.Application.Write.Commands.Projects;
using RewindPM.Application.Write.Repositories;
using RewindPM.Domain.Aggregates;
using RewindPM.Domain.Common;

namespace RewindPM.Application.Write.CommandHandlers.Projects;

/// <summary>
/// プロジェクト削除コマンドのハンドラ
/// </summary>
public class DeleteProjectCommandHandler : IRequestHandler<DeleteProjectCommand>
{
    private readonly IAggregateRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeleteProjectCommandHandler(IAggregateRepository repository, IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        // Aggregateを取得
        var project = await _repository.GetByIdAsync<ProjectAggregate>(request.ProjectId);

        if (project == null)
        {
            throw new InvalidOperationException($"プロジェクト（ID: {request.ProjectId}）が見つかりません");
        }

        // 削除
        project.Delete(request.DeletedBy, _dateTimeProvider);

        // 保存
        await _repository.SaveAsync(project);
    }
}

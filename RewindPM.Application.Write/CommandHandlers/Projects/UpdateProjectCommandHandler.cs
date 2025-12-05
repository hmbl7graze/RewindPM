using MediatR;
using RewindPM.Application.Write.Commands.Projects;
using RewindPM.Application.Write.Repositories;
using RewindPM.Domain.Aggregates;

namespace RewindPM.Application.Write.CommandHandlers.Projects;

/// <summary>
/// プロジェクト更新コマンドのハンドラ
/// </summary>
public class UpdateProjectCommandHandler : IRequestHandler<UpdateProjectCommand>
{
    private readonly IAggregateRepository _repository;

    public UpdateProjectCommandHandler(IAggregateRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
    {
        // Aggregateを取得
        var project = await _repository.GetByIdAsync<ProjectAggregate>(request.ProjectId);

        if (project == null)
        {
            throw new InvalidOperationException($"プロジェクト（ID: {request.ProjectId}）が見つかりません");
        }

        // 更新
        project.Update(request.Title, request.Description, request.UpdatedBy);

        // 保存
        await _repository.SaveAsync(project);
    }
}

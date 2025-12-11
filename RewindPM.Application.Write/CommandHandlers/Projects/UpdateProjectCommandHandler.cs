using MediatR;
using RewindPM.Application.Write.Commands.Projects;
using RewindPM.Application.Write.Repositories;
using RewindPM.Domain.Aggregates;
using RewindPM.Domain.Common;

namespace RewindPM.Application.Write.CommandHandlers.Projects;

/// <summary>
/// プロジェクト更新コマンドのハンドラ
/// </summary>
public class UpdateProjectCommandHandler : IRequestHandler<UpdateProjectCommand>
{
    private readonly IAggregateRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateProjectCommandHandler(IAggregateRepository repository, IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
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
        project.Update(request.Title, request.Description, request.UpdatedBy, _dateTimeProvider);

        // 保存
        await _repository.SaveAsync(project);
    }
}

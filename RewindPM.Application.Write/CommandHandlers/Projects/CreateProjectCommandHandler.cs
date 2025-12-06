using MediatR;
using RewindPM.Application.Write.Commands.Projects;
using RewindPM.Application.Write.Repositories;
using RewindPM.Domain.Aggregates;

namespace RewindPM.Application.Write.CommandHandlers.Projects;

/// <summary>
/// プロジェクト作成コマンドのハンドラ
/// </summary>
public class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, Guid>
{
    private readonly IAggregateRepository _repository;

    public CreateProjectCommandHandler(IAggregateRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        // Aggregateを作成
        var project = ProjectAggregate.Create(
            request.Id,
            request.Title,
            request.Description,
            request.CreatedBy
        );

        // リポジトリに保存
        await _repository.SaveAsync(project);

        return project.Id;
    }
}

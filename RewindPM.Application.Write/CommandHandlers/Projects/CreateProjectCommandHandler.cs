using MediatR;
using RewindPM.Application.Write.Commands.Projects;
using RewindPM.Application.Write.Repositories;
using RewindPM.Domain.Aggregates;
using RewindPM.Domain.Common;

namespace RewindPM.Application.Write.CommandHandlers.Projects;

/// <summary>
/// プロジェクト作成コマンドのハンドラ
/// </summary>
public class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, Guid>
{
    private readonly IAggregateRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateProjectCommandHandler(IAggregateRepository repository, IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Guid> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        // Aggregateを作成
        var project = ProjectAggregate.Create(
            request.Id,
            request.Title,
            request.Description,
            request.CreatedBy,
            _dateTimeProvider
        );

        // リポジトリに保存
        await _repository.SaveAsync(project);

        return project.Id;
    }
}

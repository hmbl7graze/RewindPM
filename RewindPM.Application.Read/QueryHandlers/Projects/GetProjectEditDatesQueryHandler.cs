using MediatR;
using RewindPM.Application.Read.Queries.Projects;
using RewindPM.Application.Read.Repositories;

namespace RewindPM.Application.Read.QueryHandlers.Projects;

/// <summary>
/// GetProjectEditDatesQueryのハンドラー
/// </summary>
public class GetProjectEditDatesQueryHandler : IRequestHandler<GetProjectEditDatesQuery, List<DateTime>>
{
    private readonly IReadModelRepository _repository;

    public GetProjectEditDatesQueryHandler(IReadModelRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<DateTime>> Handle(GetProjectEditDatesQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetProjectEditDatesAsync(request.ProjectId, request.Ascending, cancellationToken);
    }
}

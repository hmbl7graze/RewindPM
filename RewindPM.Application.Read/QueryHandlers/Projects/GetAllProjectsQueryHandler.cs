using MediatR;
using RewindPM.Application.Read.DTOs;
using RewindPM.Application.Read.Queries.Projects;
using RewindPM.Application.Read.Repositories;

namespace RewindPM.Application.Read.QueryHandlers.Projects;

/// <summary>
/// GetAllProjectsQueryのハンドラー
/// </summary>
public class GetAllProjectsQueryHandler : IRequestHandler<GetAllProjectsQuery, List<ProjectDto>>
{
    private readonly IReadModelRepository _repository;

    public GetAllProjectsQueryHandler(IReadModelRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<ProjectDto>> Handle(GetAllProjectsQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetAllProjectsAsync();
    }
}

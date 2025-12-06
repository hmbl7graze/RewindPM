using MediatR;
using RewindPM.Application.Read.DTOs;
using RewindPM.Application.Read.Queries.Projects;
using RewindPM.Application.Read.Repositories;

namespace RewindPM.Application.Read.QueryHandlers.Projects;

/// <summary>
/// GetProjectAtTimeQueryのハンドラー
/// </summary>
public class GetProjectAtTimeQueryHandler : IRequestHandler<GetProjectAtTimeQuery, ProjectDto?>
{
    private readonly IReadModelRepository _repository;

    public GetProjectAtTimeQueryHandler(IReadModelRepository repository)
    {
        _repository = repository;
    }

    public async Task<ProjectDto?> Handle(GetProjectAtTimeQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetProjectAtTimeAsync(request.ProjectId, request.PointInTime);
    }
}

using MediatR;
using RewindPM.Application.Read.DTOs;
using RewindPM.Application.Read.Queries.Tasks;
using RewindPM.Application.Read.Repositories;

namespace RewindPM.Application.Read.QueryHandlers.Tasks;

/// <summary>
/// GetTasksByProjectIdAtTimeQueryのハンドラー
/// </summary>
public class GetTasksByProjectIdAtTimeQueryHandler : IRequestHandler<GetTasksByProjectIdAtTimeQuery, List<TaskDto>>
{
    private readonly IReadModelRepository _repository;

    public GetTasksByProjectIdAtTimeQueryHandler(IReadModelRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<TaskDto>> Handle(GetTasksByProjectIdAtTimeQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetTasksByProjectIdAtTimeAsync(request.ProjectId, request.PointInTime);
    }
}

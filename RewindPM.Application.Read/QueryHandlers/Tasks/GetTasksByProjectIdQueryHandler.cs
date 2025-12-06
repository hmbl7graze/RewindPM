using MediatR;
using RewindPM.Application.Read.DTOs;
using RewindPM.Application.Read.Queries.Tasks;
using RewindPM.Application.Read.Repositories;

namespace RewindPM.Application.Read.QueryHandlers.Tasks;

/// <summary>
/// GetTasksByProjectIdQueryのハンドラー
/// </summary>
public class GetTasksByProjectIdQueryHandler : IRequestHandler<GetTasksByProjectIdQuery, List<TaskDto>>
{
    private readonly IReadModelRepository _repository;

    public GetTasksByProjectIdQueryHandler(IReadModelRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<TaskDto>> Handle(GetTasksByProjectIdQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetTasksByProjectIdAsync(request.ProjectId);
    }
}

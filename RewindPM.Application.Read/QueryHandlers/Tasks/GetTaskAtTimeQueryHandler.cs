using MediatR;
using RewindPM.Application.Read.DTOs;
using RewindPM.Application.Read.Queries.Tasks;
using RewindPM.Application.Read.Repositories;

namespace RewindPM.Application.Read.QueryHandlers.Tasks;

/// <summary>
/// GetTaskAtTimeQueryのハンドラー
/// </summary>
public class GetTaskAtTimeQueryHandler : IRequestHandler<GetTaskAtTimeQuery, TaskDto?>
{
    private readonly IReadModelRepository _repository;

    public GetTaskAtTimeQueryHandler(IReadModelRepository repository)
    {
        _repository = repository;
    }

    public async Task<TaskDto?> Handle(GetTaskAtTimeQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetTaskAtTimeAsync(request.TaskId, request.PointInTime);
    }
}

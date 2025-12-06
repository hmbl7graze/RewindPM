using MediatR;
using RewindPM.Application.Read.DTOs;
using RewindPM.Application.Read.Queries.Tasks;
using RewindPM.Application.Read.Repositories;

namespace RewindPM.Application.Read.QueryHandlers.Tasks;

/// <summary>
/// GetTaskByIdQueryのハンドラー
/// </summary>
public class GetTaskByIdQueryHandler : IRequestHandler<GetTaskByIdQuery, TaskDto?>
{
    private readonly IReadModelRepository _repository;

    public GetTaskByIdQueryHandler(IReadModelRepository repository)
    {
        _repository = repository;
    }

    public async Task<TaskDto?> Handle(GetTaskByIdQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetTaskByIdAsync(request.TaskId);
    }
}

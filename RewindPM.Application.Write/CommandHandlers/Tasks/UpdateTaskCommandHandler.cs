using MediatR;
using RewindPM.Application.Write.Commands.Tasks;
using RewindPM.Application.Write.Repositories;
using RewindPM.Domain.Aggregates;

namespace RewindPM.Application.Write.CommandHandlers.Tasks;

/// <summary>
/// タスク更新コマンドのハンドラ
/// </summary>
public class UpdateTaskCommandHandler : IRequestHandler<UpdateTaskCommand>
{
    private readonly IAggregateRepository _repository;

    public UpdateTaskCommandHandler(IAggregateRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
    {
        // Aggregateを取得
        var task = await _repository.GetByIdAsync<TaskAggregate>(request.TaskId);

        if (task == null)
        {
            throw new InvalidOperationException($"タスク（ID: {request.TaskId}）が見つかりません");
        }

        // 更新
        task.Update(request.Title, request.Description, request.UpdatedBy);

        // 保存
        await _repository.SaveAsync(task);
    }
}

using MediatR;
using RewindPM.Application.Write.Commands.Tasks;
using RewindPM.Application.Write.Repositories;
using RewindPM.Domain.Aggregates;

namespace RewindPM.Application.Write.CommandHandlers.Tasks;

/// <summary>
/// タスクステータス変更コマンドのハンドラ
/// </summary>
public class ChangeTaskStatusCommandHandler : IRequestHandler<ChangeTaskStatusCommand>
{
    private readonly IAggregateRepository _repository;

    public ChangeTaskStatusCommandHandler(IAggregateRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(ChangeTaskStatusCommand request, CancellationToken cancellationToken)
    {
        // Aggregateを取得
        var task = await _repository.GetByIdAsync<TaskAggregate>(request.TaskId);

        if (task == null)
        {
            throw new InvalidOperationException($"タスク（ID: {request.TaskId}）が見つかりません");
        }

        // ステータス変更
        task.ChangeStatus(request.NewStatus, request.ChangedBy);

        // 保存
        await _repository.SaveAsync(task);
    }
}

using MediatR;
using RewindPM.Application.Write.Commands.Tasks;
using RewindPM.Application.Write.Repositories;
using RewindPM.Domain.Aggregates;
using RewindPM.Domain.Common;

namespace RewindPM.Application.Write.CommandHandlers.Tasks;

/// <summary>
/// タスク削除コマンドのハンドラ
/// </summary>
public class DeleteTaskCommandHandler : IRequestHandler<DeleteTaskCommand>
{
    private readonly IAggregateRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeleteTaskCommandHandler(IAggregateRepository repository, IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        // Aggregateを取得
        var task = await _repository.GetByIdAsync<TaskAggregate>(request.TaskId);

        if (task == null)
        {
            throw new InvalidOperationException($"タスク（ID: {request.TaskId}）が見つかりません");
        }

        // 削除
        task.Delete(request.DeletedBy, _dateTimeProvider);

        // 保存
        await _repository.SaveAsync(task);
    }
}

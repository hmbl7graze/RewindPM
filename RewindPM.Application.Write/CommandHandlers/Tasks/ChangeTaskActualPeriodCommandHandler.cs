using MediatR;
using RewindPM.Application.Write.Commands.Tasks;
using RewindPM.Application.Write.Repositories;
using RewindPM.Domain.Aggregates;
using RewindPM.Domain.ValueObjects;

namespace RewindPM.Application.Write.CommandHandlers.Tasks;

/// <summary>
/// タスク実績期間変更コマンドのハンドラ
/// </summary>
public class ChangeTaskActualPeriodCommandHandler : IRequestHandler<ChangeTaskActualPeriodCommand>
{
    private readonly IAggregateRepository _repository;

    public ChangeTaskActualPeriodCommandHandler(IAggregateRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(ChangeTaskActualPeriodCommand request, CancellationToken cancellationToken)
    {
        // Aggregateを取得
        var task = await _repository.GetByIdAsync<TaskAggregate>(request.TaskId);

        if (task == null)
        {
            throw new InvalidOperationException($"タスク（ID: {request.TaskId}）が見つかりません");
        }

        // ActualPeriod ValueObjectを作成
        var actualPeriod = new ActualPeriod(
            request.ActualStartDate,
            request.ActualEndDate,
            request.ActualHours
        );

        // 実績期間変更
        task.ChangeActualPeriod(actualPeriod, request.ChangedBy);

        // 保存
        await _repository.SaveAsync(task);
    }
}

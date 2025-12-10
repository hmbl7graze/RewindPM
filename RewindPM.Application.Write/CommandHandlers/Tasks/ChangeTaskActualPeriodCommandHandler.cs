using MediatR;
using RewindPM.Application.Write.Commands.Tasks;
using RewindPM.Application.Write.Repositories;
using RewindPM.Domain.Aggregates;
using RewindPM.Domain.ValueObjects;
using RewindPM.Domain.Common;

namespace RewindPM.Application.Write.CommandHandlers.Tasks;

/// <summary>
/// タスク実績期間変更コマンドのハンドラ
/// </summary>
public class ChangeTaskActualPeriodCommandHandler : IRequestHandler<ChangeTaskActualPeriodCommand>
{
    private readonly IAggregateRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ChangeTaskActualPeriodCommandHandler(IAggregateRepository repository, IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
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
        task.ChangeActualPeriod(actualPeriod, request.ChangedBy, _dateTimeProvider);

        // 保存
        await _repository.SaveAsync(task);
    }
}

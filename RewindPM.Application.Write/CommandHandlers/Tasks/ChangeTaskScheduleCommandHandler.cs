using MediatR;
using RewindPM.Application.Write.Commands.Tasks;
using RewindPM.Application.Write.Repositories;
using RewindPM.Domain.Aggregates;
using RewindPM.Domain.ValueObjects;
using RewindPM.Domain.Common;

namespace RewindPM.Application.Write.CommandHandlers.Tasks;

/// <summary>
/// タスク予定期間変更コマンドのハンドラ
/// </summary>
public class ChangeTaskScheduleCommandHandler : IRequestHandler<ChangeTaskScheduleCommand>
{
    private readonly IAggregateRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ChangeTaskScheduleCommandHandler(IAggregateRepository repository, IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task Handle(ChangeTaskScheduleCommand request, CancellationToken cancellationToken)
    {
        // Aggregateを取得
        var task = await _repository.GetByIdAsync<TaskAggregate>(request.TaskId);

        if (task == null)
        {
            throw new InvalidOperationException($"タスク（ID: {request.TaskId}）が見つかりません");
        }

        // ScheduledPeriod ValueObjectを作成
        var scheduledPeriod = new ScheduledPeriod(
            request.ScheduledStartDate,
            request.ScheduledEndDate,
            request.EstimatedHours
        );

        // 予定期間変更
        task.ChangeSchedule(scheduledPeriod, request.ChangedBy, _dateTimeProvider);

        // 保存
        await _repository.SaveAsync(task);
    }
}

using MediatR;
using RewindPM.Application.Write.Commands.Tasks;
using RewindPM.Application.Write.Repositories;
using RewindPM.Domain.Aggregates;
using RewindPM.Domain.Common;
using RewindPM.Domain.ValueObjects;

namespace RewindPM.Application.Write.CommandHandlers.Tasks;

/// <summary>
/// タスク完全更新コマンドのハンドラ
/// </summary>
public class UpdateTaskCompleteCommandHandler : IRequestHandler<UpdateTaskCompleteCommand>
{
    private readonly IAggregateRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public UpdateTaskCompleteCommandHandler(IAggregateRepository repository, IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task Handle(UpdateTaskCompleteCommand request, CancellationToken cancellationToken)
    {
        // Aggregateを取得
        var task = await _repository.GetByIdAsync<TaskAggregate>(request.TaskId);

        if (task == null)
        {
            throw new InvalidOperationException($"タスク（ID: {request.TaskId}）が見つかりません");
        }

        // Value Objectsの作成
        var scheduledPeriod = new ScheduledPeriod(
            request.ScheduledStartDate,
            request.ScheduledEndDate,
            request.EstimatedHours
        );

        var actualPeriod = new ActualPeriod(
            request.ActualStartDate,
            request.ActualEndDate,
            request.ActualHours
        );

        // 一括更新
        task.UpdateCompletely(
            request.Title,
            request.Description,
            request.Status,
            scheduledPeriod,
            actualPeriod,
            request.UpdatedBy,
            _dateTimeProvider
        );

        // 保存
        await _repository.SaveAsync(task);
    }
}

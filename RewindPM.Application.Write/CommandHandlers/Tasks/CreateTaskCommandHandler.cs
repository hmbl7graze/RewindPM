using MediatR;
using RewindPM.Application.Write.Commands.Tasks;
using RewindPM.Application.Write.Repositories;
using RewindPM.Domain.Aggregates;
using RewindPM.Domain.ValueObjects;
using RewindPM.Domain.Common;

namespace RewindPM.Application.Write.CommandHandlers.Tasks;

/// <summary>
/// タスク作成コマンドのハンドラ
/// </summary>
public class CreateTaskCommandHandler : IRequestHandler<CreateTaskCommand, Guid>
{
    private readonly IAggregateRepository _repository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateTaskCommandHandler(IAggregateRepository repository, IDateTimeProvider dateTimeProvider)
    {
        _repository = repository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Guid> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        // ScheduledPeriod ValueObjectを作成
        var scheduledPeriod = new ScheduledPeriod(
            request.ScheduledStartDate,
            request.ScheduledEndDate,
            request.EstimatedHours
        );

        // Aggregateを作成
        var task = TaskAggregate.Create(
            request.Id,
            request.ProjectId,
            request.Title,
            request.Description,
            scheduledPeriod,
            request.CreatedBy,
            _dateTimeProvider
        );

        // 実績期間が設定されている場合は、実績を設定
        if (request.ActualStartDate.HasValue || request.ActualEndDate.HasValue || request.ActualHours.HasValue)
        {
            var actualPeriod = new ActualPeriod(
                request.ActualStartDate,
                request.ActualEndDate,
                request.ActualHours
            );
            task.ChangeActualPeriod(actualPeriod, request.CreatedBy, _dateTimeProvider);
        }

        // リポジトリに保存
        await _repository.SaveAsync(task);

        return task.Id;
    }
}

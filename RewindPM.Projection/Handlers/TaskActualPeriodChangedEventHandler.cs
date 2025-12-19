using Microsoft.Extensions.Logging;
using RewindPM.Domain.Common;
using RewindPM.Domain.Events;
using RewindPM.Infrastructure.Read.Persistence;
using RewindPM.Projection.Services;

namespace RewindPM.Projection.Handlers;

/// <summary>
/// TaskActualPeriodChangedイベントを処理してReadModelを更新するハンドラー
/// </summary>
public class TaskActualPeriodChangedEventHandler : IEventHandler<TaskActualPeriodChanged>
{
    private readonly ReadModelDbContext _context;
    private readonly TaskSnapshotService _snapshotService;
    private readonly ILogger<TaskActualPeriodChangedEventHandler> _logger;

    public TaskActualPeriodChangedEventHandler(
        ReadModelDbContext context,
        TaskSnapshotService snapshotService,
        ILogger<TaskActualPeriodChangedEventHandler> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _snapshotService = snapshotService ?? throw new ArgumentNullException(nameof(snapshotService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(TaskActualPeriodChanged @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        _logger.LogInformation("Handling TaskActualPeriodChanged event for task {AggregateId}", @event.AggregateId);

        // 現在の状態を更新
        var task = await _context.Tasks.FindAsync(@event.AggregateId);
        if (task == null)
        {
            _logger.LogWarning("Task {TaskId} not found in ReadModel", @event.AggregateId);
            return;
        }

        task.ActualStartDate = @event.ActualPeriod.StartDate;
        task.ActualEndDate = @event.ActualPeriod.EndDate;
        task.ActualHours = @event.ActualPeriod.ActualHours;
        task.UpdatedAt = @event.OccurredAt;
        task.UpdatedBy = @event.ChangedBy;

        // 当日のスナップショットを作成または更新
        await _snapshotService.UpsertTaskSnapshotAsync(@event.AggregateId, task, @event.OccurredAt);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Successfully updated actual period for task {TaskId}", @event.AggregateId);
    }
}

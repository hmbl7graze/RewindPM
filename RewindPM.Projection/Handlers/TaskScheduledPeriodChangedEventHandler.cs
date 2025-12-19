using Microsoft.Extensions.Logging;
using RewindPM.Domain.Common;
using RewindPM.Domain.Events;
using RewindPM.Infrastructure.Read.Persistence;
using RewindPM.Projection.Services;

namespace RewindPM.Projection.Handlers;

/// <summary>
/// TaskScheduledPeriodChangedイベントを処理してReadModelを更新するハンドラー
/// </summary>
public class TaskScheduledPeriodChangedEventHandler : IEventHandler<TaskScheduledPeriodChanged>
{
    private readonly ReadModelDbContext _context;
    private readonly TaskSnapshotService _snapshotService;
    private readonly ILogger<TaskScheduledPeriodChangedEventHandler> _logger;

    public TaskScheduledPeriodChangedEventHandler(
        ReadModelDbContext context,
        TaskSnapshotService snapshotService,
        ILogger<TaskScheduledPeriodChangedEventHandler> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _snapshotService = snapshotService ?? throw new ArgumentNullException(nameof(snapshotService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(TaskScheduledPeriodChanged @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        _logger.LogInformation("Handling TaskScheduledPeriodChanged event for task {AggregateId}", @event.AggregateId);

        // 現在の状態を更新
        var task = await _context.Tasks.FindAsync(@event.AggregateId);
        if (task == null)
        {
            _logger.LogWarning("Task {TaskId} not found in ReadModel", @event.AggregateId);
            return;
        }

        task.ScheduledStartDate = @event.ScheduledPeriod.StartDate;
        task.ScheduledEndDate = @event.ScheduledPeriod.EndDate;
        task.EstimatedHours = @event.ScheduledPeriod.EstimatedHours;
        task.UpdatedAt = @event.OccurredAt;
        task.UpdatedBy = @event.ChangedBy;

        // 当日のスナップショットを作成または更新
        await _snapshotService.UpsertTaskSnapshotAsync(@event.AggregateId, task, @event.OccurredAt);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Successfully updated scheduled period for task {TaskId}", @event.AggregateId);
    }
}

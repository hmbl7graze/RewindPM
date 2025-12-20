using Microsoft.Extensions.Logging;
using RewindPM.Domain.Common;
using RewindPM.Domain.Events;
using RewindPM.Infrastructure.Read.Services;
using RewindPM.Projection.Services;

namespace RewindPM.Projection.Handlers;

/// <summary>
/// TaskCompletelyUpdatedイベントを処理してReadModelを更新するハンドラー
/// </summary>
public class TaskCompletelyUpdatedEventHandler : IEventHandler<TaskCompletelyUpdated>
{
    private readonly IReadModelContext _context;
    private readonly TaskSnapshotService _snapshotService;
    private readonly ILogger<TaskCompletelyUpdatedEventHandler> _logger;

    public TaskCompletelyUpdatedEventHandler(
        IReadModelContext context,
        TaskSnapshotService snapshotService,
        ILogger<TaskCompletelyUpdatedEventHandler> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _snapshotService = snapshotService ?? throw new ArgumentNullException(nameof(snapshotService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(TaskCompletelyUpdated @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        _logger.LogInformation("Handling TaskCompletelyUpdated event for task {AggregateId}", @event.AggregateId);

        // 現在の状態を更新
        var task = _context.Tasks.FirstOrDefault(t => t.Id == @event.AggregateId);
        if (task == null)
        {
            _logger.LogWarning("Task {TaskId} not found in ReadModel", @event.AggregateId);
            return;
        }

        // すべてのプロパティを更新
        task.Title = @event.Title;
        task.Description = @event.Description;
        task.Status = @event.Status;
        task.ScheduledStartDate = @event.ScheduledPeriod.StartDate;
        task.ScheduledEndDate = @event.ScheduledPeriod.EndDate;
        task.EstimatedHours = @event.ScheduledPeriod.EstimatedHours;
        task.ActualStartDate = @event.ActualPeriod.StartDate;
        task.ActualEndDate = @event.ActualPeriod.EndDate;
        task.ActualHours = @event.ActualPeriod.ActualHours;
        task.UpdatedAt = @event.OccurredAt;
        task.UpdatedBy = @event.UpdatedBy;

        // 当日のスナップショットを作成または更新
        await _snapshotService.PrepareTaskSnapshotAsync(@event.AggregateId, task, @event.OccurredAt);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Successfully updated task {TaskId} completely", @event.AggregateId);
    }
}

using Microsoft.Extensions.Logging;
using RewindPM.Domain.Common;
using RewindPM.Domain.Events;
using RewindPM.Infrastructure.Read.SQLite.Persistence;
using RewindPM.Projection.Services;

namespace RewindPM.Projection.Handlers;

/// <summary>
/// TaskStatusChangedイベントを処理してReadModelを更新するハンドラー
/// </summary>
public class TaskStatusChangedEventHandler : IEventHandler<TaskStatusChanged>
{
    private readonly ReadModelDbContext _context;
    private readonly TaskSnapshotService _snapshotService;
    private readonly ILogger<TaskStatusChangedEventHandler> _logger;

    public TaskStatusChangedEventHandler(
        ReadModelDbContext context,
        TaskSnapshotService snapshotService,
        ILogger<TaskStatusChangedEventHandler> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _snapshotService = snapshotService ?? throw new ArgumentNullException(nameof(snapshotService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(TaskStatusChanged @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        _logger.LogInformation("Handling TaskStatusChanged event for task {AggregateId}: {OldStatus} -> {NewStatus}",
            @event.AggregateId, @event.OldStatus, @event.NewStatus);

        // 現在の状態を更新
        var task = await _context.Tasks.FindAsync(@event.AggregateId);
        if (task == null)
        {
            _logger.LogWarning("Task {TaskId} not found in ReadModel", @event.AggregateId);
            return;
        }

        task.Status = @event.NewStatus;
        task.UpdatedAt = @event.OccurredAt;
        task.UpdatedBy = @event.ChangedBy;

        // 当日のスナップショットを作成または更新
        await _snapshotService.PrepareTaskSnapshotAsync(@event.AggregateId, task, @event.OccurredAt);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Successfully updated task status for {TaskId}", @event.AggregateId);
    }
}

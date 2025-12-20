using Microsoft.Extensions.Logging;
using RewindPM.Domain.Common;
using RewindPM.Domain.Events;
using RewindPM.Infrastructure.Read.Services;
using RewindPM.Projection.Services;

namespace RewindPM.Projection.Handlers;

/// <summary>
/// TaskUpdatedイベントを処理してReadModelを更新するハンドラー
/// </summary>
public class TaskUpdatedEventHandler : IEventHandler<TaskUpdated>
{
    private readonly IReadModelContext _context;
    private readonly TaskSnapshotService _snapshotService;
    private readonly ILogger<TaskUpdatedEventHandler> _logger;

    public TaskUpdatedEventHandler(
        IReadModelContext context,
        TaskSnapshotService snapshotService,
        ILogger<TaskUpdatedEventHandler> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _snapshotService = snapshotService ?? throw new ArgumentNullException(nameof(snapshotService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(TaskUpdated @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        _logger.LogInformation("Handling TaskUpdated event for task {AggregateId}", @event.AggregateId);

        // 現在の状態を更新
        var task = _context.Tasks.FirstOrDefault(t => t.Id == @event.AggregateId);
        if (task == null)
        {
            _logger.LogWarning("Task {TaskId} not found in ReadModel", @event.AggregateId);
            return;
        }

        task.Title = @event.Title;
        task.Description = @event.Description;
        task.UpdatedAt = @event.OccurredAt;
        task.UpdatedBy = @event.UpdatedBy;

        // 当日のスナップショットを作成または更新
        await _snapshotService.PrepareTaskSnapshotAsync(@event.AggregateId, task, @event.OccurredAt);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Successfully updated task {TaskId}", @event.AggregateId);
    }
}

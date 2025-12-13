using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RewindPM.Domain.Common;
using RewindPM.Domain.Events;
using RewindPM.Infrastructure.Read.Persistence;

namespace RewindPM.Projection.Handlers;

/// <summary>
/// TaskDeletedイベントを処理してReadModelを更新するハンドラー
/// </summary>
public class TaskDeletedEventHandler : IEventHandler<TaskDeleted>
{
    private readonly ReadModelDbContext _context;
    private readonly ILogger<TaskDeletedEventHandler> _logger;

    public TaskDeletedEventHandler(
        ReadModelDbContext context,
        ILogger<TaskDeletedEventHandler> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(TaskDeleted @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        _logger.LogInformation("Handling TaskDeleted event for task {AggregateId}", @event.AggregateId);

        // Read Modelの削除フラグを更新
        var task = await _context.Tasks.FindAsync(@event.AggregateId);
        if (task != null)
        {
            task.IsDeleted = true;
            task.DeletedAt = @event.OccurredAt;
            task.DeletedBy = @event.DeletedBy;
            task.UpdatedAt = @event.OccurredAt;

            await _context.SaveChangesAsync();
        }
        else
        {
            _logger.LogWarning("Task {TaskId} not found in read model", @event.AggregateId);
        }

        _logger.LogInformation("Successfully marked task {TaskId} as deleted", @event.AggregateId);
    }
}

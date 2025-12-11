using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RewindPM.Domain.Common;
using RewindPM.Domain.Events;
using RewindPM.Infrastructure.Read.Entities;
using RewindPM.Infrastructure.Read.Persistence;
using RewindPM.Infrastructure.Read.Services;

namespace RewindPM.Projection.Handlers;

/// <summary>
/// TaskUpdatedイベントを処理してReadModelを更新するハンドラー
/// </summary>
public class TaskUpdatedEventHandler : IEventHandler<TaskUpdated>
{
    private readonly ReadModelDbContext _context;
    private readonly ITimeZoneService _timeZoneService;
    private readonly ILogger<TaskUpdatedEventHandler> _logger;

    public TaskUpdatedEventHandler(
        ReadModelDbContext context,
        ITimeZoneService timeZoneService,
        ILogger<TaskUpdatedEventHandler> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _timeZoneService = timeZoneService ?? throw new ArgumentNullException(nameof(timeZoneService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(TaskUpdated @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        _logger.LogInformation("Handling TaskUpdated event for task {AggregateId}", @event.AggregateId);

        // 現在の状態を更新
        var task = await _context.Tasks.FindAsync(@event.AggregateId);
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
        await UpsertTaskSnapshotAsync(@event.AggregateId, task, @event.OccurredAt);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Successfully updated task {TaskId}", @event.AggregateId);
    }

    private async Task UpsertTaskSnapshotAsync(Guid taskId, TaskEntity currentState, DateTimeOffset occurredAt)
    {
        var snapshotDate = _timeZoneService.GetSnapshotDate(occurredAt);
        var snapshot = await _context.TaskHistories
            .FirstOrDefaultAsync(h => h.TaskId == taskId && h.SnapshotDate == snapshotDate);

        if (snapshot != null)
        {
            // 既存のスナップショットを更新
            snapshot.Title = currentState.Title;
            snapshot.Description = currentState.Description;
            snapshot.Status = currentState.Status;
            snapshot.ScheduledStartDate = currentState.ScheduledStartDate;
            snapshot.ScheduledEndDate = currentState.ScheduledEndDate;
            snapshot.EstimatedHours = currentState.EstimatedHours;
            snapshot.ActualStartDate = currentState.ActualStartDate;
            snapshot.ActualEndDate = currentState.ActualEndDate;
            snapshot.ActualHours = currentState.ActualHours;
            snapshot.UpdatedAt = occurredAt;
            snapshot.UpdatedBy = currentState.UpdatedBy;

            _logger.LogDebug("Updated existing snapshot for task {TaskId} on {SnapshotDate}",
                taskId, snapshotDate);
        }
        else
        {
            // 新規スナップショットを作成
            snapshot = new TaskHistoryEntity
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                ProjectId = currentState.ProjectId,
                SnapshotDate = snapshotDate,
                Title = currentState.Title,
                Description = currentState.Description,
                Status = currentState.Status,
                ScheduledStartDate = currentState.ScheduledStartDate,
                ScheduledEndDate = currentState.ScheduledEndDate,
                EstimatedHours = currentState.EstimatedHours,
                ActualStartDate = currentState.ActualStartDate,
                ActualEndDate = currentState.ActualEndDate,
                ActualHours = currentState.ActualHours,
                CreatedAt = currentState.CreatedAt,
                UpdatedAt = occurredAt,
                CreatedBy = currentState.CreatedBy,
                UpdatedBy = currentState.UpdatedBy,
                SnapshotCreatedAt = DateTimeOffset.UtcNow
            };

            _context.TaskHistories.Add(snapshot);

            _logger.LogDebug("Created new snapshot for task {TaskId} on {SnapshotDate}",
                taskId, snapshotDate);
        }
    }
}

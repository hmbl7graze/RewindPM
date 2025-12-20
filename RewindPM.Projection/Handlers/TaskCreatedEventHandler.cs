using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RewindPM.Domain.Common;
using RewindPM.Domain.Events;
using RewindPM.Infrastructure.Read.SQLite.Entities;
using RewindPM.Infrastructure.Read.SQLite.Persistence;
using RewindPM.Infrastructure.Read.Services;
using TaskStatus = RewindPM.Domain.ValueObjects.TaskStatus;

namespace RewindPM.Projection.Handlers;

/// <summary>
/// TaskCreatedイベントを処理してReadModelを更新するハンドラー
/// </summary>
public class TaskCreatedEventHandler : IEventHandler<TaskCreated>
{
    private readonly ReadModelDbContext _context;
    private readonly ITimeZoneService _timeZoneService;
    private readonly ILogger<TaskCreatedEventHandler> _logger;

    public TaskCreatedEventHandler(
        ReadModelDbContext context,
        ITimeZoneService timeZoneService,
        ILogger<TaskCreatedEventHandler> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _timeZoneService = timeZoneService ?? throw new ArgumentNullException(nameof(timeZoneService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(TaskCreated @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        _logger.LogInformation("Handling TaskCreated event for task {AggregateId}", @event.AggregateId);

        // 現在の状態をTasksテーブルに追加
        var task = new TaskEntity
        {
            Id = @event.AggregateId,
            ProjectId = @event.ProjectId,
            Title = @event.Title,
            Description = @event.Description,
            Status = TaskStatus.Todo, // 作成時は常にTodo
            ScheduledStartDate = @event.ScheduledPeriod.StartDate,
            ScheduledEndDate = @event.ScheduledPeriod.EndDate,
            EstimatedHours = @event.ScheduledPeriod.EstimatedHours,
            ActualStartDate = null,
            ActualEndDate = null,
            ActualHours = null,
            CreatedAt = @event.OccurredAt,
            UpdatedAt = null,
            CreatedBy = @event.CreatedBy,
            UpdatedBy = null
        };

        _context.Tasks.Add(task);

        // 初回スナップショットをTaskHistoriesテーブルに追加
        var snapshotDate = _timeZoneService.GetSnapshotDate(@event.OccurredAt);
        var snapshot = new TaskHistoryEntity
        {
            Id = Guid.NewGuid(),
            TaskId = @event.AggregateId,
            ProjectId = @event.ProjectId,
            SnapshotDate = snapshotDate,
            Title = @event.Title,
            Description = @event.Description,
            Status = TaskStatus.Todo,
            ScheduledStartDate = @event.ScheduledPeriod.StartDate,
            ScheduledEndDate = @event.ScheduledPeriod.EndDate,
            EstimatedHours = @event.ScheduledPeriod.EstimatedHours,
            ActualStartDate = null,
            ActualEndDate = null,
            ActualHours = null,
            CreatedAt = @event.OccurredAt,
            UpdatedAt = null,
            CreatedBy = @event.CreatedBy,
            UpdatedBy = null,
            SnapshotCreatedAt = DateTimeOffset.UtcNow
        };

        _context.TaskHistories.Add(snapshot);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Successfully created task {TaskId} and snapshot for {SnapshotDate}",
            @event.AggregateId, snapshotDate);
    }
}

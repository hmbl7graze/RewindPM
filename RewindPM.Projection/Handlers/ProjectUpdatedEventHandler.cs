using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RewindPM.Domain.Common;
using RewindPM.Domain.Events;
using RewindPM.Infrastructure.Read.Entities;
using RewindPM.Infrastructure.Read.Services;

namespace RewindPM.Projection.Handlers;

/// <summary>
/// ProjectUpdatedイベントを処理してReadModelを更新するハンドラー
/// </summary>
public class ProjectUpdatedEventHandler : IEventHandler<ProjectUpdated>
{
    private readonly IReadModelContext _context;
    private readonly ITimeZoneService _timeZoneService;
    private readonly ILogger<ProjectUpdatedEventHandler> _logger;

    public ProjectUpdatedEventHandler(
        IReadModelContext context,
        ITimeZoneService timeZoneService,
        ILogger<ProjectUpdatedEventHandler> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _timeZoneService = timeZoneService ?? throw new ArgumentNullException(nameof(timeZoneService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(ProjectUpdated @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        _logger.LogInformation("Handling ProjectUpdated event for project {AggregateId}", @event.AggregateId);

        // Projectsテーブルの現在の状態を更新
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == @event.AggregateId);

        if (project == null)
        {
            _logger.LogWarning("Project {AggregateId} not found for update", @event.AggregateId);
            return;
        }

        project.Title = @event.Title;
        project.Description = @event.Description;
        project.UpdatedAt = @event.OccurredAt;
        project.UpdatedBy = @event.UpdatedBy;

        // スナップショットを作成または更新
        var snapshotDate = _timeZoneService.GetSnapshotDate(@event.OccurredAt);
        var snapshot = await _context.ProjectHistories
            .FirstOrDefaultAsync(h => h.ProjectId == @event.AggregateId && h.SnapshotDate == snapshotDate);

        if (snapshot != null)
        {
            // 既存のスナップショットを更新（同じ日に複数回更新された場合）
            snapshot.Title = @event.Title;
            snapshot.Description = @event.Description;
            snapshot.UpdatedAt = @event.OccurredAt;
            snapshot.UpdatedBy = @event.UpdatedBy;

            _logger.LogDebug("Updated existing snapshot for project {ProjectId} on {SnapshotDate}",
                @event.AggregateId, snapshotDate);
        }
        else
        {
            // 新規スナップショットを作成
            snapshot = new ProjectHistoryEntity
            {
                Id = Guid.NewGuid(),
                ProjectId = @event.AggregateId,
                SnapshotDate = snapshotDate,
                Title = @event.Title,
                Description = @event.Description,
                CreatedAt = project.CreatedAt,
                UpdatedAt = @event.OccurredAt,
                CreatedBy = project.CreatedBy,
                UpdatedBy = @event.UpdatedBy,
                SnapshotCreatedAt = DateTimeOffset.UtcNow
            };

            _context.AddProjectHistory(snapshot);

            _logger.LogDebug("Created new snapshot for project {ProjectId} on {SnapshotDate}",
                @event.AggregateId, snapshotDate);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Successfully updated project {ProjectId}", @event.AggregateId);
    }
}

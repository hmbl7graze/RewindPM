using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RewindPM.Domain.Common;
using RewindPM.Domain.Events;
using RewindPM.Infrastructure.Read.Entities;
using RewindPM.Infrastructure.Read.Persistence;

namespace RewindPM.Projection.Handlers;

/// <summary>
/// ProjectCreatedイベントを処理してReadModelを更新するハンドラー
/// </summary>
public class ProjectCreatedEventHandler : IEventHandler<ProjectCreated>
{
    private readonly ReadModelDbContext _context;
    private readonly ILogger<ProjectCreatedEventHandler> _logger;

    public ProjectCreatedEventHandler(
        ReadModelDbContext context,
        ILogger<ProjectCreatedEventHandler> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(ProjectCreated @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        _logger.LogInformation("Handling ProjectCreated event for project {AggregateId}", @event.AggregateId);

        // 現在の状態をProjectsテーブルに追加
        var project = new ProjectEntity
        {
            Id = @event.AggregateId,
            Title = @event.Title,
            Description = @event.Description,
            CreatedAt = @event.OccurredAt,
            UpdatedAt = null,
            CreatedBy = @event.CreatedBy,
            UpdatedBy = null
        };

        _context.Projects.Add(project);

        // 初回スナップショットをProjectHistoriesテーブルに追加
        var snapshotDate = @event.OccurredAt.Date;
        var snapshot = new ProjectHistoryEntity
        {
            Id = Guid.NewGuid(),
            ProjectId = @event.AggregateId,
            SnapshotDate = snapshotDate,
            Title = @event.Title,
            Description = @event.Description,
            CreatedAt = @event.OccurredAt,
            UpdatedAt = null,
            CreatedBy = @event.CreatedBy,
            UpdatedBy = null,
            SnapshotCreatedAt = DateTime.UtcNow
        };

        _context.ProjectHistories.Add(snapshot);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Successfully created project {ProjectId} and snapshot for {SnapshotDate}",
            @event.AggregateId, snapshotDate);
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RewindPM.Domain.Common;
using RewindPM.Domain.Events;
using RewindPM.Infrastructure.Read.Services;

namespace RewindPM.Projection.Handlers;

/// <summary>
/// ProjectDeletedイベントを処理してReadModelを更新するハンドラー
/// </summary>
public class ProjectDeletedEventHandler : IEventHandler<ProjectDeleted>
{
    private readonly IReadModelContext _context;
    private readonly ILogger<ProjectDeletedEventHandler> _logger;

    public ProjectDeletedEventHandler(
        IReadModelContext context,
        ILogger<ProjectDeletedEventHandler> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(ProjectDeleted @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        _logger.LogInformation("Handling ProjectDeleted event for project {AggregateId}", @event.AggregateId);

        // Read Modelの削除フラグを更新
        var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == @event.AggregateId);
        if (project != null)
        {
            project.IsDeleted = true;
            project.DeletedAt = @event.OccurredAt;
            project.DeletedBy = @event.DeletedBy;

            await _context.SaveChangesAsync();
        }
        else
        {
            _logger.LogWarning("Project {ProjectId} not found in read model", @event.AggregateId);
        }

        _logger.LogInformation("Successfully marked project {ProjectId} as deleted", @event.AggregateId);
    }
}

using Microsoft.EntityFrameworkCore;
using RewindPM.Application.Read.DTOs;
using RewindPM.Application.Read.Repositories;
using RewindPM.Infrastructure.Read.Persistence;
using TaskStatus = RewindPM.Domain.ValueObjects.TaskStatus;

namespace RewindPM.Infrastructure.Read.Repositories;

/// <summary>
/// プロジェクト統計情報リポジトリの実装
/// </summary>
public class ProjectStatisticsRepository : IProjectStatisticsRepository
{
    private readonly ReadModelDbContext _context;

    public ProjectStatisticsRepository(ReadModelDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// プロジェクトカード用の統計情報を取得
    /// </summary>
    public async Task<ProjectStatisticsSummaryDto> GetProjectStatisticsSummaryAsync(
        Guid projectId,
        CancellationToken cancellationToken = default)
    {
        // プロジェクトに属するすべてのタスクを取得
        var tasks = await _context.Tasks
            .Where(t => t.ProjectId == projectId)
            .ToListAsync(cancellationToken);

        // ステータスごとにカウント
        var totalTasks = tasks.Count;
        var completedTasks = tasks.Count(t => t.Status == TaskStatus.Done);
        var inProgressTasks = tasks.Count(t => t.Status == TaskStatus.InProgress);
        var inReviewTasks = tasks.Count(t => t.Status == TaskStatus.InReview);
        var todoTasks = tasks.Count(t => t.Status == TaskStatus.Todo);

        return new ProjectStatisticsSummaryDto
        {
            ProjectId = projectId,
            TotalTasks = totalTasks,
            CompletedTasks = completedTasks,
            InProgressTasks = inProgressTasks,
            InReviewTasks = inReviewTasks,
            TodoTasks = todoTasks
        };
    }
}

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

    /// <summary>
    /// プロジェクト詳細画面用の統計情報を取得
    /// </summary>
    public async Task<ProjectStatisticsDetailDto?> GetProjectStatisticsDetailAsync(
        Guid projectId,
        DateTime asOfDate,
        CancellationToken cancellationToken = default)
    {
        // 指定日時以前に作成されたタスクを取得
        var tasks = await _context.Tasks
            .Where(t => t.ProjectId == projectId && t.CreatedAt <= asOfDate)
            .ToListAsync(cancellationToken);

        // タスクが1件もなければプロジェクトの存在確認
        if (!tasks.Any())
        {
            var projectExists = await _context.Projects
                .AnyAsync(p => p.Id == projectId, cancellationToken);
            if (!projectExists)
            {
                return null;
            }
        }

        // タスクステータスのカウント
        var totalTasks = tasks.Count;
        var completedTasks = tasks.Count(t => t.Status == TaskStatus.Done);
        var inProgressTasks = tasks.Count(t => t.Status == TaskStatus.InProgress);
        var inReviewTasks = tasks.Count(t => t.Status == TaskStatus.InReview);
        var todoTasks = tasks.Count(t => t.Status == TaskStatus.Todo);

        // 工数統計の計算
        var totalEstimatedHours = tasks
            .Where(t => t.EstimatedHours.HasValue)
            .Sum(t => t.EstimatedHours!.Value);

        var totalActualHours = tasks
            .Where(t => t.ActualHours.HasValue)
            .Sum(t => t.ActualHours!.Value);

        var remainingEstimatedHours = tasks
            .Where(t => t.Status != TaskStatus.Done && t.EstimatedHours.HasValue)
            .Sum(t => t.EstimatedHours!.Value);

        // スケジュール統計の計算
        var completedTasksList = tasks.Where(t => t.Status == TaskStatus.Done).ToList();
        var onTimeTasks = completedTasksList
            .Count(t => t.ScheduledEndDate.HasValue 
                     && t.ActualEndDate.HasValue 
                     && t.ActualEndDate.Value <= t.ScheduledEndDate.Value);

        var delayedTasks = completedTasksList
            .Count(t => t.ScheduledEndDate.HasValue 
                     && t.ActualEndDate.HasValue 
                     && t.ActualEndDate.Value > t.ScheduledEndDate.Value);

        // 平均遅延日数の計算
        var delayedTasksWithDates = completedTasksList
            .Where(t => t.ScheduledEndDate.HasValue 
                     && t.ActualEndDate.HasValue 
                     && t.ActualEndDate.Value > t.ScheduledEndDate.Value)
            .ToList();

        var averageDelayDays = delayedTasksWithDates.Any()
            ? Math.Round(delayedTasksWithDates
                .Average(t => (t.ActualEndDate!.Value - t.ScheduledEndDate!.Value).TotalDays), 1)
            : 0;

        return new ProjectStatisticsDetailDto
        {
            TotalTasks = totalTasks,
            CompletedTasks = completedTasks,
            InProgressTasks = inProgressTasks,
            InReviewTasks = inReviewTasks,
            TodoTasks = todoTasks,
            TotalEstimatedHours = totalEstimatedHours,
            TotalActualHours = totalActualHours,
            RemainingEstimatedHours = remainingEstimatedHours,
            OnTimeTasks = onTimeTasks,
            DelayedTasks = delayedTasks,
            AverageDelayDays = averageDelayDays,
            AsOfDate = asOfDate
        };
    }
}

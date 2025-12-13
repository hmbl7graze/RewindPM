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
            .AsNoTracking()
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
        DateTimeOffset asOfDate,
        CancellationToken cancellationToken = default)
    {
        // プロジェクトの存在確認
        var projectExists = await _context.Projects
            .AnyAsync(p => p.Id == projectId, cancellationToken);
        if (!projectExists)
        {
            return null;
        }

        // 指定日時時点でのタスク状態を取得
        // まずTaskHistoriesから過去の状態を復元する
        // SQLiteはDateTimeOffsetの比較をサポートしないため、クライアント側でフィルタ
        var allTaskHistories = await _context.TaskHistories
            .AsNoTracking()
            .Where(th => th.ProjectId == projectId)
            .ToListAsync(cancellationToken);

        // 指定日時以前のスナップショットから、各タスクの最新状態を取得
        // SnapshotDateを使用して、その日の時点でのタスクの状態を取得
        var tasksFromHistory = allTaskHistories
            .Where(th => th.SnapshotDate <= asOfDate)
            .GroupBy(th => th.TaskId)
            .Select(g => g.OrderByDescending(th => th.SnapshotDate).First())
            .ToList();

        // TaskHistoriesにデータがない場合は、現在のTasksテーブルからフォールバック
        // （最新の状態を取得する場合や、TaskHistoriesがまだ作成されていない場合）
        if (tasksFromHistory.Count == 0)
        {
            var allTasks = await _context.Tasks
                .AsNoTracking()
                .Where(t => t.ProjectId == projectId)
                .ToListAsync(cancellationToken);

            // Tasksテーブルから取得した場合も、CreatedAtでフィルタ
            var tasks = allTasks.Where(t => t.CreatedAt <= asOfDate).ToList();

            // タスクが1件もなければ空の統計を返す
            if (tasks.Count == 0)
            {
                return new ProjectStatisticsDetailDto
                {
                    TotalTasks = 0,
                    CompletedTasks = 0,
                    InProgressTasks = 0,
                    InReviewTasks = 0,
                    TodoTasks = 0,
                    TotalEstimatedHours = 0,
                    TotalActualHours = 0,
                    RemainingEstimatedHours = 0,
                    OnTimeTasks = 0,
                    DelayedTasks = 0,
                    AverageDelayDays = 0,
                    AccurateEstimateTasks = 0,
                    OverEstimateTasks = 0,
                    UnderEstimateTasks = 0,
                    AverageEstimateErrorDays = 0,
                    AsOfDate = asOfDate
                };
            }

            // Tasksテーブルのデータを使って統計を計算
            var totalTasks = tasks.Count;
            var completedTasks = tasks.Count(t => t.Status == TaskStatus.Done);
            var inProgressTasks = tasks.Count(t => t.Status == TaskStatus.InProgress);
            var inReviewTasks = tasks.Count(t => t.Status == TaskStatus.InReview);
            var todoTasks = tasks.Count(t => t.Status == TaskStatus.Todo);

            var totalEstimatedHours = tasks
                .Where(t => t.EstimatedHours.HasValue)
                .Sum(t => t.EstimatedHours!.Value);

            var totalActualHours = tasks
                .Where(t => t.ActualHours.HasValue)
                .Sum(t => t.ActualHours!.Value);

            // 残予定工数 = 未完了タスクの(予定工数 - 実績工数)の合計
            var remainingEstimatedHours = tasks
                .Where(t => t.Status != TaskStatus.Done)
                .Sum(t =>
                {
                    var estimated = t.EstimatedHours ?? 0;
                    var actual = t.ActualHours ?? 0;
                    return Math.Max(0, estimated - actual); // 負の値にならないようにする
                });

            var completedTasksList = tasks.Where(t => t.Status == TaskStatus.Done).ToList();
            var onTimeTasks = completedTasksList
                .Count(t => t.ScheduledEndDate.HasValue
                         && t.ActualEndDate.HasValue
                         && t.ActualEndDate.Value <= t.ScheduledEndDate.Value);

            var delayedTasks = completedTasksList
                .Count(t => t.ScheduledEndDate.HasValue
                         && t.ActualEndDate.HasValue
                         && t.ActualEndDate.Value > t.ScheduledEndDate.Value);

            var delayedTasksWithDates = completedTasksList
                .Where(t => t.ScheduledEndDate.HasValue
                         && t.ActualEndDate.HasValue
                         && t.ActualEndDate.Value > t.ScheduledEndDate.Value)
                .ToList();

            var averageDelayDays = delayedTasksWithDates.Count > 0
                ? Math.Round(delayedTasksWithDates
                    .Average(t => (t.ActualEndDate!.Value - t.ScheduledEndDate!.Value).TotalDays), 1)
                : 0;

            // 見積もり精度の計算（作業期間ベース）
            var tasksWithDuration = completedTasksList
                .Where(t => t.ScheduledStartDate.HasValue
                         && t.ScheduledEndDate.HasValue
                         && t.ActualStartDate.HasValue
                         && t.ActualEndDate.HasValue)
                .ToList();

            var accurateEstimateTasks = tasksWithDuration
                .Count(t =>
                {
                    var plannedDuration = (t.ScheduledEndDate!.Value - t.ScheduledStartDate!.Value).TotalDays;
                    var actualDuration = (t.ActualEndDate!.Value - t.ActualStartDate!.Value).TotalDays;
                    var errorDays = Math.Abs(actualDuration - plannedDuration);
                    var errorRate = plannedDuration > 0 ? errorDays / plannedDuration : 0;
                    // 誤差が±10%以内または±1日以内なら正確とみなす
                    return errorRate <= 0.1 || errorDays <= 1;
                });

            var overEstimateTasks = tasksWithDuration
                .Count(t =>
                {
                    var plannedDuration = (t.ScheduledEndDate!.Value - t.ScheduledStartDate!.Value).TotalDays;
                    var actualDuration = (t.ActualEndDate!.Value - t.ActualStartDate!.Value).TotalDays;
                    var errorDays = Math.Abs(actualDuration - plannedDuration);
                    var errorRate = plannedDuration > 0 ? errorDays / plannedDuration : 0;
                    // 見積もりが正確でなく、実際が予定より短い
                    return errorRate > 0.1 && errorDays > 1 && actualDuration < plannedDuration;
                });

            var underEstimateTasks = tasksWithDuration
                .Count(t =>
                {
                    var plannedDuration = (t.ScheduledEndDate!.Value - t.ScheduledStartDate!.Value).TotalDays;
                    var actualDuration = (t.ActualEndDate!.Value - t.ActualStartDate!.Value).TotalDays;
                    var errorDays = Math.Abs(actualDuration - plannedDuration);
                    var errorRate = plannedDuration > 0 ? errorDays / plannedDuration : 0;
                    // 見積もりが正確でなく、実際が予定より長い
                    return errorRate > 0.1 && errorDays > 1 && actualDuration > plannedDuration;
                });

            var averageEstimateErrorDays = tasksWithDuration.Count > 0
                ? Math.Round(tasksWithDuration
                    .Average(t => (t.ActualEndDate!.Value - t.ActualStartDate!.Value).TotalDays
                               - (t.ScheduledEndDate!.Value - t.ScheduledStartDate!.Value).TotalDays), 1)
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
                AccurateEstimateTasks = accurateEstimateTasks,
                OverEstimateTasks = overEstimateTasks,
                UnderEstimateTasks = underEstimateTasks,
                AverageEstimateErrorDays = averageEstimateErrorDays,
                AsOfDate = asOfDate
            };
        }

        // TaskHistoriesのデータを使って統計を計算
        var totalTasks2 = tasksFromHistory.Count;
        var completedTasks2 = tasksFromHistory.Count(t => t.Status == TaskStatus.Done);
        var inProgressTasks2 = tasksFromHistory.Count(t => t.Status == TaskStatus.InProgress);
        var inReviewTasks2 = tasksFromHistory.Count(t => t.Status == TaskStatus.InReview);
        var todoTasks2 = tasksFromHistory.Count(t => t.Status == TaskStatus.Todo);

        // 工数統計の計算
        var totalEstimatedHours2 = tasksFromHistory
            .Where(t => t.EstimatedHours.HasValue)
            .Sum(t => t.EstimatedHours!.Value);

        var totalActualHours2 = tasksFromHistory
            .Where(t => t.ActualHours.HasValue)
            .Sum(t => t.ActualHours!.Value);

        // 残予定工数 = 未完了タスクの(予定工数 - 実績工数)の合計
        var remainingEstimatedHours2 = tasksFromHistory
            .Where(t => t.Status != TaskStatus.Done)
            .Sum(t =>
            {
                var estimated = t.EstimatedHours ?? 0;
                var actual = t.ActualHours ?? 0;
                return Math.Max(0, estimated - actual); // 負の値にならないようにする
            });

        // スケジュール統計の計算
        var completedTasksList2 = tasksFromHistory.Where(t => t.Status == TaskStatus.Done).ToList();
        var onTimeTasks2 = completedTasksList2
            .Count(t => t.ScheduledEndDate.HasValue
                     && t.ActualEndDate.HasValue
                     && t.ActualEndDate.Value <= t.ScheduledEndDate.Value);

        var delayedTasks2 = completedTasksList2
            .Count(t => t.ScheduledEndDate.HasValue
                     && t.ActualEndDate.HasValue
                     && t.ActualEndDate.Value > t.ScheduledEndDate.Value);

        // 平均遅延日数の計算
        var delayedTasksWithDates2 = completedTasksList2
            .Where(t => t.ScheduledEndDate.HasValue
                     && t.ActualEndDate.HasValue
                     && t.ActualEndDate.Value > t.ScheduledEndDate.Value)
            .ToList();

        var averageDelayDays2 = delayedTasksWithDates2.Count > 0
            ? Math.Round(delayedTasksWithDates2
                .Average(t => (t.ActualEndDate!.Value - t.ScheduledEndDate!.Value).TotalDays), 1)
            : 0;

        // 見積もり精度の計算（作業期間ベース）
        var tasksWithDuration2 = completedTasksList2
            .Where(t => t.ScheduledStartDate.HasValue
                     && t.ScheduledEndDate.HasValue
                     && t.ActualStartDate.HasValue
                     && t.ActualEndDate.HasValue)
            .ToList();

        var accurateEstimateTasks2 = tasksWithDuration2
            .Count(t =>
            {
                var plannedDuration = (t.ScheduledEndDate!.Value - t.ScheduledStartDate!.Value).TotalDays;
                var actualDuration = (t.ActualEndDate!.Value - t.ActualStartDate!.Value).TotalDays;
                var errorDays = Math.Abs(actualDuration - plannedDuration);
                var errorRate = plannedDuration > 0 ? errorDays / plannedDuration : 0;
                // 誤差が±10%以内または±1日以内なら正確とみなす
                return errorRate <= 0.1 || errorDays <= 1;
            });

        var overEstimateTasks2 = tasksWithDuration2
            .Count(t =>
            {
                var plannedDuration = (t.ScheduledEndDate!.Value - t.ScheduledStartDate!.Value).TotalDays;
                var actualDuration = (t.ActualEndDate!.Value - t.ActualStartDate!.Value).TotalDays;
                var errorDays = Math.Abs(actualDuration - plannedDuration);
                var errorRate = plannedDuration > 0 ? errorDays / plannedDuration : 0;
                // 見積もりが正確でなく、実際が予定より短い
                return errorRate > 0.1 && errorDays > 1 && actualDuration < plannedDuration;
            });

        var underEstimateTasks2 = tasksWithDuration2
            .Count(t =>
            {
                var plannedDuration = (t.ScheduledEndDate!.Value - t.ScheduledStartDate!.Value).TotalDays;
                var actualDuration = (t.ActualEndDate!.Value - t.ActualStartDate!.Value).TotalDays;
                var errorDays = Math.Abs(actualDuration - plannedDuration);
                var errorRate = plannedDuration > 0 ? errorDays / plannedDuration : 0;
                // 見積もりが正確でなく、実際が予定より長い
                return errorRate > 0.1 && errorDays > 1 && actualDuration > plannedDuration;
            });

        var averageEstimateErrorDays2 = tasksWithDuration2.Count > 0
            ? Math.Round(tasksWithDuration2
                .Average(t => (t.ActualEndDate!.Value - t.ActualStartDate!.Value).TotalDays
                           - (t.ScheduledEndDate!.Value - t.ScheduledStartDate!.Value).TotalDays), 1)
            : 0;

        return new ProjectStatisticsDetailDto
        {
            TotalTasks = totalTasks2,
            CompletedTasks = completedTasks2,
            InProgressTasks = inProgressTasks2,
            InReviewTasks = inReviewTasks2,
            TodoTasks = todoTasks2,
            TotalEstimatedHours = totalEstimatedHours2,
            TotalActualHours = totalActualHours2,
            RemainingEstimatedHours = remainingEstimatedHours2,
            OnTimeTasks = onTimeTasks2,
            DelayedTasks = delayedTasks2,
            AverageDelayDays = averageDelayDays2,
            AccurateEstimateTasks = accurateEstimateTasks2,
            OverEstimateTasks = overEstimateTasks2,
            UnderEstimateTasks = underEstimateTasks2,
            AverageEstimateErrorDays = averageEstimateErrorDays2,
            AsOfDate = asOfDate
        };
    }

    /// <summary>
    /// プロジェクト統計の時系列データを取得
    /// </summary>
    public async Task<ProjectStatisticsTimeSeriesDto?> GetProjectStatisticsTimeSeriesAsync(
        Guid projectId,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default)
    {
        // プロジェクトの存在確認
        var projectExists = await _context.Projects
            .AnyAsync(p => p.Id == projectId, cancellationToken);
        if (!projectExists)
        {
            return null;
        }

        // プロジェクトに属するタスク履歴を取得
        // 終了日以前のスナップショットに限定してパフォーマンスを改善
        // SQLiteはDateTimeOffsetのOrderByをサポートしないため、クライアント側でソート
        var allTaskHistories = await _context.TaskHistories
            .AsNoTracking()
            .Where(th => th.ProjectId == projectId)
            .ToListAsync(cancellationToken);

        var taskHistories = allTaskHistories
            .Where(th => th.SnapshotDate <= endDate)
            .OrderBy(th => th.SnapshotDate)
            .ToList();

        // 日次スナップショットを生成
        var dailySnapshots = new List<DailyStatisticsSnapshot>();
        var currentDate = startDate.Date;
        var end = endDate.Date;

        while (currentDate <= end)
        {
            var currentDateOffset = new DateTimeOffset(currentDate, startDate.Offset);

            // この日付時点でのタスク状態を計算
            var tasksAtDate = taskHistories
                .Where(th => th.SnapshotDate <= currentDateOffset)
                .GroupBy(th => th.TaskId)
                .Select(g => g.OrderByDescending(th => th.SnapshotDate).First())
                .ToList();

            var totalTasks = tasksAtDate.Count;
            var completedTasks = tasksAtDate.Count(t => t.Status == TaskStatus.Done);
            var inProgressTasks = tasksAtDate.Count(t => t.Status == TaskStatus.InProgress);
            var inReviewTasks = tasksAtDate.Count(t => t.Status == TaskStatus.InReview);
            var todoTasks = tasksAtDate.Count(t => t.Status == TaskStatus.Todo);

            dailySnapshots.Add(new DailyStatisticsSnapshot
            {
                Date = currentDateOffset,
                TotalTasks = totalTasks,
                CompletedTasks = completedTasks,
                InProgressTasks = inProgressTasks,
                InReviewTasks = inReviewTasks,
                TodoTasks = todoTasks
            });

            currentDate = currentDate.AddDays(1);
        }

        return new ProjectStatisticsTimeSeriesDto
        {
            ProjectId = projectId,
            DailySnapshots = dailySnapshots
        };
    }
}

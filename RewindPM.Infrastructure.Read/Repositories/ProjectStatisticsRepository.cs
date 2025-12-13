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

    // 見積もり精度判定の閾値定数
    /// <summary>
    /// 見積もり精度の許容誤差率（±10%）
    /// </summary>
    private const double _accuracyErrorRateThreshold = 0.1;

    /// <summary>
    /// 見積もり精度の許容誤差日数（±1日）
    /// </summary>
    private const double _accuracyErrorDaysThreshold = 1.0;

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
            .Where(t => t.ProjectId == projectId && !t.IsDeleted)
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
                .Where(t => t.ProjectId == projectId && !t.IsDeleted)
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
            var remainingEstimatedHours = CalculateRemainingEstimatedHours(tasks);

            var completedTasksList = tasks.Where(t => t.Status == TaskStatus.Done).ToList();
            var (onTimeTasks, delayedTasks, averageDelayDays) = CalculateDelayStatistics(completedTasksList);

            // 見積もり精度の計算（作業期間ベース）
            var (accurateEstimateTasks, overEstimateTasks, underEstimateTasks, averageEstimateErrorDays) = 
                CalculateEstimateAccuracy(completedTasksList);

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
        var totalTasksFromHistory = tasksFromHistory.Count;
        var completedTasksFromHistory = tasksFromHistory.Count(t => t.Status == TaskStatus.Done);
        var inProgressTasksFromHistory = tasksFromHistory.Count(t => t.Status == TaskStatus.InProgress);
        var inReviewTasksFromHistory = tasksFromHistory.Count(t => t.Status == TaskStatus.InReview);
        var todoTasksFromHistory = tasksFromHistory.Count(t => t.Status == TaskStatus.Todo);

        // 工数統計の計算
        var totalEstimatedHoursFromHistory = tasksFromHistory
            .Where(t => t.EstimatedHours.HasValue)
            .Sum(t => t.EstimatedHours!.Value);

        var totalActualHoursFromHistory = tasksFromHistory
            .Where(t => t.ActualHours.HasValue)
            .Sum(t => t.ActualHours!.Value);

        // 残予定工数 = 未完了タスクの(予定工数 - 実績工数)の合計
        var remainingEstimatedHoursFromHistory = CalculateRemainingEstimatedHours(tasksFromHistory);

        // スケジュール統計の計算
        var completedTasksListFromHistory = tasksFromHistory.Where(t => t.Status == TaskStatus.Done).ToList();
        var (onTimeTasksFromHistory, delayedTasksFromHistory, averageDelayDaysFromHistory) = 
            CalculateDelayStatistics(completedTasksListFromHistory);

        // 見積もり精度の計算（作業期間ベース）
        var (accurateEstimateTasksFromHistory, overEstimateTasksFromHistory, underEstimateTasksFromHistory, averageEstimateErrorDaysFromHistory) = 
            CalculateEstimateAccuracy(completedTasksListFromHistory);

        return new ProjectStatisticsDetailDto
        {
            TotalTasks = totalTasksFromHistory,
            CompletedTasks = completedTasksFromHistory,
            InProgressTasks = inProgressTasksFromHistory,
            InReviewTasks = inReviewTasksFromHistory,
            TodoTasks = todoTasksFromHistory,
            TotalEstimatedHours = totalEstimatedHoursFromHistory,
            TotalActualHours = totalActualHoursFromHistory,
            RemainingEstimatedHours = remainingEstimatedHoursFromHistory,
            OnTimeTasks = onTimeTasksFromHistory,
            DelayedTasks = delayedTasksFromHistory,
            AverageDelayDays = averageDelayDaysFromHistory,
            AccurateEstimateTasks = accurateEstimateTasksFromHistory,
            OverEstimateTasks = overEstimateTasksFromHistory,
            UnderEstimateTasks = underEstimateTasksFromHistory,
            AverageEstimateErrorDays = averageEstimateErrorDaysFromHistory,
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

    /// <summary>
    /// 残予定工数を計算
    /// 未完了タスクの(予定工数 - 実績工数)の合計を返す
    /// </summary>
    private static int CalculateRemainingEstimatedHours<T>(IEnumerable<T> tasks) where T : class
    {
        return tasks
            .Where(t => GetTaskStatus(t) != TaskStatus.Done)
            .Sum(t =>
            {
                var estimated = GetEstimatedHours(t) ?? 0;
                var actual = GetActualHours(t) ?? 0;
                return Math.Max(0, estimated - actual); // 負の値にならないようにする
            });
    }

    /// <summary>
    /// 遅延統計を計算
    /// </summary>
    private static (int onTimeTasks, int delayedTasks, double averageDelayDays) CalculateDelayStatistics<T>(IEnumerable<T> completedTasks) where T : class
    {
        var completedTasksList = completedTasks.ToList();
        
        var onTimeTasks = completedTasksList
            .Count(t => GetScheduledEndDate(t).HasValue
                     && GetActualEndDate(t).HasValue
                     && GetActualEndDate(t)!.Value <= GetScheduledEndDate(t)!.Value);

        var delayedTasks = completedTasksList
            .Count(t => GetScheduledEndDate(t).HasValue
                     && GetActualEndDate(t).HasValue
                     && GetActualEndDate(t)!.Value > GetScheduledEndDate(t)!.Value);

        var delayedTasksWithDates = completedTasksList
            .Where(t => GetScheduledEndDate(t).HasValue
                     && GetActualEndDate(t).HasValue
                     && GetActualEndDate(t)!.Value > GetScheduledEndDate(t)!.Value)
            .ToList();

        var averageDelayDays = delayedTasksWithDates.Count > 0
            ? Math.Round(delayedTasksWithDates
                .Average(t => (GetActualEndDate(t)!.Value - GetScheduledEndDate(t)!.Value).TotalDays), 1)
            : 0;

        return (onTimeTasks, delayedTasks, averageDelayDays);
    }

    /// <summary>
    /// 見積もり精度を計算（作業期間ベース）
    /// </summary>
    private static (int accurateTasks, int overEstimateTasks, int underEstimateTasks, double averageErrorDays) 
        CalculateEstimateAccuracy<T>(IEnumerable<T> completedTasks) where T : class
    {
        var tasksWithDuration = completedTasks
            .Where(t => GetScheduledStartDate(t).HasValue
                     && GetScheduledEndDate(t).HasValue
                     && GetActualStartDate(t).HasValue
                     && GetActualEndDate(t).HasValue)
            .ToList();

        var accurateEstimateTasks = tasksWithDuration
            .Count(t =>
            {
                var plannedDuration = (GetScheduledEndDate(t)!.Value - GetScheduledStartDate(t)!.Value).TotalDays;
                var actualDuration = (GetActualEndDate(t)!.Value - GetActualStartDate(t)!.Value).TotalDays;
                var errorDays = Math.Abs(actualDuration - plannedDuration);
                var errorRate = plannedDuration > 0 ? errorDays / plannedDuration : 0;
                // 誤差が±10%以内または±1日以内なら正確とみなす
                return errorRate <= _accuracyErrorRateThreshold || errorDays <= _accuracyErrorDaysThreshold;
            });

        var overEstimateTasks = tasksWithDuration
            .Count(t =>
            {
                var plannedDuration = (GetScheduledEndDate(t)!.Value - GetScheduledStartDate(t)!.Value).TotalDays;
                var actualDuration = (GetActualEndDate(t)!.Value - GetActualStartDate(t)!.Value).TotalDays;
                var errorDays = Math.Abs(actualDuration - plannedDuration);
                var errorRate = plannedDuration > 0 ? errorDays / plannedDuration : 0;
                // 見積もりが正確でなく、実際が予定より短い
                return errorRate > _accuracyErrorRateThreshold && errorDays > _accuracyErrorDaysThreshold && actualDuration < plannedDuration;
            });

        var underEstimateTasks = tasksWithDuration
            .Count(t =>
            {
                var plannedDuration = (GetScheduledEndDate(t)!.Value - GetScheduledStartDate(t)!.Value).TotalDays;
                var actualDuration = (GetActualEndDate(t)!.Value - GetActualStartDate(t)!.Value).TotalDays;
                var errorDays = Math.Abs(actualDuration - plannedDuration);
                var errorRate = plannedDuration > 0 ? errorDays / plannedDuration : 0;
                // 見積もりが正確でなく、実際が予定より長い
                return errorRate > _accuracyErrorRateThreshold && errorDays > _accuracyErrorDaysThreshold && actualDuration > plannedDuration;
            });

        var averageEstimateErrorDays = tasksWithDuration.Count > 0
            ? Math.Round(tasksWithDuration
                .Average(t => (GetActualEndDate(t)!.Value - GetActualStartDate(t)!.Value).TotalDays
                           - (GetScheduledEndDate(t)!.Value - GetScheduledStartDate(t)!.Value).TotalDays), 1)
            : 0;

        return (accurateEstimateTasks, overEstimateTasks, underEstimateTasks, averageEstimateErrorDays);
    }

    // ヘルパーメソッド（リフレクションを使用してプロパティにアクセス）
    private static TaskStatus GetTaskStatus<T>(T task) where T : class
    {
        var prop = typeof(T).GetProperty("Status");
        return (TaskStatus)(prop?.GetValue(task) ?? TaskStatus.Todo);
    }

    private static int? GetEstimatedHours<T>(T task) where T : class
    {
        var prop = typeof(T).GetProperty("EstimatedHours");
        return (int?)prop?.GetValue(task);
    }

    private static int? GetActualHours<T>(T task) where T : class
    {
        var prop = typeof(T).GetProperty("ActualHours");
        return (int?)prop?.GetValue(task);
    }

    private static DateTimeOffset? GetScheduledStartDate<T>(T task) where T : class
    {
        var prop = typeof(T).GetProperty("ScheduledStartDate");
        return (DateTimeOffset?)prop?.GetValue(task);
    }

    private static DateTimeOffset? GetScheduledEndDate<T>(T task) where T : class
    {
        var prop = typeof(T).GetProperty("ScheduledEndDate");
        return (DateTimeOffset?)prop?.GetValue(task);
    }

    private static DateTimeOffset? GetActualStartDate<T>(T task) where T : class
    {
        var prop = typeof(T).GetProperty("ActualStartDate");
        return (DateTimeOffset?)prop?.GetValue(task);
    }

    private static DateTimeOffset? GetActualEndDate<T>(T task) where T : class
    {
        var prop = typeof(T).GetProperty("ActualEndDate");
        return (DateTimeOffset?)prop?.GetValue(task);
    }
}

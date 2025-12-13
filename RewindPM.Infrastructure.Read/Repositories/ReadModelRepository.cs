using Microsoft.EntityFrameworkCore;
using RewindPM.Application.Read.DTOs;
using RewindPM.Application.Read.Repositories;
using RewindPM.Infrastructure.Read.Entities;
using RewindPM.Infrastructure.Read.Persistence;

namespace RewindPM.Infrastructure.Read.Repositories;

/// <summary>
/// ReadModelリポジトリの実装
/// EF CoreでReadModelデータベースにアクセスする
/// </summary>
public class ReadModelRepository : IReadModelRepository
{
    private readonly ReadModelDbContext _context;

    public ReadModelRepository(ReadModelDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 全プロジェクトを取得（削除されたものを除く）
    /// </summary>
    public async Task<List<ProjectDto>> GetAllProjectsAsync()
    {
        return await _context.Projects
            .Where(p => !p.IsDeleted)
            .Select(p => MapToProjectDto(p))
            .ToListAsync();
    }

    /// <summary>
    /// 指定されたIDのプロジェクトを取得（削除されたものを除く）
    /// </summary>
    public async Task<ProjectDto?> GetProjectByIdAsync(Guid projectId)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);

        return project == null ? null : MapToProjectDto(project);
    }

    /// <summary>
    /// 指定されたプロジェクトに属する全タスクを取得（削除されたものを除く）
    /// </summary>
    public async Task<List<TaskDto>> GetTasksByProjectIdAsync(Guid projectId)
    {
        return await _context.Tasks
            .Where(t => t.ProjectId == projectId && !t.IsDeleted)
            .Select(t => MapToTaskDto(t))
            .ToListAsync();
    }

    /// <summary>
    /// 指定されたIDのタスクを取得（削除されたものを除く）
    /// </summary>
    public async Task<TaskDto?> GetTaskByIdAsync(Guid taskId)
    {
        var task = await _context.Tasks
            .FirstOrDefaultAsync(t => t.Id == taskId && !t.IsDeleted);

        return task == null ? null : MapToTaskDto(task);
    }

    /// <summary>
    /// 指定された時点のプロジェクト状態を取得（タイムトラベル用）
    /// </summary>
    public async Task<ProjectDto?> GetProjectAtTimeAsync(Guid projectId, DateTimeOffset pointInTime)
    {
        // 指定された時点の日付（日単位）
        var targetDate = pointInTime.Date;

        // 指定された時点以前の最新のスナップショットを取得
        // SQLiteはDateTimeOffsetの比較とORDER BYをサポートしないため、クライアント側で処理
        var histories = await _context.ProjectHistories
            .Where(h => h.ProjectId == projectId)
            .ToListAsync();

        var history = histories
            .Where(h => h.SnapshotDate.Date <= targetDate)
            .OrderByDescending(h => h.SnapshotDate)
            .FirstOrDefault();

        return history == null ? null : MapToProjectDto(history);
    }

    /// <summary>
    /// 指定された時点のタスク状態を取得（タイムトラベル用）
    /// </summary>
    public async Task<TaskDto?> GetTaskAtTimeAsync(Guid taskId, DateTimeOffset pointInTime)
    {
        // 指定された時点の日付（日単位）
        var targetDate = pointInTime.Date;

        // 指定された時点以前の最新のスナップショットを取得
        // SQLiteはDateTimeOffsetの比較とORDER BYをサポートしないため、クライアント側で処理
        var histories = await _context.TaskHistories
            .Where(h => h.TaskId == taskId)
            .ToListAsync();

        var history = histories
            .Where(h => h.SnapshotDate.Date <= targetDate)
            .OrderByDescending(h => h.SnapshotDate)
            .FirstOrDefault();

        return history == null ? null : MapToTaskDto(history);
    }

    /// <summary>
    /// 指定された時点のプロジェクトに属する全タスクを取得（タイムトラベル用）
    /// </summary>
    public async Task<List<TaskDto>> GetTasksByProjectIdAtTimeAsync(Guid projectId, DateTimeOffset pointInTime)
    {
        // 指定された時点の日付（日単位）
        var targetDate = pointInTime.Date;

        // 指定された時点以前の各タスクの最新スナップショットを取得
        // SQLiteはDateTimeOffsetの比較とORDER BYをサポートしないため、クライアント側で処理
        var allHistories = await _context.TaskHistories
            .Where(h => h.ProjectId == projectId)
            .ToListAsync();

        var filteredHistories = allHistories
            .Where(h => h.SnapshotDate.Date <= targetDate)
            .ToList();

        var taskIds = filteredHistories
            .Select(h => h.TaskId)
            .Distinct()
            .ToList();

        var tasks = new List<TaskDto>();
        foreach (var taskId in taskIds)
        {
            var history = filteredHistories
                .Where(h => h.TaskId == taskId)
                .OrderByDescending(h => h.SnapshotDate)
                .FirstOrDefault();

            if (history != null)
            {
                tasks.Add(MapToTaskDto(history));
            }
        }

        return tasks;
    }

    /// <summary>
    /// 指定されたプロジェクトの編集日一覧を取得（リワインド機能用）
    /// </summary>
    public async Task<List<DateTimeOffset>> GetProjectEditDatesAsync(Guid projectId, bool ascending = false, CancellationToken cancellationToken = default)
    {
        // プロジェクトに属するタスクの履歴から、編集日（SnapshotDate）を取得
        // SQLiteはDateTimeOffsetのORDER BYをサポートしないため、クライアント側でソート
        var dates = await _context.TaskHistories
            .Where(h => h.ProjectId == projectId)
            .Select(h => h.SnapshotDate)
            .Distinct()
            .ToListAsync(cancellationToken);

        // クライアント側で並び順を指定
        return ascending
            ? dates.OrderBy(d => d).ToList()
            : dates.OrderByDescending(d => d).ToList();
    }

    /// <summary>
    /// ProjectEntityからProjectDtoへのマッピング
    /// </summary>
    private static ProjectDto MapToProjectDto(ProjectEntity entity)
    {
        return new ProjectDto
        {
            Id = entity.Id,
            Title = entity.Title,
            Description = entity.Description,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            CreatedBy = entity.CreatedBy,
            UpdatedBy = entity.UpdatedBy
        };
    }

    /// <summary>
    /// ProjectHistoryEntityからProjectDtoへのマッピング
    /// </summary>
    private static ProjectDto MapToProjectDto(ProjectHistoryEntity entity)
    {
        return new ProjectDto
        {
            Id = entity.ProjectId,
            Title = entity.Title,
            Description = entity.Description,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            CreatedBy = entity.CreatedBy,
            UpdatedBy = entity.UpdatedBy
        };
    }

    /// <summary>
    /// TaskEntityからTaskDtoへのマッピング
    /// </summary>
    private static TaskDto MapToTaskDto(TaskEntity entity)
    {
        return new TaskDto
        {
            Id = entity.Id,
            ProjectId = entity.ProjectId,
            Title = entity.Title,
            Description = entity.Description,
            Status = entity.Status,
            ScheduledStartDate = entity.ScheduledStartDate,
            ScheduledEndDate = entity.ScheduledEndDate,
            EstimatedHours = entity.EstimatedHours,
            ActualStartDate = entity.ActualStartDate,
            ActualEndDate = entity.ActualEndDate,
            ActualHours = entity.ActualHours,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            CreatedBy = entity.CreatedBy,
            UpdatedBy = entity.UpdatedBy
        };
    }

    /// <summary>
    /// TaskHistoryEntityからTaskDtoへのマッピング
    /// </summary>
    private static TaskDto MapToTaskDto(TaskHistoryEntity entity)
    {
        return new TaskDto
        {
            Id = entity.TaskId,
            ProjectId = entity.ProjectId,
            Title = entity.Title,
            Description = entity.Description,
            Status = entity.Status,
            ScheduledStartDate = entity.ScheduledStartDate,
            ScheduledEndDate = entity.ScheduledEndDate,
            EstimatedHours = entity.EstimatedHours,
            ActualStartDate = entity.ActualStartDate,
            ActualEndDate = entity.ActualEndDate,
            ActualHours = entity.ActualHours,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            CreatedBy = entity.CreatedBy,
            UpdatedBy = entity.UpdatedBy
        };
    }
}

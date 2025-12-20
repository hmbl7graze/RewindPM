using Microsoft.Extensions.Logging;
using RewindPM.Infrastructure.Read.Entities;
using RewindPM.Infrastructure.Read.Services;

namespace RewindPM.Projection.Services;

/// <summary>
/// タスクのスナップショット作成・更新を担当するサービス
/// 複数のEvent Handlerで共通利用される
/// </summary>
public class TaskSnapshotService
{
    private readonly IReadModelContext _context;
    private readonly ITimeZoneService _timeZoneService;
    private readonly ILogger<TaskSnapshotService> _logger;

    public TaskSnapshotService(
        IReadModelContext context,
        ITimeZoneService timeZoneService,
        ILogger<TaskSnapshotService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _timeZoneService = timeZoneService ?? throw new ArgumentNullException(nameof(timeZoneService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// タスクのスナップショットを作成または更新する準備を行う
    /// 同じ日に複数回更新された場合、その日の最後の状態を保持する
    /// </summary>
    /// <remarks>
    /// このメソッドは変更をDbContextに追加するのみで、SaveChangesAsyncは呼び出し側で実行すること
    /// </remarks>
    /// <param name="taskId">タスクID</param>
    /// <param name="currentState">タスクの現在状態</param>
    /// <param name="occurredAt">イベント発生日時</param>
    public async Task PrepareTaskSnapshotAsync(Guid taskId, TaskEntity currentState, DateTimeOffset occurredAt)
    {
        ArgumentNullException.ThrowIfNull(currentState);

        var snapshotDate = _timeZoneService.GetSnapshotDate(occurredAt);
        var snapshot = _context.TaskHistories
            .FirstOrDefault(h => h.TaskId == taskId && h.SnapshotDate == snapshotDate);

        if (snapshot != null)
        {
            // 既存のスナップショットを更新
            UpdateSnapshot(snapshot, currentState, occurredAt);

            _logger.LogDebug("Updated existing snapshot for task {TaskId} on {SnapshotDate}",
                taskId, snapshotDate);
        }
        else
        {
            // 新規スナップショットを作成
            snapshot = CreateSnapshot(taskId, currentState, snapshotDate, occurredAt);
            _context.AddTaskHistory(snapshot);

            _logger.LogDebug("Created new snapshot for task {TaskId} on {SnapshotDate}",
                taskId, snapshotDate);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 既存のスナップショットを更新する
    /// </summary>
    private void UpdateSnapshot(TaskHistoryEntity snapshot, TaskEntity currentState, DateTimeOffset occurredAt)
    {
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
    }

    /// <summary>
    /// 新規スナップショットを作成する
    /// </summary>
    private TaskHistoryEntity CreateSnapshot(
        Guid taskId,
        TaskEntity currentState,
        DateTimeOffset snapshotDate,
        DateTimeOffset occurredAt)
    {
        return new TaskHistoryEntity
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
    }
}

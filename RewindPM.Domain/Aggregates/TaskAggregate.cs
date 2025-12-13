using RewindPM.Domain.Common;
using RewindPM.Domain.Events;
using RewindPM.Domain.ValueObjects;
using TaskStatus = RewindPM.Domain.ValueObjects.TaskStatus;

namespace RewindPM.Domain.Aggregates;

/// <summary>
/// タスクのAggregate
/// タスクの全ての情報と操作を管理する
/// </summary>
public class TaskAggregate : AggregateRoot
{
    /// <summary>
    /// 所属するプロジェクトのID
    /// </summary>
    public Guid ProjectId { get; private set; }

    /// <summary>
    /// タスクのタイトル
    /// </summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>
    /// タスクの説明
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// タスクのステータス
    /// </summary>
    public TaskStatus Status { get; private set; }

    /// <summary>
    /// 予定期間と工数
    /// </summary>
    public ScheduledPeriod ScheduledPeriod { get; private set; } = null!;

    /// <summary>
    /// 実績期間と工数
    /// </summary>
    public ActualPeriod ActualPeriod { get; private set; } = new ActualPeriod();

    /// <summary>
    /// タスクを作成したユーザーID
    /// </summary>
    public string CreatedBy { get; private set; } = string.Empty;

    /// <summary>
    /// タスクを最後に更新したユーザーID
    /// </summary>
    public string UpdatedBy { get; private set; } = string.Empty;

    /// <summary>
    /// デフォルトコンストラクタ（イベントリプレイ用）
    /// テストからアクセス可能にするためinternalとする
    /// </summary>
    internal TaskAggregate()
    {
    }

    /// <summary>
    /// 新しいタスクを作成する
    /// </summary>
    /// <param name="id">タスクID</param>
    /// <param name="projectId">所属するプロジェクトのID</param>
    /// <param name="title">タスクのタイトル</param>
    /// <param name="description">タスクの説明</param>
    /// <param name="scheduledPeriod">予定期間と工数</param>
    /// <param name="createdBy">作成者のユーザーID</param>
    /// <param name="dateTimeProvider">時刻プロバイダー</param>
    /// <returns>新しいTaskAggregateインスタンス</returns>
    public static TaskAggregate Create(
        Guid id,
        Guid projectId,
        string title,
        string description,
        ScheduledPeriod scheduledPeriod,
        string createdBy,
        IDateTimeProvider dateTimeProvider)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("タスクのタイトルは必須です");
        }

        if (projectId == Guid.Empty)
        {
            throw new DomainException("プロジェクトIDは必須です");
        }

        if (scheduledPeriod == null)
        {
            throw new DomainException("予定期間は必須です");
        }

        if (string.IsNullOrWhiteSpace(createdBy))
        {
            throw new DomainException("作成者のユーザーIDは必須です");
        }

        var task = new TaskAggregate();
        task.ApplyEvent(new TaskCreated
        {
            AggregateId = id,
            OccurredAt = dateTimeProvider.UtcNow,
            ProjectId = projectId,
            Title = title,
            Description = description ?? string.Empty,
            ScheduledPeriod = scheduledPeriod,
            CreatedBy = createdBy
        });

        return task;
    }

    /// <summary>
    /// タスクのステータスを変更する
    /// </summary>
    /// <param name="newStatus">新しいステータス</param>
    /// <param name="changedBy">変更者のユーザーID</param>
    /// <param name="dateTimeProvider">時刻プロバイダー</param>
    public void ChangeStatus(TaskStatus newStatus, string changedBy, IDateTimeProvider dateTimeProvider)
    {
        if (Status == newStatus)
        {
            return; // 同じステータスへの変更は無視
        }

        if (string.IsNullOrWhiteSpace(changedBy))
        {
            throw new DomainException("変更者のユーザーIDは必須です");
        }

        ApplyEvent(new TaskStatusChanged
        {
            AggregateId = Id,
            OccurredAt = dateTimeProvider.UtcNow,
            OldStatus = Status,
            NewStatus = newStatus,
            ChangedBy = changedBy
        });
    }

    /// <summary>
    /// タスクのタイトルまたは説明を更新する
    /// </summary>
    /// <param name="title">新しいタイトル</param>
    /// <param name="description">新しい説明</param>
    /// <param name="updatedBy">更新者のユーザーID</param>
    /// <param name="dateTimeProvider">時刻プロバイダー</param>
    public void Update(string title, string description, string updatedBy, IDateTimeProvider dateTimeProvider)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("タスクのタイトルは必須です");
        }

        if (string.IsNullOrWhiteSpace(updatedBy))
        {
            throw new DomainException("更新者のユーザーIDは必須です");
        }

        ApplyEvent(new TaskUpdated
        {
            AggregateId = Id,
            OccurredAt = dateTimeProvider.UtcNow,
            Title = title,
            Description = description ?? string.Empty,
            UpdatedBy = updatedBy
        });
    }

    /// <summary>
    /// タスクの予定期間を変更する
    /// </summary>
    /// <param name="scheduledPeriod">新しい予定期間</param>
    /// <param name="changedBy">変更者のユーザーID</param>
    /// <param name="dateTimeProvider">時刻プロバイダー</param>
    public void ChangeSchedule(ScheduledPeriod scheduledPeriod, string changedBy, IDateTimeProvider dateTimeProvider)
    {
        if (scheduledPeriod == null)
        {
            throw new DomainException("予定期間は必須です");
        }

        if (string.IsNullOrWhiteSpace(changedBy))
        {
            throw new DomainException("変更者のユーザーIDは必須です");
        }

        ApplyEvent(new TaskScheduledPeriodChanged
        {
            AggregateId = Id,
            OccurredAt = dateTimeProvider.UtcNow,
            ScheduledPeriod = scheduledPeriod,
            ChangedBy = changedBy
        });
    }

    /// <summary>
    /// タスクの実績期間を変更する
    /// </summary>
    /// <param name="actualPeriod">新しい実績期間</param>
    /// <param name="changedBy">変更者のユーザーID</param>
    /// <param name="dateTimeProvider">時刻プロバイダー</param>
    public void ChangeActualPeriod(ActualPeriod actualPeriod, string changedBy, IDateTimeProvider dateTimeProvider)
    {
        if (actualPeriod == null)
        {
            throw new DomainException("実績期間は必須です");
        }

        if (string.IsNullOrWhiteSpace(changedBy))
        {
            throw new DomainException("変更者のユーザーIDは必須です");
        }

        ApplyEvent(new TaskActualPeriodChanged
        {
            AggregateId = Id,
            OccurredAt = dateTimeProvider.UtcNow,
            ActualPeriod = actualPeriod,
            ChangedBy = changedBy
        });
    }

    /// <summary>
    /// タスクを削除する（論理削除）
    /// </summary>
    /// <param name="deletedBy">削除者のユーザーID</param>
    /// <param name="dateTimeProvider">時刻プロバイダー</param>
    public void Delete(string deletedBy, IDateTimeProvider dateTimeProvider)
    {
        if (string.IsNullOrWhiteSpace(deletedBy))
        {
            throw new DomainException("削除者のユーザーIDは必須です");
        }

        ApplyEvent(new TaskDeleted
        {
            AggregateId = Id,
            OccurredAt = dateTimeProvider.UtcNow,
            ProjectId = ProjectId,
            DeletedBy = deletedBy,
            Reason = string.Empty
        });
    }

    /// <summary>
    /// イベントに応じてAggregateの状態を変更する
    /// </summary>
    /// <param name="event">適用するドメインイベント</param>
    protected override void When(IDomainEvent @event)
    {
        switch (@event)
        {
            case TaskCreated e:
                Id = e.AggregateId;
                ProjectId = e.ProjectId;
                Title = e.Title;
                Description = e.Description;
                Status = TaskStatus.Todo;
                ScheduledPeriod = e.ScheduledPeriod;
                ActualPeriod = new ActualPeriod();
                CreatedBy = e.CreatedBy;
                UpdatedBy = e.CreatedBy;
                break;

            case TaskStatusChanged e:
                Status = e.NewStatus;
                UpdatedBy = e.ChangedBy;
                break;

            case TaskUpdated e:
                Title = e.Title;
                Description = e.Description;
                UpdatedBy = e.UpdatedBy;
                break;

            case TaskScheduledPeriodChanged e:
                ScheduledPeriod = e.ScheduledPeriod;
                UpdatedBy = e.ChangedBy;
                break;

            case TaskActualPeriodChanged e:
                ActualPeriod = e.ActualPeriod;
                UpdatedBy = e.ChangedBy;
                break;

            case TaskDeleted e:
                // 論理削除のため、Aggregate自体の状態は変更しない
                // イベントストアに記録するのみ
                break;
        }
    }
}

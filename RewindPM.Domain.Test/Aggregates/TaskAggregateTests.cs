using RewindPM.Domain.Aggregates;
using RewindPM.Domain.Common;
using RewindPM.Domain.Events;
using RewindPM.Domain.ValueObjects;
using RewindPM.Domain.Test.TestHelpers;
using TaskStatus = RewindPM.Domain.ValueObjects.TaskStatus;

namespace RewindPM.Domain.Test.Aggregates;

public class TaskAggregateTests
{
    private readonly IDateTimeProvider _dateTimeProvider = new TestDateTimeProvider();
    private readonly Guid _projectId = Guid.NewGuid();
    private readonly ScheduledPeriod _scheduledPeriod = new(
        new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2025, 1, 10, 0, 0, 0, TimeSpan.Zero),
        40);

    [Fact(DisplayName = "有効な値でタスクを作成できる")]
    public void Create_WithValidValues_ShouldCreateTask()
    {
        // Arrange
        var id = Guid.NewGuid();
        var title = "新しいタスク";
        var description = "タスクの説明";
        var createdBy = "user123";

        // Act
        var task = TaskAggregate.Create(id, _projectId, title, description, _scheduledPeriod, createdBy, _dateTimeProvider);

        // Assert
        Assert.Equal(id, task.Id);
        Assert.Equal(_projectId, task.ProjectId);
        Assert.Equal(title, task.Title);
        Assert.Equal(description, task.Description);
        Assert.Equal(TaskStatus.Todo, task.Status);
        Assert.Equal(_scheduledPeriod, task.ScheduledPeriod);
        Assert.NotNull(task.ActualPeriod);
        Assert.False(task.ActualPeriod.IsStarted);
        Assert.Equal(createdBy, task.CreatedBy);
        Assert.Equal(createdBy, task.UpdatedBy);
    }

    [Fact(DisplayName = "タスク作成時にTaskCreatedイベントが発生する")]
    public void Create_ShouldRaiseTaskCreatedEvent()
    {
        // Arrange
        var id = Guid.NewGuid();
        var title = "新しいタスク";
        var description = "タスクの説明";
        var createdBy = "user123";

        // Act
        var task = TaskAggregate.Create(id, _projectId, title, description, _scheduledPeriod, createdBy, _dateTimeProvider);

        // Assert
        Assert.Single(task.UncommittedEvents);
        var @event = task.UncommittedEvents.First();
        Assert.IsType<TaskCreated>(@event);

        var taskCreatedEvent = (TaskCreated)@event;
        Assert.Equal(id, taskCreatedEvent.AggregateId);
        Assert.Equal(_projectId, taskCreatedEvent.ProjectId);
        Assert.Equal(title, taskCreatedEvent.Title);
        Assert.Equal(description, taskCreatedEvent.Description);
        Assert.Equal(_scheduledPeriod, taskCreatedEvent.ScheduledPeriod);
        Assert.Equal(createdBy, taskCreatedEvent.CreatedBy);
    }

    [Fact(DisplayName = "タイトルがnullの場合、DomainExceptionをスローする")]
    public void Create_WhenTitleIsNull_ShouldThrowDomainException()
    {
        // Arrange
        var id = Guid.NewGuid();
        string title = null!;
        var description = "タスクの説明";
        var createdBy = "user123";

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() =>
            TaskAggregate.Create(id, _projectId, title, description, _scheduledPeriod, createdBy, _dateTimeProvider));
        Assert.Equal("タスクのタイトルは必須です", exception.Message);
    }

    [Fact(DisplayName = "プロジェクトIDが空の場合、DomainExceptionをスローする")]
    public void Create_WhenProjectIdIsEmpty_ShouldThrowDomainException()
    {
        // Arrange
        var id = Guid.NewGuid();
        var projectId = Guid.Empty;
        var title = "新しいタスク";
        var description = "タスクの説明";
        var createdBy = "user123";

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() =>
            TaskAggregate.Create(id, projectId, title, description, _scheduledPeriod, createdBy, _dateTimeProvider));
        Assert.Equal("プロジェクトIDは必須です", exception.Message);
    }

    [Fact(DisplayName = "予定期間がnullの場合、DomainExceptionをスローする")]
    public void Create_WhenScheduledPeriodIsNull_ShouldThrowDomainException()
    {
        // Arrange
        var id = Guid.NewGuid();
        var title = "新しいタスク";
        var description = "タスクの説明";
        ScheduledPeriod scheduledPeriod = null!;
        var createdBy = "user123";

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() =>
            TaskAggregate.Create(id, _projectId, title, description, scheduledPeriod, createdBy, _dateTimeProvider));
        Assert.Equal("予定期間は必須です", exception.Message);
    }

    [Fact(DisplayName = "作成者がnullの場合、DomainExceptionをスローする")]
    public void Create_WhenCreatedByIsNull_ShouldThrowDomainException()
    {
        // Arrange
        var id = Guid.NewGuid();
        var title = "新しいタスク";
        var description = "タスクの説明";
        string createdBy = null!;

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() =>
            TaskAggregate.Create(id, _projectId, title, description, _scheduledPeriod, createdBy, _dateTimeProvider));
        Assert.Equal("作成者のユーザーIDは必須です", exception.Message);
    }

    [Fact(DisplayName = "タスクのステータスを変更できる")]
    public void ChangeStatus_WithValidValues_ShouldChangeStatus()
    {
        // Arrange
        var task = TaskAggregate.Create(Guid.NewGuid(), _projectId, "タスク", "説明", _scheduledPeriod, "user123", _dateTimeProvider);
        task.ClearUncommittedEvents();

        var newStatus = TaskStatus.InProgress;
        var changedBy = "user456";

        // Act
        task.ChangeStatus(newStatus, changedBy, _dateTimeProvider);

        // Assert
        Assert.Equal(newStatus, task.Status);
        Assert.Equal(changedBy, task.UpdatedBy);
    }

    [Fact(DisplayName = "ステータス変更時にTaskStatusChangedイベントが発生する")]
    public void ChangeStatus_ShouldRaiseTaskStatusChangedEvent()
    {
        // Arrange
        var task = TaskAggregate.Create(Guid.NewGuid(), _projectId, "タスク", "説明", _scheduledPeriod, "user123", _dateTimeProvider);
        task.ClearUncommittedEvents();

        var oldStatus = TaskStatus.Todo;
        var newStatus = TaskStatus.InProgress;
        var changedBy = "user456";

        // Act
        task.ChangeStatus(newStatus, changedBy, _dateTimeProvider);

        // Assert
        Assert.Single(task.UncommittedEvents);
        var @event = task.UncommittedEvents.First();
        Assert.IsType<TaskStatusChanged>(@event);

        var statusChangedEvent = (TaskStatusChanged)@event;
        Assert.Equal(oldStatus, statusChangedEvent.OldStatus);
        Assert.Equal(newStatus, statusChangedEvent.NewStatus);
        Assert.Equal(changedBy, statusChangedEvent.ChangedBy);
    }

    [Fact(DisplayName = "同じステータスへの変更は無視される")]
    public void ChangeStatus_WhenSameStatus_ShouldBeIgnored()
    {
        // Arrange
        var task = TaskAggregate.Create(Guid.NewGuid(), _projectId, "タスク", "説明", _scheduledPeriod, "user123", _dateTimeProvider);
        task.ClearUncommittedEvents();

        // Act
        task.ChangeStatus(TaskStatus.Todo, "user456", _dateTimeProvider);

        // Assert
        Assert.Empty(task.UncommittedEvents);
    }

    [Fact(DisplayName = "タスクのタイトルと説明を更新できる")]
    public void Update_WithValidValues_ShouldUpdateTask()
    {
        // Arrange
        var task = TaskAggregate.Create(Guid.NewGuid(), _projectId, "旧タイトル", "旧説明", _scheduledPeriod, "user123", _dateTimeProvider);
        task.ClearUncommittedEvents();

        var newTitle = "新タイトル";
        var newDescription = "新説明";
        var updatedBy = "user456";

        // Act
        task.Update(newTitle, newDescription, updatedBy, _dateTimeProvider);

        // Assert
        Assert.Equal(newTitle, task.Title);
        Assert.Equal(newDescription, task.Description);
        Assert.Equal(updatedBy, task.UpdatedBy);
    }

    [Fact(DisplayName = "タスク更新時にTaskUpdatedイベントが発生する")]
    public void Update_ShouldRaiseTaskUpdatedEvent()
    {
        // Arrange
        var task = TaskAggregate.Create(Guid.NewGuid(), _projectId, "旧タイトル", "旧説明", _scheduledPeriod, "user123", _dateTimeProvider);
        task.ClearUncommittedEvents();

        var newTitle = "新タイトル";
        var newDescription = "新説明";
        var updatedBy = "user456";

        // Act
        task.Update(newTitle, newDescription, updatedBy, _dateTimeProvider);

        // Assert
        Assert.Single(task.UncommittedEvents);
        var @event = task.UncommittedEvents.First();
        Assert.IsType<TaskUpdated>(@event);

        var taskUpdatedEvent = (TaskUpdated)@event;
        Assert.Equal(newTitle, taskUpdatedEvent.Title);
        Assert.Equal(newDescription, taskUpdatedEvent.Description);
        Assert.Equal(updatedBy, taskUpdatedEvent.UpdatedBy);
    }

    [Fact(DisplayName = "タスクの予定期間を変更できる")]
    public void ChangeSchedule_WithValidValues_ShouldChangeSchedule()
    {
        // Arrange
        var task = TaskAggregate.Create(Guid.NewGuid(), _projectId, "タスク", "説明", _scheduledPeriod, "user123", _dateTimeProvider);
        task.ClearUncommittedEvents();

        var newSchedule = new ScheduledPeriod(
            new DateTimeOffset(2025, 2, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2025, 2, 15, 0, 0, 0, TimeSpan.Zero),
            60);
        var changedBy = "user456";

        // Act
        task.ChangeSchedule(newSchedule, changedBy, _dateTimeProvider);

        // Assert
        Assert.Equal(newSchedule, task.ScheduledPeriod);
        Assert.Equal(changedBy, task.UpdatedBy);
    }

    [Fact(DisplayName = "予定期間変更時にTaskScheduledPeriodChangedイベントが発生する")]
    public void ChangeSchedule_ShouldRaiseTaskScheduledPeriodChangedEvent()
    {
        // Arrange
        var task = TaskAggregate.Create(Guid.NewGuid(), _projectId, "タスク", "説明", _scheduledPeriod, "user123", _dateTimeProvider);
        task.ClearUncommittedEvents();

        var newSchedule = new ScheduledPeriod(
            new DateTimeOffset(2025, 2, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2025, 2, 15, 0, 0, 0, TimeSpan.Zero),
            60);
        var changedBy = "user456";

        // Act
        task.ChangeSchedule(newSchedule, changedBy, _dateTimeProvider);

        // Assert
        Assert.Single(task.UncommittedEvents);
        var @event = task.UncommittedEvents.First();
        Assert.IsType<TaskScheduledPeriodChanged>(@event);

        var scheduleChangedEvent = (TaskScheduledPeriodChanged)@event;
        Assert.Equal(newSchedule, scheduleChangedEvent.ScheduledPeriod);
        Assert.Equal(changedBy, scheduleChangedEvent.ChangedBy);
    }

    [Fact(DisplayName = "タスクの実績期間を変更できる")]
    public void ChangeActualPeriod_WithValidValues_ShouldChangeActualPeriod()
    {
        // Arrange
        var task = TaskAggregate.Create(Guid.NewGuid(), _projectId, "タスク", "説明", _scheduledPeriod, "user123", _dateTimeProvider);
        task.ClearUncommittedEvents();

        var actualPeriod = new ActualPeriod(
            new DateTimeOffset(2025, 1, 2, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2025, 1, 8, 0, 0, 0, TimeSpan.Zero),
            35);
        var changedBy = "user456";

        // Act
        task.ChangeActualPeriod(actualPeriod, changedBy, _dateTimeProvider);

        // Assert
        Assert.Equal(actualPeriod, task.ActualPeriod);
        Assert.Equal(changedBy, task.UpdatedBy);
    }

    [Fact(DisplayName = "実績期間変更時にTaskActualPeriodChangedイベントが発生する")]
    public void ChangeActualPeriod_ShouldRaiseTaskActualPeriodChangedEvent()
    {
        // Arrange
        var task = TaskAggregate.Create(Guid.NewGuid(), _projectId, "タスク", "説明", _scheduledPeriod, "user123", _dateTimeProvider);
        task.ClearUncommittedEvents();

        var actualPeriod = new ActualPeriod(
            new DateTimeOffset(2025, 1, 2, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2025, 1, 8, 0, 0, 0, TimeSpan.Zero),
            35);
        var changedBy = "user456";

        // Act
        task.ChangeActualPeriod(actualPeriod, changedBy, _dateTimeProvider);

        // Assert
        Assert.Single(task.UncommittedEvents);
        var @event = task.UncommittedEvents.First();
        Assert.IsType<TaskActualPeriodChanged>(@event);

        var actualPeriodChangedEvent = (TaskActualPeriodChanged)@event;
        Assert.Equal(actualPeriod, actualPeriodChangedEvent.ActualPeriod);
        Assert.Equal(changedBy, actualPeriodChangedEvent.ChangedBy);
    }

    [Fact(DisplayName = "イベントリプレイでタスクの状態を復元できる")]
    public void ReplayEvents_ShouldRestoreTaskState()
    {
        // Arrange
        var id = Guid.NewGuid();
        var events = new List<IDomainEvent>
        {
            new TaskCreated
            {
                AggregateId = id,
                ProjectId = _projectId,
                Title = "元のタイトル",
                Description = "元の説明",
                ScheduledPeriod = _scheduledPeriod,
                CreatedBy = "user123"
            },
            new TaskStatusChanged
            {
                AggregateId = id,
                OldStatus = TaskStatus.Todo,
                NewStatus = TaskStatus.InProgress,
                ChangedBy = "user456"
            },
            new TaskUpdated
            {
                AggregateId = id,
                Title = "更新されたタイトル",
                Description = "更新された説明",
                UpdatedBy = "user789"
            }
        };

        // Act
        var task = new TaskAggregate();
        task.ReplayEvents(events);

        // Assert
        Assert.Equal(id, task.Id);
        Assert.Equal(_projectId, task.ProjectId);
        Assert.Equal("更新されたタイトル", task.Title);
        Assert.Equal("更新された説明", task.Description);
        Assert.Equal(TaskStatus.InProgress, task.Status);
        Assert.Equal(_scheduledPeriod, task.ScheduledPeriod);
        Assert.Equal("user123", task.CreatedBy);
        Assert.Equal("user789", task.UpdatedBy);
        Assert.Equal(2, task.Version); // 3つのイベント = Version 2 (0-indexed)
        Assert.Empty(task.UncommittedEvents);
    }

    [Fact(DisplayName = "タスクを削除できる")]
    public void Delete_WithValidValues_ShouldDeleteTask()
    {
        // Arrange
        var task = TaskAggregate.Create(Guid.NewGuid(), _projectId, "タスク", "説明", _scheduledPeriod, "user123", _dateTimeProvider);
        task.ClearUncommittedEvents();

        var deletedBy = "user456";

        // Act
        task.Delete(deletedBy, _dateTimeProvider);

        // Assert
        // Aggregateの状態は変わらない（論理削除はProjectionで処理）
        Assert.Equal("タスク", task.Title);
        Assert.Single(task.UncommittedEvents);
    }

    [Fact(DisplayName = "タスク削除時にTaskDeletedイベントが発生する")]
    public void Delete_ShouldRaiseTaskDeletedEvent()
    {
        // Arrange
        var task = TaskAggregate.Create(Guid.NewGuid(), _projectId, "タスク", "説明", _scheduledPeriod, "user123", _dateTimeProvider);
        task.ClearUncommittedEvents();

        var deletedBy = "user456";

        // Act
        task.Delete(deletedBy, _dateTimeProvider);

        // Assert
        Assert.Single(task.UncommittedEvents);
        var @event = task.UncommittedEvents.First();
        Assert.IsType<TaskDeleted>(@event);

        var taskDeletedEvent = (TaskDeleted)@event;
        Assert.Equal(task.Id, taskDeletedEvent.AggregateId);
        Assert.Equal(_projectId, taskDeletedEvent.ProjectId);
        Assert.Equal(deletedBy, taskDeletedEvent.DeletedBy);
    }

    [Fact(DisplayName = "削除者がnullの場合、DomainExceptionをスローする")]
    public void Delete_WhenDeletedByIsNull_ShouldThrowDomainException()
    {
        // Arrange
        var task = TaskAggregate.Create(Guid.NewGuid(), _projectId, "タスク", "説明", _scheduledPeriod, "user123", _dateTimeProvider);
        string deletedBy = null!;

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() =>
            task.Delete(deletedBy, _dateTimeProvider));
        Assert.Equal("削除者のユーザーIDは必須です", exception.Message);
    }

    [Fact(DisplayName = "削除者が空文字列の場合、DomainExceptionをスローする")]
    public void Delete_WhenDeletedByIsEmpty_ShouldThrowDomainException()
    {
        // Arrange
        var task = TaskAggregate.Create(Guid.NewGuid(), _projectId, "タスク", "説明", _scheduledPeriod, "user123", _dateTimeProvider);
        var deletedBy = "";

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() =>
            task.Delete(deletedBy, _dateTimeProvider));
        Assert.Equal("削除者のユーザーIDは必須です", exception.Message);
    }

    [Fact(DisplayName = "UpdateCompletelyで全てのプロパティを一括更新できる")]
    public void UpdateCompletely_WithAllChanges_ShouldUpdateAllProperties()
    {
        // Arrange
        var task = TaskAggregate.Create(Guid.NewGuid(), _projectId, "旧タイトル", "旧説明", _scheduledPeriod, "user123", _dateTimeProvider);
        task.ClearUncommittedEvents();

        var newTitle = "新タイトル";
        var newDescription = "新説明";
        var newStatus = TaskStatus.InProgress;
        var newScheduledPeriod = new ScheduledPeriod(
            new DateTimeOffset(2025, 2, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2025, 2, 15, 0, 0, 0, TimeSpan.Zero),
            60);
        var newActualPeriod = new ActualPeriod(
            new DateTimeOffset(2025, 2, 2, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2025, 2, 10, 0, 0, 0, TimeSpan.Zero),
            40);
        var updatedBy = "user456";

        // Act
        task.UpdateCompletely(newTitle, newDescription, newStatus, newScheduledPeriod, newActualPeriod, updatedBy, _dateTimeProvider);

        // Assert
        Assert.Equal(newTitle, task.Title);
        Assert.Equal(newDescription, task.Description);
        Assert.Equal(newStatus, task.Status);
        Assert.Equal(newScheduledPeriod, task.ScheduledPeriod);
        Assert.Equal(newActualPeriod, task.ActualPeriod);
        Assert.Equal(updatedBy, task.UpdatedBy);
    }

    [Fact(DisplayName = "UpdateCompletelyでTaskCompletelyUpdatedイベントが発生する")]
    public void UpdateCompletely_WithAllChanges_ShouldRaiseTaskCompletelyUpdatedEvent()
    {
        // Arrange
        var task = TaskAggregate.Create(Guid.NewGuid(), _projectId, "旧タイトル", "旧説明", _scheduledPeriod, "user123", _dateTimeProvider);
        task.ClearUncommittedEvents();

        var newTitle = "新タイトル";
        var newDescription = "新説明";
        var newStatus = TaskStatus.InProgress;
        var newScheduledPeriod = new ScheduledPeriod(
            new DateTimeOffset(2025, 2, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2025, 2, 15, 0, 0, 0, TimeSpan.Zero),
            60);
        var newActualPeriod = new ActualPeriod(
            new DateTimeOffset(2025, 2, 2, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2025, 2, 10, 0, 0, 0, TimeSpan.Zero),
            40);
        var updatedBy = "user456";

        // Act
        task.UpdateCompletely(newTitle, newDescription, newStatus, newScheduledPeriod, newActualPeriod, updatedBy, _dateTimeProvider);

        // Assert - 単一のTaskCompletelyUpdatedイベントが発生する
        Assert.Single(task.UncommittedEvents);
        var @event = Assert.IsType<TaskCompletelyUpdated>(task.UncommittedEvents.First());
        Assert.Equal(newTitle, @event.Title);
        Assert.Equal(newDescription, @event.Description);
        Assert.Equal(newStatus, @event.Status);
        Assert.Equal(newScheduledPeriod, @event.ScheduledPeriod);
        Assert.Equal(newActualPeriod, @event.ActualPeriod);
        Assert.Equal(updatedBy, @event.UpdatedBy);
    }

    [Fact(DisplayName = "UpdateCompletelyで変更がなくてもTaskCompletelyUpdatedイベントが発生する")]
    public void UpdateCompletely_WithNoChanges_ShouldStillRaiseEvent()
    {
        // Arrange
        var task = TaskAggregate.Create(Guid.NewGuid(), _projectId, "タイトル", "説明", _scheduledPeriod, "user123", _dateTimeProvider);
        task.ClearUncommittedEvents();

        var actualPeriod = new ActualPeriod();
        var updatedBy = "user456";

        // Act - 同じ値で更新
        task.UpdateCompletely("タイトル", "説明", TaskStatus.Todo, _scheduledPeriod, actualPeriod, updatedBy, _dateTimeProvider);

        // Assert - TaskCompletelyUpdatedイベントが発生する
        Assert.Single(task.UncommittedEvents);
        Assert.IsType<TaskCompletelyUpdated>(task.UncommittedEvents.First());
    }

    [Fact(DisplayName = "UpdateCompletelyでタイトルのみ変更した場合でもTaskCompletelyUpdatedイベントが発生する")]
    public void UpdateCompletely_WithOnlyTitleChange_ShouldRaiseTaskCompletelyUpdatedEvent()
    {
        // Arrange
        var task = TaskAggregate.Create(Guid.NewGuid(), _projectId, "旧タイトル", "説明", _scheduledPeriod, "user123", _dateTimeProvider);
        task.ClearUncommittedEvents();

        var newTitle = "新タイトル";
        var actualPeriod = new ActualPeriod();
        var updatedBy = "user456";

        // Act
        task.UpdateCompletely(newTitle, "説明", TaskStatus.Todo, _scheduledPeriod, actualPeriod, updatedBy, _dateTimeProvider);

        // Assert
        Assert.Single(task.UncommittedEvents);
        var @event = Assert.IsType<TaskCompletelyUpdated>(task.UncommittedEvents.First());
        Assert.Equal(newTitle, @event.Title);
    }

    [Fact(DisplayName = "UpdateCompletelyでタイトルがnullの場合、DomainExceptionをスローする")]
    public void UpdateCompletely_WhenTitleIsNull_ShouldThrowDomainException()
    {
        // Arrange
        var task = TaskAggregate.Create(Guid.NewGuid(), _projectId, "タスク", "説明", _scheduledPeriod, "user123", _dateTimeProvider);
        var actualPeriod = new ActualPeriod();

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() =>
            task.UpdateCompletely(null!, "新説明", TaskStatus.Todo, _scheduledPeriod, actualPeriod, "user456", _dateTimeProvider));
        Assert.Equal("タスクのタイトルは必須です", exception.Message);
    }

    [Fact(DisplayName = "UpdateCompletelyで更新者がnullの場合、DomainExceptionをスローする")]
    public void UpdateCompletely_WhenUpdatedByIsNull_ShouldThrowDomainException()
    {
        // Arrange
        var task = TaskAggregate.Create(Guid.NewGuid(), _projectId, "タスク", "説明", _scheduledPeriod, "user123", _dateTimeProvider);
        string updatedBy = null!;
        var actualPeriod = new ActualPeriod();

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() =>
            task.UpdateCompletely("新タイトル", "新説明", TaskStatus.Todo, _scheduledPeriod, actualPeriod, updatedBy, _dateTimeProvider));
        Assert.Equal("更新者のユーザーIDは必須です", exception.Message);
    }

    [Fact(DisplayName = "UpdateCompletelyでScheduledPeriodがnullの場合、DomainExceptionをスローする")]
    public void UpdateCompletely_WhenScheduledPeriodIsNull_ShouldThrowDomainException()
    {
        // Arrange
        var task = TaskAggregate.Create(Guid.NewGuid(), _projectId, "タスク", "説明", _scheduledPeriod, "user123", _dateTimeProvider);
        var actualPeriod = new ActualPeriod();

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() =>
            task.UpdateCompletely("新タイトル", "新説明", TaskStatus.Todo, null!, actualPeriod, "user456", _dateTimeProvider));
        Assert.Equal("予定期間は必須です", exception.Message);
    }

    [Fact(DisplayName = "UpdateCompletelyでActualPeriodがnullの場合、DomainExceptionをスローする")]
    public void UpdateCompletely_WhenActualPeriodIsNull_ShouldThrowDomainException()
    {
        // Arrange
        var task = TaskAggregate.Create(Guid.NewGuid(), _projectId, "タスク", "説明", _scheduledPeriod, "user123", _dateTimeProvider);

        // Act & Assert
        var exception = Assert.Throws<DomainException>(() =>
            task.UpdateCompletely("新タイトル", "新説明", TaskStatus.Todo, _scheduledPeriod, null!, "user456", _dateTimeProvider));
        Assert.Equal("実績期間は必須です", exception.Message);
    }
}

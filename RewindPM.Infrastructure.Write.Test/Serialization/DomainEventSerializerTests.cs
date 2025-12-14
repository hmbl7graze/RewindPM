using RewindPM.Domain.Events;
using RewindPM.Domain.ValueObjects;
using RewindPM.Infrastructure.Write.Serialization;
using TaskStatus = RewindPM.Domain.ValueObjects.TaskStatus;

namespace RewindPM.Infrastructure.Write.Test.Serialization;

/// <summary>
/// DomainEventSerializerのテスト
/// イベントのシリアライズとデシリアライズが正しく動作することを検証する
/// </summary>
public class DomainEventSerializerTests
{
    private readonly DomainEventSerializer _serializer;

    public DomainEventSerializerTests()
    {
        _serializer = new DomainEventSerializer();
        // 各テストでキャッシュをクリア
        DomainEventSerializer.ClearCache();
    }

    [Fact(DisplayName = "TaskCreatedイベントをシリアライズするとJSON文字列が返される")]
    public void Serialize_TaskCreatedEvent_ReturnsJsonString()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var scheduledPeriod = new ScheduledPeriod(
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddDays(7),
            40);

        var taskCreated = new TaskCreated
        {
            AggregateId = taskId,
            ProjectId = projectId,
            Title = "Test Task",
            Description = "Test Description",
            ScheduledPeriod = scheduledPeriod,
            CreatedBy = "user1"
        };

        // Act
        var json = _serializer.Serialize(taskCreated);

        // Assert
        Assert.NotNull(json);
        Assert.NotEmpty(json);
        Assert.Contains("Test Task", json);
        Assert.Contains("user1", json);
    }

    [Fact(DisplayName = "TaskCreatedのJSONをデシリアライズすると元のイベントが復元される")]
    public void Deserialize_TaskCreatedJson_ReturnsTaskCreatedEvent()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var scheduledPeriod = new ScheduledPeriod(now, now.AddDays(7), 40);

        var original = new TaskCreated
        {
            AggregateId = taskId,
            ProjectId = projectId,
            Title = "Test Task",
            Description = "Test Description",
            ScheduledPeriod = scheduledPeriod,
            CreatedBy = "user1"
        };

        var json = _serializer.Serialize(original);

        // Act
        var deserialized = _serializer.Deserialize("TaskCreated", json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.IsType<TaskCreated>(deserialized);

        var taskCreated = (TaskCreated)deserialized;
        // recordの等価性比較を利用（EventIdとOccurredAtも含めて完全一致を確認）
        Assert.Equal(original, taskCreated);
    }

    [Fact(DisplayName = "TaskStatusChangedのJSONをデシリアライズすると元のイベントが復元される")]
    public void Deserialize_TaskStatusChangedJson_ReturnsTaskStatusChangedEvent()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var original = new TaskStatusChanged
        {
            AggregateId = taskId,
            OldStatus = TaskStatus.Todo,
            NewStatus = TaskStatus.InProgress,
            ChangedBy = "user1"
        };

        var json = _serializer.Serialize(original);

        // Act
        var deserialized = _serializer.Deserialize("TaskStatusChanged", json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.IsType<TaskStatusChanged>(deserialized);

        // recordの等価性比較を利用して完全一致を確認
        Assert.Equal(original, deserialized);
    }

    [Fact(DisplayName = "複数の異なるイベント型でシリアライズとデシリアライズが正しく動作する")]
    public void SerializeAndDeserialize_MultipleEventTypes_WorksCorrectly()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var taskCreated = new TaskCreated
        {
            AggregateId = taskId,
            ProjectId = projectId,
            Title = "Test Task",
            Description = "Description",
            ScheduledPeriod = new ScheduledPeriod(now, now.AddDays(7), 40),
            CreatedBy = "user1"
        };

        var statusChanged = new TaskStatusChanged
        {
            AggregateId = taskId,
            OldStatus = TaskStatus.Todo,
            NewStatus = TaskStatus.InProgress,
            ChangedBy = "user1"
        };

        // Act
        var json1 = _serializer.Serialize(taskCreated);
        var json2 = _serializer.Serialize(statusChanged);

        var deserialized1 = _serializer.Deserialize("TaskCreated", json1);
        var deserialized2 = _serializer.Deserialize("TaskStatusChanged", json2);

        // Assert
        Assert.IsType<TaskCreated>(deserialized1);
        Assert.IsType<TaskStatusChanged>(deserialized2);
    }

    [Fact(DisplayName = "存在しないイベント型でデシリアライズするとInvalidOperationExceptionがスローされる")]
    public void Deserialize_InvalidEventType_ThrowsInvalidOperationException()
    {
        // Arrange
        var json = "{\"title\":\"Test\"}";

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => _serializer.Deserialize("NonExistentEvent", json));

        Assert.Contains("NonExistentEvent", exception.Message);
        Assert.Contains("見つかりません", exception.Message);
    }

    [Fact(DisplayName = "nullイベントをシリアライズするとArgumentNullExceptionがスローされる")]
    public void Serialize_NullEvent_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _serializer.Serialize(null!));
    }

    [Fact(DisplayName = "nullまたは空のイベント型でデシリアライズすると例外がスローされる")]
    public void Deserialize_NullOrEmptyEventType_ThrowsArgumentException()
    {
        // Arrange
        var json = "{\"title\":\"Test\"}";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _serializer.Deserialize(null!, json));
        Assert.Throws<ArgumentException>(() => _serializer.Deserialize("", json));
        Assert.Throws<ArgumentException>(() => _serializer.Deserialize("   ", json));
    }

    [Fact(DisplayName = "nullまたは空のJSONでデシリアライズすると例外がスローされる")]
    public void Deserialize_NullOrEmptyJson_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _serializer.Deserialize("TaskCreated", null!));
        Assert.Throws<ArgumentException>(() => _serializer.Deserialize("TaskCreated", ""));
        Assert.Throws<ArgumentException>(() => _serializer.Deserialize("TaskCreated", "   "));
    }
}

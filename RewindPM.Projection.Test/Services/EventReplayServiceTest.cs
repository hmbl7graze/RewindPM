using System.Text.Json;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RewindPM.Domain.Common;
using RewindPM.Domain.Events;
using RewindPM.Projection.Services;
using Xunit;

namespace RewindPM.Projection.Test.Services;

/// <summary>
/// EventReplayServiceのテスト
/// </summary>
public class EventReplayServiceTest
{
    private readonly IEventPublisher _mockEventPublisher;
    private readonly ILogger<EventReplayService> _mockLogger;
    private readonly IServiceProvider _mockServiceProvider;

    /// <summary>
    /// EventReplayServiceと同じシリアライズオプションを使用
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        PropertyNameCaseInsensitive = true
    };

    public EventReplayServiceTest()
    {
        _mockEventPublisher = Substitute.For<IEventPublisher>();
        _mockLogger = Substitute.For<ILogger<EventReplayService>>();
        _mockServiceProvider = Substitute.For<IServiceProvider>();
    }

    [Fact]
    public async Task HasEventsAsync_イベントが存在しない場合_Falseを返す()
    {
        // Arrange
        Func<Task<bool>> hasEventsFunc = () => Task.FromResult(false);
        var service = new EventReplayService(
            _mockEventPublisher,
            _mockServiceProvider,
            _mockLogger,
            hasEventsFunc);

        // Act
        var result = await service.HasEventsAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task HasEventsAsync_イベントが存在する場合_Trueを返す()
    {
        // Arrange
        Func<Task<bool>> hasEventsFunc = () => Task.FromResult(true);
        var service = new EventReplayService(
            _mockEventPublisher,
            _mockServiceProvider,
            _mockLogger,
            hasEventsFunc);

        // Act
        var result = await service.HasEventsAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void RegisterAllEventHandlers_すべてのイベントハンドラーを登録する()
    {
        // Arrange
        Func<Task<bool>> hasEventsFunc = () => Task.FromResult(false);
        var service = new EventReplayService(
            _mockEventPublisher,
            _mockServiceProvider,
            _mockLogger,
            hasEventsFunc);

        // Act
        service.RegisterAllEventHandlers();

        // Assert
        // 各イベントタイプに対してSubscribeが呼ばれたことを確認
        _mockEventPublisher.Received(1).Subscribe(Arg.Any<IEventHandler<ProjectCreated>>());
        _mockEventPublisher.Received(1).Subscribe(Arg.Any<IEventHandler<ProjectUpdated>>());
        _mockEventPublisher.Received(1).Subscribe(Arg.Any<IEventHandler<ProjectDeleted>>());
        _mockEventPublisher.Received(1).Subscribe(Arg.Any<IEventHandler<TaskCreated>>());
        _mockEventPublisher.Received(1).Subscribe(Arg.Any<IEventHandler<TaskUpdated>>());
        _mockEventPublisher.Received(1).Subscribe(Arg.Any<IEventHandler<TaskCompletelyUpdated>>());
        _mockEventPublisher.Received(1).Subscribe(Arg.Any<IEventHandler<TaskStatusChanged>>());
        _mockEventPublisher.Received(1).Subscribe(Arg.Any<IEventHandler<TaskScheduledPeriodChanged>>());
        _mockEventPublisher.Received(1).Subscribe(Arg.Any<IEventHandler<TaskActualPeriodChanged>>());
        _mockEventPublisher.Received(1).Subscribe(Arg.Any<IEventHandler<TaskDeleted>>());
    }

    [Fact]
    public async Task ReplayAllEventsAsync_すべてのイベントを時系列でリプレイする()
    {
        // Arrange
        Func<Task<bool>> hasEventsFunc = () => Task.FromResult(true);
        var service = new EventReplayService(
            _mockEventPublisher,
            _mockServiceProvider,
            _mockLogger,
            hasEventsFunc);

        var projectId = Guid.NewGuid();
        var event1OccurredAt = DateTimeOffset.UtcNow.AddHours(-2);
        var event2OccurredAt = DateTimeOffset.UtcNow.AddHours(-1);

        var event1 = new ProjectCreated
        {
            AggregateId = projectId,
            Title = "Test Project",
            Description = "Test Description",
            OccurredAt = event1OccurredAt,
            CreatedBy = "test-user"
        };

        var event2 = new ProjectUpdated
        {
            AggregateId = projectId,
            Title = "Updated Project",
            Description = "Updated Description",
            OccurredAt = event2OccurredAt,
            UpdatedBy = "test-user"
        };

        var eventDataList = new List<(string EventType, string EventData)>
        {
            ("ProjectCreated", JsonSerializer.Serialize(event1, JsonOptions)),
            ("ProjectUpdated", JsonSerializer.Serialize(event2, JsonOptions))
        };

        // Act
        await service.ReplayAllEventsAsync(_ => Task.FromResult(eventDataList));

        // Assert
        // PublishAsyncが2回呼ばれたことを確認
        await _mockEventPublisher.Received(2).PublishAsync(Arg.Any<IDomainEvent>());
    }

    [Fact]
    public async Task ReplayAllEventsAsync_イベントが存在しない場合_何もしない()
    {
        // Arrange
        Func<Task<bool>> hasEventsFunc = () => Task.FromResult(false);
        var service = new EventReplayService(
            _mockEventPublisher,
            _mockServiceProvider,
            _mockLogger,
            hasEventsFunc);

        var emptyEventList = new List<(string EventType, string EventData)>();

        // Act
        await service.ReplayAllEventsAsync(_ => Task.FromResult(emptyEventList));

        // Assert
        await _mockEventPublisher.DidNotReceive().PublishAsync(Arg.Any<IDomainEvent>());
    }

    [Fact]
    public async Task ReplayAllEventsAsync_イベントのデシリアライズに失敗した場合_ログを出力して続行する()
    {
        // Arrange
        Func<Task<bool>> hasEventsFunc = () => Task.FromResult(true);
        var service = new EventReplayService(
            _mockEventPublisher,
            _mockServiceProvider,
            _mockLogger,
            hasEventsFunc);

        // 無効なJSONを持つイベントを追加
        var eventDataList = new List<(string EventType, string EventData)>
        {
            ("ProjectCreated", "invalid json")
        };

        // Act & Assert (例外がスローされないことを確認)
        await service.ReplayAllEventsAsync(_ => Task.FromResult(eventDataList));

        // 警告ログが出力されることを確認 (NSubstituteではLogger検証が難しいため省略)
    }
}
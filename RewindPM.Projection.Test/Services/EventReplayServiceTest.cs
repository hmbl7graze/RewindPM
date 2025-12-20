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
        Func<IServiceProvider, Task<bool>> hasEventsFunc = (_) => Task.FromResult(false);
        var service = new EventReplayService(
            _mockEventPublisher,
            _mockServiceProvider,
            _mockLogger,
            hasEventsFunc);

        // Act
        var result = await service.HasEventsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task HasEventsAsync_イベントが存在する場合_Trueを返す()
    {
        // Arrange
        Func<IServiceProvider, Task<bool>> hasEventsFunc = (_) => Task.FromResult(true);
        var service = new EventReplayService(
            _mockEventPublisher,
            _mockServiceProvider,
            _mockLogger,
            hasEventsFunc);

        // Act
        var result = await service.HasEventsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void RegisterAllEventHandlers_すべてのイベントハンドラーを登録する()
    {
        // Arrange
        Func<IServiceProvider, Task<bool>> hasEventsFunc = (_) => Task.FromResult(false);
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
        Func<IServiceProvider, Task<bool>> hasEventsFunc = (_) => Task.FromResult(true);
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

        var eventList = new List<IDomainEvent> { event1, event2 };

        // Act
        await service.ReplayAllEventsAsync(_ => Task.FromResult(eventList), TestContext.Current.CancellationToken);

        // Assert
        // PublishAsyncが2回呼ばれたことを確認
        await _mockEventPublisher.Received(2).PublishAsync(Arg.Any<IDomainEvent>());
    }

    [Fact]
    public async Task ReplayAllEventsAsync_イベントが存在しない場合_何もしない()
    {
        // Arrange
        Func<IServiceProvider, Task<bool>> hasEventsFunc = (_) => Task.FromResult(false);
        var service = new EventReplayService(
            _mockEventPublisher,
            _mockServiceProvider,
            _mockLogger,
            hasEventsFunc);

        var emptyEventList = new List<IDomainEvent>();

        // Act
        await service.ReplayAllEventsAsync(_ => Task.FromResult(emptyEventList), TestContext.Current.CancellationToken);

        // Assert
        await _mockEventPublisher.DidNotReceive().PublishAsync(Arg.Any<IDomainEvent>());
    }

    [Fact]
    public async Task ReplayAllEventsAsync_非重要イベントの処理に失敗した場合_ログを出力して続行する()
    {
        // Arrange
        Func<IServiceProvider, Task<bool>> hasEventsFunc = (_) => Task.FromResult(true);
        var service = new EventReplayService(
            _mockEventPublisher,
            _mockServiceProvider,
            _mockLogger,
            hasEventsFunc);

        var projectId = Guid.NewGuid();
        var event1 = new ProjectUpdated
        {
            AggregateId = projectId,
            Title = "Test Project",
            Description = "Test Description",
            OccurredAt = DateTimeOffset.UtcNow,
            UpdatedBy = "test-user"
        };

        var eventList = new List<IDomainEvent> { event1 };

        // PublishAsyncが例外をスローするように設定
        _mockEventPublisher.PublishAsync(Arg.Any<IDomainEvent>())
            .Returns<Task>(_ => throw new InvalidOperationException("Test exception"));

        // Act & Assert (例外がスローされないことを確認)
        await service.ReplayAllEventsAsync(_ => Task.FromResult(eventList), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ReplayAllEventsAsync_重要イベントの処理に失敗した場合_例外をスローする()
    {
        // Arrange
        Func<IServiceProvider, Task<bool>> hasEventsFunc = (_) => Task.FromResult(true);
        var service = new EventReplayService(
            _mockEventPublisher,
            _mockServiceProvider,
            _mockLogger,
            hasEventsFunc);

        var projectId = Guid.NewGuid();
        var event1 = new ProjectCreated
        {
            AggregateId = projectId,
            Title = "Test Project",
            Description = "Test Description",
            OccurredAt = DateTimeOffset.UtcNow,
            CreatedBy = "test-user"
        };

        var eventList = new List<IDomainEvent> { event1 };

        // PublishAsyncが例外をスローするように設定
        _mockEventPublisher.PublishAsync(Arg.Any<IDomainEvent>())
            .Returns<Task>(_ => throw new InvalidOperationException("Test exception"));

        // Act & Assert (例外がスローされることを確認)
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.ReplayAllEventsAsync(_ => Task.FromResult(eventList), TestContext.Current.CancellationToken));
    }
}
using Microsoft.Extensions.Logging;
using NSubstitute;
using RewindPM.Domain.Common;
using RewindPM.Domain.Events;
using RewindPM.Domain.ValueObjects;
using RewindPM.Infrastructure.Write.EventPublishing;

namespace RewindPM.Infrastructure.Write.Test.EventPublishing;

/// <summary>
/// EventPublishingEventStoreDecoratorのテスト
/// デコレータパターンにより内部EventStoreに正しく委譲されることを検証する
/// </summary>
public class EventPublishingEventStoreDecoratorTests
{
    private readonly IEventStore _mockInnerEventStore;
    private readonly IEventPublisher _mockEventPublisher;
    private readonly ILogger<EventPublishingEventStoreDecorator> _mockLogger;
    private readonly EventPublishingEventStoreDecorator _decorator;

    public EventPublishingEventStoreDecoratorTests()
    {
        _mockInnerEventStore = Substitute.For<IEventStore>();
        _mockEventPublisher = Substitute.For<IEventPublisher>();
        _mockLogger = Substitute.For<ILogger<EventPublishingEventStoreDecorator>>();

        _decorator = new EventPublishingEventStoreDecorator(
            _mockInnerEventStore,
            _mockEventPublisher,
            _mockLogger);
    }

    [Fact(DisplayName = "HasEventsAsync - 内部EventStoreに委譲される")]
    public async Task HasEventsAsync_DelegatesToInnerEventStore()
    {
        // Arrange
        var expectedResult = true;
        _mockInnerEventStore.HasEventsAsync(Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        // Act
        var result = await _decorator.HasEventsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(expectedResult, result);
        await _mockInnerEventStore.Received(1).HasEventsAsync(Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "HasEventsAsync - CancellationTokenが正しく渡される")]
    public async Task HasEventsAsync_PassesCancellationToken()
    {
        // Arrange
        var expectedResult = false;
        _mockInnerEventStore.HasEventsAsync(TestContext.Current.CancellationToken)
            .Returns(expectedResult);

        // Act
        var result = await _decorator.HasEventsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(expectedResult, result);
        await _mockInnerEventStore.Received(1).HasEventsAsync(TestContext.Current.CancellationToken);
    }

    [Fact(DisplayName = "GetAllEventsAsync - 内部EventStoreに委譲される")]
    public async Task GetAllEventsAsync_DelegatesToInnerEventStore()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var expectedResult = new List<IDomainEvent>
        {
            new TaskCreated
            {
                AggregateId = Guid.NewGuid(),
                ProjectId = projectId,
                Title = "Test Task",
                Description = "Description",
                ScheduledPeriod = new ScheduledPeriod(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(7), 40),
                CreatedBy = "user1"
            },
            new TaskStatusChanged
            {
                AggregateId = Guid.NewGuid(),
                OldStatus = RewindPM.Domain.ValueObjects.TaskStatus.Todo,
                NewStatus = RewindPM.Domain.ValueObjects.TaskStatus.InProgress,
                ChangedBy = "user1"
            }
        };
        _mockInnerEventStore.GetAllEventsAsync(Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        // Act
        var result = await _decorator.GetAllEventsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(expectedResult.Count, result.Count);
        Assert.Equal(expectedResult, result);
        await _mockInnerEventStore.Received(1).GetAllEventsAsync(Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "GetAllEventsAsync - CancellationTokenが正しく渡される")]
    public async Task GetAllEventsAsync_PassesCancellationToken()
    {
        // Arrange
        var projectId = Guid.NewGuid();
        var expectedResult = new List<IDomainEvent>
        {
            new TaskCreated
            {
                AggregateId = Guid.NewGuid(),
                ProjectId = projectId,
                Title = "Test Task",
                Description = "Description",
                ScheduledPeriod = new ScheduledPeriod(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(7), 40),
                CreatedBy = "user1"
            }
        };
        _mockInnerEventStore.GetAllEventsAsync(TestContext.Current.CancellationToken)
            .Returns(expectedResult);

        // Act
        var result = await _decorator.GetAllEventsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(expectedResult, result);
        await _mockInnerEventStore.Received(1).GetAllEventsAsync(TestContext.Current.CancellationToken);
    }

    [Fact(DisplayName = "GetAllEventsAsync - 空のリストが正しく返される")]
    public async Task GetAllEventsAsync_ReturnsEmptyList_WhenNoEvents()
    {
        // Arrange
        var expectedResult = new List<IDomainEvent>();
        _mockInnerEventStore.GetAllEventsAsync(Arg.Any<CancellationToken>())
            .Returns(expectedResult);

        // Act
        var result = await _decorator.GetAllEventsAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Empty(result);
        await _mockInnerEventStore.Received(1).GetAllEventsAsync(Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "SaveEventsAsync - イベント保存後にEventPublisherで発行される")]
    public async Task SaveEventsAsync_PublishesEventsAfterSaving()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var events = new List<IDomainEvent>
        {
            new TaskCreated
            {
                AggregateId = aggregateId,
                ProjectId = projectId,
                Title = "Test Task",
                Description = "Description",
                ScheduledPeriod = new ScheduledPeriod(now, now.AddDays(7), 40),
                CreatedBy = "user1"
            }
        };

        _mockInnerEventStore.SaveEventsAsync(aggregateId, events, -1)
            .Returns(Task.CompletedTask);

        // Act
        await _decorator.SaveEventsAsync(aggregateId, events, -1);

        // Assert
        await _mockInnerEventStore.Received(1).SaveEventsAsync(aggregateId, Arg.Any<IEnumerable<IDomainEvent>>(), -1);
        await _mockEventPublisher.Received(1).PublishAsync(Arg.Any<IDomainEvent>());
    }
}

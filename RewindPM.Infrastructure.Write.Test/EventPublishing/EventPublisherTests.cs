using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RewindPM.Domain.Common;
using RewindPM.Infrastructure.Write.EventPublishing;

namespace RewindPM.Infrastructure.Write.Test.EventPublishing;

/// <summary>
/// EventPublisherの単体テスト
/// リフレクションを使わない型安全な実装を検証
/// </summary>
public class EventPublisherTests
{
    private readonly ILogger<EventPublisher> _logger;
    private readonly EventPublisher _publisher;

    public EventPublisherTests()
    {
        _logger = Substitute.For<ILogger<EventPublisher>>();
        _publisher = new EventPublisher(_logger);
    }

    #region Subscribe Tests

    [Fact(DisplayName = "Subscribeでハンドラーが正しく登録されること")]
    public async Task Subscribe_Should_Register_Handler_Successfully()
    {
        // Arrange
        var handler = Substitute.For<IEventHandler<TestEvent>>();
        var @event = new TestEvent { EventId = Guid.NewGuid(), AggregateId = Guid.NewGuid() };

        // Act
        _publisher.Subscribe(handler);
        await _publisher.PublishAsync(@event);

        // Assert
        await handler.Received(1).HandleAsync(@event);
    }

    [Fact(DisplayName = "同じイベント型に複数のハンドラーを登録できること")]
    public async Task Subscribe_Should_Allow_Multiple_Handlers_For_Same_Event_Type()
    {
        // Arrange
        var handler1 = Substitute.For<IEventHandler<TestEvent>>();
        var handler2 = Substitute.For<IEventHandler<TestEvent>>();
        var handler3 = Substitute.For<IEventHandler<TestEvent>>();
        var @event = new TestEvent { EventId = Guid.NewGuid(), AggregateId = Guid.NewGuid() };

        // Act
        _publisher.Subscribe(handler1);
        _publisher.Subscribe(handler2);
        _publisher.Subscribe(handler3);
        await _publisher.PublishAsync(@event);

        // Assert
        await handler1.Received(1).HandleAsync(@event);
        await handler2.Received(1).HandleAsync(@event);
        await handler3.Received(1).HandleAsync(@event);
    }

    [Fact(DisplayName = "異なるイベント型のハンドラーを個別に登録できること")]
    public async Task Subscribe_Should_Register_Handlers_For_Different_Event_Types_Independently()
    {
        // Arrange
        var testEventHandler = Substitute.For<IEventHandler<TestEvent>>();
        var anotherEventHandler = Substitute.For<IEventHandler<AnotherTestEvent>>();

        var testEvent = new TestEvent { EventId = Guid.NewGuid(), AggregateId = Guid.NewGuid() };
        var anotherEvent = new AnotherTestEvent { EventId = Guid.NewGuid(), AggregateId = Guid.NewGuid() };

        // Act
        _publisher.Subscribe(testEventHandler);
        _publisher.Subscribe(anotherEventHandler);

        await _publisher.PublishAsync(testEvent);
        await _publisher.PublishAsync(anotherEvent);

        // Assert
        await testEventHandler.Received(1).HandleAsync(testEvent);
        await anotherEventHandler.Received(1).HandleAsync(anotherEvent);
    }

    [Fact(DisplayName = "Subscribeにnullを渡すと例外がスローされること")]
    public void Subscribe_Should_Throw_When_Handler_Is_Null()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _publisher.Subscribe<TestEvent>(null!));
    }

    #endregion

    #region PublishAsync Tests

    [Fact(DisplayName = "PublishAsyncで登録済みハンドラーが呼び出されること")]
    public async Task PublishAsync_Should_Invoke_Registered_Handlers()
    {
        // Arrange
        var handler = Substitute.For<IEventHandler<TestEvent>>();
        var @event = new TestEvent { EventId = Guid.NewGuid(), AggregateId = Guid.NewGuid() };

        _publisher.Subscribe(handler);

        // Act
        await _publisher.PublishAsync(@event);

        // Assert
        await handler.Received(1).HandleAsync(@event);
    }

    [Fact(DisplayName = "PublishAsyncで未登録のイベント型は警告ログが記録されること")]
    public async Task PublishAsync_Should_Log_Warning_When_No_Handlers_Registered()
    {
        // Arrange
        var @event = new TestEvent { EventId = Guid.NewGuid(), AggregateId = Guid.NewGuid() };

        // Act
        await _publisher.PublishAsync(@event);

        // Assert - 警告ログが記録されたことを確認
        _logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("No handlers registered")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact(DisplayName = "PublishAsyncにnullを渡すと例外がスローされること")]
    public async Task PublishAsync_Should_Throw_When_Event_Is_Null()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _publisher.PublishAsync(null!));
    }

    [Fact(DisplayName = "PublishAsyncで複数ハンドラーが並列実行されること")]
    public async Task PublishAsync_Should_Execute_Multiple_Handlers_In_Parallel()
    {
        // Arrange
        var executionOrder = new List<int>();
        var handler1 = Substitute.For<IEventHandler<TestEvent>>();
        var handler2 = Substitute.For<IEventHandler<TestEvent>>();
        var handler3 = Substitute.For<IEventHandler<TestEvent>>();

        handler1.HandleAsync(Arg.Any<TestEvent>()).Returns(async _ =>
        {
            await Task.Delay(50);
            lock (executionOrder) { executionOrder.Add(1); }
        });

        handler2.HandleAsync(Arg.Any<TestEvent>()).Returns(async _ =>
        {
            await Task.Delay(30);
            lock (executionOrder) { executionOrder.Add(2); }
        });

        handler3.HandleAsync(Arg.Any<TestEvent>()).Returns(async _ =>
        {
            await Task.Delay(10);
            lock (executionOrder) { executionOrder.Add(3); }
        });

        var @event = new TestEvent { EventId = Guid.NewGuid(), AggregateId = Guid.NewGuid() };

        _publisher.Subscribe(handler1);
        _publisher.Subscribe(handler2);
        _publisher.Subscribe(handler3);

        // Act
        await _publisher.PublishAsync(@event);

        // Assert - 並列実行なので、遅延が短い順に完了する
        Assert.Equal(3, executionOrder.Count);
        Assert.Equal(3, executionOrder[0]); // 10ms delay - 最初に完了
        Assert.Equal(2, executionOrder[1]); // 30ms delay - 2番目に完了
        Assert.Equal(1, executionOrder[2]); // 50ms delay - 最後に完了
    }

    #endregion

    #region Error Handling Tests

    [Fact(DisplayName = "1つのハンドラーで例外が発生しても他のハンドラーは実行されること")]
    public async Task PublishAsync_Should_Continue_Other_Handlers_When_One_Fails()
    {
        // Arrange
        var failingHandler = Substitute.For<IEventHandler<TestEvent>>();
        var successHandler1 = Substitute.For<IEventHandler<TestEvent>>();
        var successHandler2 = Substitute.For<IEventHandler<TestEvent>>();

        failingHandler.HandleAsync(Arg.Any<TestEvent>())
            .Throws(new InvalidOperationException("Handler failed"));

        var @event = new TestEvent { EventId = Guid.NewGuid(), AggregateId = Guid.NewGuid() };

        _publisher.Subscribe(successHandler1);
        _publisher.Subscribe(failingHandler);
        _publisher.Subscribe(successHandler2);

        // Act
        await _publisher.PublishAsync(@event);

        // Assert - 失敗したハンドラー以外は正常に実行される
        await successHandler1.Received(1).HandleAsync(@event);
        await successHandler2.Received(1).HandleAsync(@event);

        // エラーログが記録されたことを確認
        _logger.Received().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Is<Exception>(ex => ex.Message == "Handler failed"),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact(DisplayName = "複数のハンドラーで例外が発生してもすべて実行されること")]
    public async Task PublishAsync_Should_Execute_All_Handlers_Even_If_Multiple_Fail()
    {
        // Arrange
        var failingHandler1 = Substitute.For<IEventHandler<TestEvent>>();
        var failingHandler2 = Substitute.For<IEventHandler<TestEvent>>();
        var successHandler = Substitute.For<IEventHandler<TestEvent>>();

        failingHandler1.HandleAsync(Arg.Any<TestEvent>())
            .Throws(new InvalidOperationException("Handler 1 failed"));

        failingHandler2.HandleAsync(Arg.Any<TestEvent>())
            .Throws(new InvalidOperationException("Handler 2 failed"));

        var @event = new TestEvent { EventId = Guid.NewGuid(), AggregateId = Guid.NewGuid() };

        _publisher.Subscribe(failingHandler1);
        _publisher.Subscribe(successHandler);
        _publisher.Subscribe(failingHandler2);

        // Act
        await _publisher.PublishAsync(@event);

        // Assert - 成功したハンドラーは実行される
        await successHandler.Received(1).HandleAsync(@event);

        // 2つのエラーログが記録されたことを確認
        _logger.Received(2).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Is<Exception>(ex => ex.Message.Contains("failed")),
            Arg.Any<Func<object, Exception?, string>>());
    }

    #endregion

    #region Type Safety Tests

    [Fact(DisplayName = "ラッパーが正しい型でハンドラーを呼び出すこと")]
    public async Task Wrapper_Should_Invoke_Handler_With_Correct_Type()
    {
        // Arrange
        TestEvent? receivedEvent = null;
        var handler = Substitute.For<IEventHandler<TestEvent>>();
        handler.HandleAsync(Arg.Do<TestEvent>(e => receivedEvent = e))
            .Returns(Task.CompletedTask);

        var @event = new TestEvent
        {
            EventId = Guid.NewGuid(),
            AggregateId = Guid.NewGuid(),
            TestData = "Test Data"
        };

        _publisher.Subscribe(handler);

        // Act
        await _publisher.PublishAsync(@event);

        // Assert
        Assert.NotNull(receivedEvent);
        Assert.Equal(@event.EventId, receivedEvent.EventId);
        Assert.Equal(@event.AggregateId, receivedEvent.AggregateId);
        Assert.Equal(@event.TestData, receivedEvent.TestData);
    }

    [Fact(DisplayName = "継承されたイベントが正しく処理されること")]
    public async Task Publisher_Should_Handle_Derived_Events_Correctly()
    {
        // Arrange
        var baseHandler = Substitute.For<IEventHandler<DerivedTestEvent>>();
        var derivedEvent = new DerivedTestEvent
        {
            EventId = Guid.NewGuid(),
            AggregateId = Guid.NewGuid(),
            TestData = "Base Data",
            AdditionalData = "Derived Data"
        };

        _publisher.Subscribe(baseHandler);

        // Act
        await _publisher.PublishAsync(derivedEvent);

        // Assert
        await baseHandler.Received(1).HandleAsync(derivedEvent);
    }

    #endregion

    #region Test Event Classes

    public class TestEvent : IDomainEvent
    {
        public Guid EventId { get; set; }
        public Guid AggregateId { get; set; }
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
        public string EventType => nameof(TestEvent);
        public string TestData { get; set; } = string.Empty;
    }

    public class AnotherTestEvent : IDomainEvent
    {
        public Guid EventId { get; set; }
        public Guid AggregateId { get; set; }
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
        public string EventType => nameof(AnotherTestEvent);
    }

    public class DerivedTestEvent : TestEvent
    {
        public string AdditionalData { get; set; } = string.Empty;
    }

    #endregion
}

using Microsoft.Extensions.Logging;
using RewindPM.Domain.Common;

namespace RewindPM.Infrastructure.Write.EventPublishing;

/// <summary>
/// ドメインイベントを発行する実装
/// 登録されたハンドラーにイベントを通知する
/// </summary>
public class EventPublisher : IEventPublisher
{
    private readonly Dictionary<Type, List<IEventHandlerWrapper>> _handlers = new();
    private readonly ILogger<EventPublisher> _logger;

    public EventPublisher(ILogger<EventPublisher> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// イベントハンドラーを登録する
    /// </summary>
    public void Subscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : class, IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(handler);

        var eventType = typeof(TEvent);

        if (!_handlers.ContainsKey(eventType))
        {
            _handlers[eventType] = new List<IEventHandlerWrapper>();
        }

        // リフレクションを使わずに型安全なラッパーを使用
        var wrapper = new EventHandlerWrapper<TEvent>(handler);
        _handlers[eventType].Add(wrapper);

        _logger.LogDebug("Subscribed handler {HandlerType} for event {EventType}",
            handler.GetType().Name, eventType.Name);
    }

    /// <summary>
    /// ドメインイベントを発行する
    /// 登録されているハンドラーに通知される
    /// </summary>
    public async Task PublishAsync(IDomainEvent @event)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var eventType = @event.GetType();

        _logger.LogInformation("Publishing event {EventType} with ID {EventId}",
            eventType.Name, @event.EventId);

        if (!_handlers.TryGetValue(eventType, out var handlers) || !handlers.Any())
        {
            _logger.LogWarning("No handlers registered for event {EventType}", eventType.Name);
            return;
        }

        // 各ハンドラーを並列実行
        var tasks = handlers.Select(wrapper => InvokeHandlerAsync(wrapper, @event, eventType));

        await Task.WhenAll(tasks);

        _logger.LogInformation("Successfully published event {EventType} to {HandlerCount} handler(s)",
            eventType.Name, handlers.Count);
    }

    /// <summary>
    /// ハンドラーを呼び出す
    /// </summary>
    private async Task InvokeHandlerAsync(IEventHandlerWrapper wrapper, IDomainEvent @event, Type eventType)
    {
        try
        {
            await wrapper.HandleAsync(@event);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error handling event {EventType} with handler {HandlerType}",
                eventType.Name, wrapper.HandlerTypeName);
            // ハンドラーのエラーは他のハンドラーに影響させない
            // 必要に応じてリトライや Dead Letter Queue への送信を検討
        }
    }

    /// <summary>
    /// 非ジェネリックなハンドラーラッパーインターフェース
    /// </summary>
    private interface IEventHandlerWrapper
    {
        Task HandleAsync(IDomainEvent @event);
        string HandlerTypeName { get; }
    }

    /// <summary>
    /// ジェネリックハンドラーをラップして非ジェネリックインターフェースで呼び出せるようにする
    /// </summary>
    private class EventHandlerWrapper<TEvent> : IEventHandlerWrapper
        where TEvent : class, IDomainEvent
    {
        private readonly IEventHandler<TEvent> _handler;

        public EventHandlerWrapper(IEventHandler<TEvent> handler)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public string HandlerTypeName => _handler.GetType().Name;

        public Task HandleAsync(IDomainEvent @event)
        {
            if (@event is TEvent typedEvent)
            {
                return _handler.HandleAsync(typedEvent);
            }

            // イベント型が一致しない場合（通常発生しない）
            throw new InvalidOperationException(
                $"Event type mismatch. Expected {typeof(TEvent).Name}, but got {@event.GetType().Name}");
        }
    }
}

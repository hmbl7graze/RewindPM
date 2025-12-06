using Microsoft.Extensions.Logging;
using RewindPM.Domain.Common;

namespace RewindPM.Infrastructure.Write.EventPublishing;

/// <summary>
/// ドメインイベントを発行する実装
/// 登録されたハンドラーにイベントを通知する
/// </summary>
public class EventPublisher : IEventPublisher
{
    private readonly Dictionary<Type, List<object>> _handlers = new();
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
            _handlers[eventType] = new List<object>();
        }

        _handlers[eventType].Add(handler);

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
        var tasks = new List<Task>();

        foreach (var handler in handlers)
        {
            tasks.Add(InvokeHandlerAsync(handler, @event, eventType));
        }

        await Task.WhenAll(tasks);

        _logger.LogInformation("Successfully published event {EventType} to {HandlerCount} handler(s)",
            eventType.Name, handlers.Count);
    }

    /// <summary>
    /// ハンドラーを呼び出す（リフレクション使用）
    /// </summary>
    private async Task InvokeHandlerAsync(object handler, IDomainEvent @event, Type eventType)
    {
        try
        {
            var handleMethod = handler.GetType().GetMethod(nameof(IEventHandler<IDomainEvent>.HandleAsync));
            if (handleMethod != null)
            {
                var task = handleMethod.Invoke(handler, new object[] { @event }) as Task;
                if (task != null)
                {
                    await task;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error handling event {EventType} with handler {HandlerType}",
                eventType.Name, handler.GetType().Name);
            // ハンドラーのエラーは他のハンドラーに影響させない
            // 必要に応じてリトライや Dead Letter Queue への送信を検討
        }
    }
}

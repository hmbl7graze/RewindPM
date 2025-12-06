namespace RewindPM.Domain.Common;

/// <summary>
/// ドメインイベントを発行するためのインターフェース
/// イベントストアからプロジェクション層へのイベント通知を抽象化
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// イベントハンドラーを登録する
    /// </summary>
    /// <typeparam name="TEvent">イベントの型</typeparam>
    /// <param name="handler">イベントハンドラー</param>
    void Subscribe<TEvent>(IEventHandler<TEvent> handler) where TEvent : class, IDomainEvent;

    /// <summary>
    /// ドメインイベントを発行する
    /// 登録されているハンドラーに通知される
    /// </summary>
    /// <param name="event">発行するドメインイベント</param>
    /// <returns>非同期タスク</returns>
    Task PublishAsync(IDomainEvent @event);
}

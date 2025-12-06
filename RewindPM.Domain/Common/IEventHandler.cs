namespace RewindPM.Domain.Common;

/// <summary>
/// ドメインイベントを処理するハンドラーのインターフェース
/// </summary>
/// <typeparam name="TEvent">処理するイベントの型</typeparam>
public interface IEventHandler<in TEvent> where TEvent : class, IDomainEvent
{
    /// <summary>
    /// イベントを処理する
    /// </summary>
    /// <param name="event">処理するドメインイベント</param>
    /// <returns>非同期タスク</returns>
    Task HandleAsync(TEvent @event);
}

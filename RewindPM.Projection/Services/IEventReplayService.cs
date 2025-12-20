using RewindPM.Domain.Common;

namespace RewindPM.Projection.Services;

/// <summary>
/// イベントストアからイベントをリプレイしてReadModelを再構築するサービスのインターフェース
/// </summary>
public interface IEventReplayService
{
    /// <summary>
    /// EventStoreにイベントが存在するかチェックする
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>イベントが存在する場合はtrue</returns>
    Task<bool> HasEventsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// すべてのイベントハンドラーをイベントパブリッシャーに登録する
    /// </summary>
    void RegisterAllEventHandlers();

    /// <summary>
    /// EventStoreからすべてのイベントをリプレイしてReadModelを再構築する
    /// イベントデータの取得は外部から提供される関数を使用し、デシリアライズは内部で実行される
    /// </summary>
    /// <param name="getEventsAsync">EventStoreからイベントデータを取得する関数</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    Task ReplayAllEventsAsync(Func<CancellationToken, Task<List<(string EventType, string EventData)>>> getEventsAsync, CancellationToken cancellationToken = default);
}

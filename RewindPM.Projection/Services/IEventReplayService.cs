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
    /// デシリアライズ済みのドメインイベントを受け取る
    /// </summary>
    /// <param name="getEventsAsync">EventStoreからデシリアライズ済みイベントを取得する関数</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    Task ReplayAllEventsAsync(Func<CancellationToken, Task<List<IDomainEvent>>> getEventsAsync, CancellationToken cancellationToken = default);
}

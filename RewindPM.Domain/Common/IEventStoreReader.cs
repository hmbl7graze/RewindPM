namespace RewindPM.Domain.Common;

/// <summary>
/// EventStoreへの読み取り専用クエリを提供するインターフェース
/// リプレイやシステム全体のイベント取得など、読み取り専用操作を提供する
/// </summary>
public interface IEventStoreReader
{
    /// <summary>
    /// EventStoreにイベントが存在するかチェックする
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>イベントが存在する場合はtrue</returns>
    Task<bool> HasEventsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// EventStoreから全イベントを時系列順に取得する（リプレイ用）
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>イベントタイプとイベントデータのリスト</returns>
    Task<List<(string EventType, string EventData)>> GetAllEventsAsync(CancellationToken cancellationToken = default);
}

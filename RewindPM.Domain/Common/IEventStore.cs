namespace RewindPM.Domain.Common;

/// <summary>
/// イベントストアのインターフェース
/// イベントソーシングの永続化層を抽象化する
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// イベントを保存する
    /// </summary>
    /// <param name="aggregateId">AggregateのID</param>
    /// <param name="events">保存するイベントのコレクション</param>
    /// <param name="expectedVersion">期待されるバージョン（楽観的同時実行制御用）</param>
    /// <returns>非同期タスク</returns>
    /// <exception cref="ConcurrencyException">期待されるバージョンと実際のバージョンが一致しない場合</exception>
    Task SaveEventsAsync(Guid aggregateId, IEnumerable<IDomainEvent> events, int expectedVersion);

    /// <summary>
    /// 指定されたAggregateの全イベントを取得する
    /// </summary>
    /// <param name="aggregateId">AggregateのID</param>
    /// <returns>イベントのリスト（時系列順）</returns>
    Task<List<IDomainEvent>> GetEventsAsync(Guid aggregateId);

    /// <summary>
    /// 指定された時点までのイベントを取得する（タイムトラベル用）
    /// </summary>
    /// <param name="aggregateId">AggregateのID</param>
    /// <param name="pointInTime">取得する時点</param>
    /// <returns>指定時点までのイベントのリスト（時系列順）</returns>
    Task<List<IDomainEvent>> GetEventsUntilAsync(Guid aggregateId, DateTimeOffset pointInTime);

    /// <summary>
    /// 指定されたイベント種別のイベントを期間指定で取得する
    /// </summary>
    /// <param name="eventType">イベント種別名</param>
    /// <param name="from">開始日時（nullの場合は最初から）</param>
    /// <param name="to">終了日時（nullの場合は最後まで）</param>
    /// <returns>条件に一致するイベントのリスト（時系列順）</returns>
    Task<List<IDomainEvent>> GetEventsByTypeAsync(string eventType, DateTimeOffset? from = null, DateTimeOffset? to = null);
}

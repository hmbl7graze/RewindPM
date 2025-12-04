using RewindPM.Domain.Common;

namespace RewindPM.Domain.Aggregates;

/// <summary>
/// Aggregateの抽象基底クラス
/// イベントソーシングパターンを実装し、全てのAggregateの基盤となる
/// </summary>
public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _uncommittedEvents = [];

    /// <summary>
    /// AggregateのID
    /// </summary>
    public Guid Id { get; protected set; }

    /// <summary>
    /// 楽観的同時実行制御用のバージョン番号
    /// イベントが適用されるたびにインクリメントされる
    /// </summary>
    public int Version { get; private set; } = -1;

    /// <summary>
    /// まだイベントストアに保存されていない未コミットイベントのリスト
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> UncommittedEvents => _uncommittedEvents.AsReadOnly();

    /// <summary>
    /// 新しいイベントを発生させる
    /// イベントを未コミットリストに追加し、状態に適用する
    /// </summary>
    /// <param name="event">発生させるドメインイベント</param>
    protected void ApplyEvent(IDomainEvent @event)
    {
        // イベントを状態に適用
        When(@event);

        // 未コミットイベントリストに追加
        _uncommittedEvents.Add(@event);
    }

    /// <summary>
    /// イベントストアから取得したイベントを再生してAggregateの状態を復元する
    /// </summary>
    /// <param name="events">再生するイベントのリスト</param>
    public void ReplayEvents(IEnumerable<IDomainEvent> events)
    {
        foreach (var @event in events)
        {
            // イベントを状態に適用（未コミットリストには追加しない）
            When(@event);

            // バージョンをインクリメント
            Version++;
        }
    }

    /// <summary>
    /// イベントストアへの保存後、未コミットイベントをクリアする
    /// </summary>
    public void ClearUncommittedEvents()
    {
        _uncommittedEvents.Clear();
    }

    /// <summary>
    /// イベントに応じてAggregateの状態を変更する
    /// 各Aggregateで具体的なイベント処理をオーバーライドして実装する
    /// </summary>
    /// <param name="event">適用するドメインイベント</param>
    protected abstract void When(IDomainEvent @event);
}

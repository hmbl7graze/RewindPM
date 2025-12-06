using System.Reflection;
using RewindPM.Application.Write.Repositories;
using RewindPM.Domain.Aggregates;
using RewindPM.Domain.Common;

namespace RewindPM.Infrastructure.Write.Repositories;

/// <summary>
/// Aggregateリポジトリの実装
/// Event Storeを使用してAggregateの保存・取得を行う
/// </summary>
public class AggregateRepository : IAggregateRepository
{
    private readonly IEventStore _eventStore;

    public AggregateRepository(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    /// <summary>
    /// 指定されたIDのAggregateを取得する
    /// イベントストアからイベントを取得し、リプレイしてAggregateを再構築する
    /// </summary>
    public async Task<T?> GetByIdAsync<T>(Guid aggregateId) where T : AggregateRoot
    {
        // イベントストアからイベントを取得
        var events = await _eventStore.GetEventsAsync(aggregateId);

        // イベントが存在しない場合はnullを返す
        if (events.Count == 0)
        {
            return null;
        }

        // リフレクションを使用してAggregateのインスタンスを作成
        // internalコンストラクタにアクセスするため
        var aggregate = CreateAggregateInstance<T>();

        // イベントをリプレイしてAggregateの状態を復元
        aggregate.ReplayEvents(events);

        return aggregate;
    }

    /// <summary>
    /// Aggregateを保存する
    /// 未コミットイベントをEvent Storeに永続化する
    /// </summary>
    public async Task SaveAsync<T>(T aggregate) where T : AggregateRoot
    {
        // 未コミットイベントを取得
        var uncommittedEvents = aggregate.UncommittedEvents.ToList();

        // イベントが存在しない場合は何もしない
        if (uncommittedEvents.Count == 0)
        {
            return;
        }

        // イベントストアに保存
        await _eventStore.SaveEventsAsync(
            aggregate.Id,
            uncommittedEvents,
            aggregate.Version
        );

        // 未コミットイベントをクリア
        aggregate.ClearUncommittedEvents();
    }

    /// <summary>
    /// リフレクションを使用してAggregateのインスタンスを作成する
    /// internalコンストラクタにアクセスするために使用
    /// </summary>
    private static T CreateAggregateInstance<T>() where T : AggregateRoot
    {
        // パラメーターなしのコンストラクタ（public/internal/private）を取得
        var constructor = typeof(T).GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            Type.EmptyTypes,
            null
        );

        if (constructor == null)
        {
            throw new InvalidOperationException(
                $"型 '{typeof(T).Name}' にパラメーターなしのコンストラクタが見つかりません"
            );
        }

        // インスタンスを作成
        return (T)constructor.Invoke(null);
    }
}

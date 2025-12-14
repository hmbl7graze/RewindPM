using RewindPM.Domain.Aggregates;

namespace RewindPM.Application.Write.Repositories;

/// <summary>
/// Aggregateリポジトリのインターフェース
/// Event Storeへの依存を隠蔽し、Aggregateの保存・取得を抽象化する
/// </summary>
public interface IAggregateRepository
{
    /// <summary>
    /// 指定されたIDのAggregateを取得する
    /// </summary>
    /// <typeparam name="T">Aggregateの型</typeparam>
    /// <param name="aggregateId">AggregateのID</param>
    /// <returns>Aggregateインスタンス（存在しない場合はnull）</returns>
    Task<T?> GetByIdAsync<T>(Guid aggregateId) where T : AggregateRoot;

    /// <summary>
    /// Aggregateを保存する（未コミットイベントをEvent Storeに永続化）
    /// </summary>
    /// <typeparam name="T">Aggregateの型</typeparam>
    /// <param name="aggregate">保存するAggregate</param>
    Task SaveAsync<T>(T aggregate) where T : AggregateRoot;

    /// <summary>
    /// 指定されたプロジェクトに関連するタスクのIDリストを取得する
    /// イベントストアから削除されていないタスクのIDを取得
    /// </summary>
    /// <param name="projectId">プロジェクトID</param>
    /// <returns>タスクIDのリスト</returns>
    Task<List<Guid>> GetTaskIdsByProjectIdAsync(Guid projectId);
}

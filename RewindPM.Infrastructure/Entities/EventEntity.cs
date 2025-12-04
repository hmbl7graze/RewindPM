namespace RewindPM.Infrastructure.Entities;

/// <summary>
/// イベントストアのエンティティ
/// イベントをSQLiteに永続化するためのモデル
/// </summary>
public class EventEntity
{
    /// <summary>
    /// イベントID（主キー）
    /// </summary>
    public Guid EventId { get; set; }

    /// <summary>
    /// AggregateのID（インデックス）
    /// </summary>
    public Guid AggregateId { get; set; }

    /// <summary>
    /// イベントの種別名（例: TaskCreated, TaskStatusChanged）
    /// </summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// イベントデータ（JSON形式）
    /// </summary>
    public string EventData { get; set; } = string.Empty;

    /// <summary>
    /// イベント発生日時（UTC、インデックス）
    /// タイムトラベル機能で使用
    /// </summary>
    public DateTime OccurredAt { get; set; }

    /// <summary>
    /// Aggregateのバージョン
    /// 楽観的同時実行制御に使用
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// レコード作成日時（UTC）
    /// 監査用
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

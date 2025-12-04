using System.Reflection;
using System.Text.Json;
using RewindPM.Domain.Common;

namespace RewindPM.Infrastructure.Serialization;

/// <summary>
/// ドメインイベントのシリアライザー
/// イベントをJSON形式で保存・復元する
/// </summary>
public class DomainEventSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Dictionary<string, Type> EventTypeCache = new();
    private static readonly object CacheLock = new();

    /// <summary>
    /// ドメインイベントをJSON文字列にシリアライズする
    /// </summary>
    /// <param name="domainEvent">シリアライズするイベント</param>
    /// <returns>JSON文字列</returns>
    public string Serialize(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        return JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), JsonOptions);
    }

    /// <summary>
    /// JSON文字列をドメインイベントにデシリアライズする
    /// </summary>
    /// <param name="eventType">イベントの型名</param>
    /// <param name="eventData">JSON文字列</param>
    /// <returns>復元されたドメインイベント</returns>
    /// <exception cref="InvalidOperationException">イベント型が見つからない場合</exception>
    public IDomainEvent Deserialize(string eventType, string eventData)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventData);

        var type = GetEventType(eventType);
        var domainEvent = JsonSerializer.Deserialize(eventData, type, JsonOptions) as IDomainEvent;

        if (domainEvent == null)
        {
            throw new InvalidOperationException($"イベント '{eventType}' のデシリアライズに失敗しました");
        }

        return domainEvent;
    }

    /// <summary>
    /// イベント型名から実際のType型を取得する
    /// リフレクションを使用してDomain層のイベントを動的に解決
    /// </summary>
    /// <param name="eventTypeName">イベント型名</param>
    /// <returns>イベントのType</returns>
    /// <exception cref="InvalidOperationException">イベント型が見つからない場合</exception>
    private Type GetEventType(string eventTypeName)
    {
        // キャッシュから取得を試みる
        if (EventTypeCache.TryGetValue(eventTypeName, out var cachedType))
        {
            return cachedType;
        }

        lock (CacheLock)
        {
            // ダブルチェックロッキング
            if (EventTypeCache.TryGetValue(eventTypeName, out var cachedType2))
            {
                return cachedType2;
            }

            // Domain層のアセンブリからイベント型を検索
            var domainAssembly = typeof(IDomainEvent).Assembly;
            var eventType = domainAssembly.GetTypes()
                .FirstOrDefault(t =>
                    t.Name == eventTypeName &&
                    typeof(IDomainEvent).IsAssignableFrom(t) &&
                    !t.IsAbstract &&
                    !t.IsInterface);

            if (eventType == null)
            {
                throw new InvalidOperationException(
                    $"イベント型 '{eventTypeName}' が見つかりません。Domain層に定義されているか確認してください。");
            }

            // キャッシュに追加
            EventTypeCache[eventTypeName] = eventType;

            return eventType;
        }
    }

    /// <summary>
    /// キャッシュをクリアする（テスト用）
    /// </summary>
    internal static void ClearCache()
    {
        lock (CacheLock)
        {
            EventTypeCache.Clear();
        }
    }
}

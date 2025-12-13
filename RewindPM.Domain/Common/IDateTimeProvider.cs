namespace RewindPM.Domain.Common;

/// <summary>
/// 現在時刻を提供するインターフェース
/// テストやSeedDataで時刻を制御するために使用
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>
    /// 現在のUTC時刻を取得
    /// </summary>
    DateTimeOffset UtcNow { get; }
}

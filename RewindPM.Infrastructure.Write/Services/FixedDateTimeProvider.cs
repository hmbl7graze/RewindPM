using RewindPM.Domain.Common;

namespace RewindPM.Infrastructure.Write.Services;

/// <summary>
/// 固定時刻を提供する実装
/// テストやSeedDataで時刻を制御するために使用
/// </summary>
public class FixedDateTimeProvider : IDateTimeProvider
{
    private DateTimeOffset _currentTime;

    /// <summary>
    /// 指定した時刻で初期化
    /// </summary>
    /// <param name="fixedTime">固定する時刻（UTC）</param>
    public FixedDateTimeProvider(DateTime fixedTime)
    {
        _currentTime = fixedTime;
    }

    /// <summary>
    /// 設定された固定時刻を返す
    /// </summary>
    public DateTimeOffset UtcNow => _currentTime;

    /// <summary>
    /// 現在時刻を設定
    /// SeedDataで時系列に沿ってデータを作成する際に使用
    /// </summary>
    /// <param name="newTime">新しい時刻（UTC）</param>
    public void SetCurrentTime(DateTimeOffset newTime)
    {
        _currentTime = newTime;
    }

    /// <summary>
    /// 現在時刻を指定した時間分進める
    /// </summary>
    /// <param name="duration">進める時間</param>
    public void Advance(TimeSpan duration)
    {
        _currentTime = _currentTime.Add(duration);
    }
}

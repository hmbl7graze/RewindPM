using RewindPM.Domain.Common;

namespace RewindPM.Infrastructure.Write.Services;

/// <summary>
/// システム時刻を提供する実装
/// 本番環境やテスト以外で使用
/// </summary>
public class SystemDateTimeProvider : IDateTimeProvider
{
    /// <summary>
    /// 現在のシステムUTC時刻を返す
    /// </summary>
    public DateTime UtcNow => DateTime.UtcNow;
}

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RewindPM.Infrastructure.Read.Configuration;

namespace RewindPM.Infrastructure.Read.Services;

/// <summary>
/// タイムゾーン変換サービスの実装
/// </summary>
public class TimeZoneService : ITimeZoneService
{
    private readonly ILogger<TimeZoneService> _logger;

    /// <summary>
    /// 設定されているタイムゾーン
    /// </summary>
    public TimeZoneInfo TimeZone { get; }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="settings">タイムゾーン設定</param>
    /// <param name="logger">ロガー</param>
    public TimeZoneService(IOptions<TimeZoneSettings> settings, ILogger<TimeZoneService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        var timeZoneId = settings.Value.TimeZoneId;
        try
        {
            TimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            _logger.LogInformation("TimeZone initialized: {TimeZoneId} ({DisplayName})", TimeZone.Id, TimeZone.DisplayName);
        }
        catch (TimeZoneNotFoundException ex)
        {
            _logger.LogWarning(ex, "Invalid TimeZone ID '{TimeZoneId}' specified. Falling back to UTC.", timeZoneId);
            TimeZone = TimeZoneInfo.Utc;
        }
    }

    /// <summary>
    /// UTC時刻からスナップショット日付を計算(タイムゾーンを考慮)
    /// </summary>
    /// <param name="utcDateTime">UTC時刻</param>
    /// <returns>ローカル時刻の日付部分</returns>
    public DateTimeOffset GetSnapshotDate(DateTimeOffset utcDateTime)
    {
        var localDateTime = TimeZoneInfo.ConvertTime(utcDateTime, TimeZone);
        return new DateTimeOffset(localDateTime.Date, localDateTime.Offset);
    }

    /// <summary>
    /// UTC時刻をローカル時刻に変換
    /// </summary>
    /// <param name="utcDateTime">UTC時刻</param>
    /// <returns>ローカル時刻</returns>
    public DateTimeOffset ConvertUtcToLocal(DateTimeOffset utcDateTime)
    {
        return TimeZoneInfo.ConvertTime(utcDateTime, TimeZone);
    }
}

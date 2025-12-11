using Microsoft.Extensions.Options;
using RewindPM.Infrastructure.Read.Configuration;

namespace RewindPM.Infrastructure.Read.Services;

/// <summary>
/// タイムゾーン変換サービスの実装
/// </summary>
public class TimeZoneService : ITimeZoneService
{
    /// <summary>
    /// 設定されているタイムゾーン
    /// </summary>
    public TimeZoneInfo TimeZone { get; }

    /// <summary>
    /// コンストラクタ
    /// </summary>
    /// <param name="settings">タイムゾーン設定</param>
    public TimeZoneService(IOptions<TimeZoneSettings> settings)
    {
        TimeZone = settings.Value.GetTimeZoneInfo();
    }

    /// <summary>
    /// UTC時刻からスナップショット日付を計算（タイムゾーンを考慮）
    /// </summary>
    /// <param name="utcDateTime">UTC時刻</param>
    /// <returns>ローカル時刻の日付部分</returns>
    public DateTime GetSnapshotDate(DateTimeOffset utcDateTime)
    {
        var localDateTime = TimeZoneInfo.ConvertTime(utcDateTime, TimeZone);
        return localDateTime.Date; // ローカル時刻の日付部分のみ
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

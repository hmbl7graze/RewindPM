namespace RewindPM.Infrastructure.Read.Configuration;

/// <summary>
/// タイムゾーン設定
/// </summary>
public class TimeZoneSettings
{
    /// <summary>
    /// 設定セクション名
    /// </summary>
    public const string SectionName = "TimeZone";

    /// <summary>
    /// タイムゾーンID (例: "UTC", "Asia/Tokyo", "America/New_York")
    /// </summary>
    public string TimeZoneId { get; set; } = "UTC";
}

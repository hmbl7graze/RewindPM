namespace RewindPM.Infrastructure.Read.SQLite.Entities;

/// <summary>
/// システムメタデータエンティティ（設定情報の保存用）
/// </summary>
public class SystemMetadataEntity
{
    /// <summary>
    /// タイムゾーンIDのメタデータキー
    /// </summary>
    public const string TimeZoneMetadataKey = "CurrentTimeZoneId";

    /// <summary>
    /// メタデータのキー（主キー）
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// メタデータの値
    /// </summary>
    public string Value { get; set; } = string.Empty;
}

namespace RewindPM.Infrastructure.Read.Services;

/// <summary>
/// タイムゾーン変換サービスのインターフェース
/// </summary>
public interface ITimeZoneService
{
    /// <summary>
    /// 設定されているタイムゾーン
    /// </summary>
    TimeZoneInfo TimeZone { get; }

    /// <summary>
    /// UTC時刻からスナップショット日付を計算（タイムゾーンを考慮）
    /// </summary>
    /// <param name="utcDateTime">UTC時刻</param>
    /// <returns>ローカル時刻の日付部分</returns>
    DateTime GetSnapshotDate(DateTime utcDateTime);

    /// <summary>
    /// UTC時刻をローカル時刻に変換
    /// </summary>
    /// <param name="utcDateTime">UTC時刻</param>
    /// <returns>ローカル時刻</returns>
    DateTime ConvertUtcToLocal(DateTime utcDateTime);
}

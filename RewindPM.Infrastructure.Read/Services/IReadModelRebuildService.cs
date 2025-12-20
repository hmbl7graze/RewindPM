using Microsoft.EntityFrameworkCore.Storage;

namespace RewindPM.Infrastructure.Read.Services;

/// <summary>
/// ReadModelの再構築を管理するサービスのインターフェース
/// </summary>
public interface IReadModelRebuildService
{
    /// <summary>
    /// タイムゾーン変更を検出し、必要に応じてReadModelを再構築する
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>ReadModelがクリアされた場合はtrue、そうでない場合はfalse</returns>
    Task<bool> CheckAndRebuildIfTimeZoneChangedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// ReadModelのデータをクリアし、タイムゾーンメタデータを更新する（トランザクション内で完了）
    /// </summary>
    /// <param name="newTimeZoneId">新しいタイムゾーンID</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    Task ClearReadModelAndUpdateTimeZoneAsync(string newTimeZoneId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 現在保存されているタイムゾーンIDを取得する
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>保存されているタイムゾーンID (未設定の場合はnull)</returns>
    Task<string?> GetStoredTimeZoneIdAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// タイムゾーンメタデータを初期化する（トランザクション付き）
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    Task InitializeTimeZoneMetadataAsync(CancellationToken cancellationToken = default);
}

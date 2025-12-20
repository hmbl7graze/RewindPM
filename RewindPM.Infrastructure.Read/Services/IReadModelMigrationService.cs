namespace RewindPM.Infrastructure.Read.Services;

/// <summary>
/// ReadModelデータベースのマイグレーション管理サービスのインターフェース
/// </summary>
public interface IReadModelMigrationService
{
    /// <summary>
    /// 保留中のマイグレーションがあるかどうかを確認する
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>保留中のマイグレーションがある場合はtrue</returns>
    Task<bool> HasPendingMigrationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// データベースマイグレーションを適用する
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    Task ApplyMigrationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// ReadModelが空（プロジェクトが存在しない）かどうかを確認する
    /// </summary>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>ReadModelが空の場合はtrue</returns>
    Task<bool> IsEmptyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// DbContextの変更追跡をクリアする
    /// </summary>
    void ClearChangeTracking();
}

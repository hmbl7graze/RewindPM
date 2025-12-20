namespace RewindPM.Infrastructure.Write.Services;

/// <summary>
/// EventStoreデータベースのマイグレーション管理サービスのインターフェース
/// </summary>
public interface IEventStoreMigrationService
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
}

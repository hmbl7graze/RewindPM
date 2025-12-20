using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RewindPM.Application.Read.Repositories;
using RewindPM.Infrastructure.Read.Services;
using RewindPM.Infrastructure.Read.SQLite.Persistence;
using RewindPM.Infrastructure.Read.SQLite.Repositories;
using RewindPM.Infrastructure.Read.SQLite.Services;

namespace RewindPM.Infrastructure.Read.SQLite;

/// <summary>
/// Infrastructure.Read.SQLite層のサービスをDIコンテナに登録するための拡張メソッド
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// SQLite ReadModelの実装をDIコンテナに登録する
    /// </summary>
    /// <param name="services">サービスコレクション</param>
    /// <param name="connectionString">ReadModelデータベースの接続文字列</param>
    /// <returns>サービスコレクション</returns>
    public static IServiceCollection AddInfrastructureReadSQLite(
        this IServiceCollection services,
        string connectionString)
    {
        // ReadModelDbContextの登録
        services.AddDbContext<ReadModelDbContext>(options =>
            options.UseSqlite(connectionString));

        // IReadModelRepositoryの実装としてReadModelRepositoryを登録（スコープド：DbContextを使用するため）
        services.AddScoped<IReadModelRepository, ReadModelRepository>();

        // IProjectStatisticsRepositoryの実装としてProjectStatisticsRepositoryを登録
        services.AddScoped<IProjectStatisticsRepository, ProjectStatisticsRepository>();

        // IReadModelRebuildServiceの実装としてReadModelRebuildServiceを登録
        services.AddScoped<IReadModelRebuildService, ReadModelRebuildService>();

        // IReadModelMigrationServiceの実装としてReadModelMigrationServiceを登録
        services.AddScoped<IReadModelMigrationService, ReadModelMigrationService>();

        return services;
    }
}

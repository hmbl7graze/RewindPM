using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RewindPM.Application.Read.Repositories;
using RewindPM.Infrastructure.Read.Persistence;
using RewindPM.Infrastructure.Read.Repositories;

namespace RewindPM.Infrastructure.Read;

/// <summary>
/// Infrastructure.Read層のサービスをDIコンテナに登録するための拡張メソッド
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Infrastructure.Read層のサービスをDIコンテナに登録する
    /// </summary>
    /// <param name="services">サービスコレクション</param>
    /// <param name="connectionString">ReadModelデータベースの接続文字列</param>
    /// <returns>サービスコレクション</returns>
    public static IServiceCollection AddInfrastructureRead(
        this IServiceCollection services,
        string connectionString)
    {
        // ReadModelDbContextの登録
        services.AddDbContext<ReadModelDbContext>(options =>
            options.UseSqlite(connectionString));

        // IReadModelRepositoryの実装としてReadModelRepositoryを登録（スコープド：DbContextを使用するため）
        services.AddScoped<IReadModelRepository, ReadModelRepository>();

        return services;
    }
}

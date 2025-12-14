using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RewindPM.Application.Read.Repositories;
using RewindPM.Infrastructure.Read.Configuration;
using RewindPM.Infrastructure.Read.Persistence;
using RewindPM.Infrastructure.Read.Repositories;
using RewindPM.Infrastructure.Read.Services;

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
    /// <param name="configuration">アプリケーション設定</param>
    /// <returns>サービスコレクション</returns>
    public static IServiceCollection AddInfrastructureRead(
        this IServiceCollection services,
        string connectionString,
        IConfiguration configuration)
    {
        // TimeZone設定を登録
        services.Configure<TimeZoneSettings>(
            configuration.GetSection(TimeZoneSettings.SectionName));

        // TimeZoneServiceをシングルトンとして登録
        services.AddSingleton<ITimeZoneService, TimeZoneService>();

        // ReadModelDbContextの登録
        services.AddDbContext<ReadModelDbContext>(options =>
            options.UseSqlite(connectionString));

        // IReadModelRepositoryの実装としてReadModelRepositoryを登録（スコープド：DbContextを使用するため）
        services.AddScoped<IReadModelRepository, ReadModelRepository>();

        // IProjectStatisticsRepositoryの実装としてProjectStatisticsRepositoryを登録
        services.AddScoped<IProjectStatisticsRepository, ProjectStatisticsRepository>();

        return services;
    }
}

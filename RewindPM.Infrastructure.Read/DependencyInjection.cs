using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RewindPM.Infrastructure.Read.Configuration;
using RewindPM.Infrastructure.Read.Services;

namespace RewindPM.Infrastructure.Read;

/// <summary>
/// Infrastructure.Read層のサービスをDIコンテナに登録するための拡張メソッド
/// DB非依存の共通サービスを登録
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Infrastructure.Read層の共通サービスをDIコンテナに登録する
    /// </summary>
    /// <param name="services">サービスコレクション</param>
    /// <param name="configuration">アプリケーション設定</param>
    /// <returns>サービスコレクション</returns>
    public static IServiceCollection AddInfrastructureRead(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // TimeZone設定を登録
        services.Configure<TimeZoneSettings>(
            configuration.GetSection(TimeZoneSettings.SectionName));

        // TimeZoneServiceをシングルトンとして登録
        services.AddSingleton<ITimeZoneService, TimeZoneService>();

        return services;
    }
}

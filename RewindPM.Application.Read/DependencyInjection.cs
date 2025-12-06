using Microsoft.Extensions.DependencyInjection;

namespace RewindPM.Application.Read;

/// <summary>
/// Application.Read層のDI設定
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Application.Read層のサービスをDIコンテナに登録
    /// </summary>
    public static IServiceCollection AddApplicationRead(this IServiceCollection services)
    {
        // MediatRの登録（QueryHandlerを自動登録）
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
        });

        return services;
    }
}

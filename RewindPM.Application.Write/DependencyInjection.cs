using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using RewindPM.Application.Write.Behaviors;

namespace RewindPM.Application.Write;

/// <summary>
/// Application.Write層の依存性注入設定
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Application.Write層のサービスを登録する
    /// </summary>
    /// <param name="services">サービスコレクション</param>
    /// <returns>サービスコレクション</returns>
    public static IServiceCollection AddApplicationWrite(this IServiceCollection services)
    {
        // MediatRの登録
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);

            // ValidationBehaviorをパイプラインに追加
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // FluentValidationの登録
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}

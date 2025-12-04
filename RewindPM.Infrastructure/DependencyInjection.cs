using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RewindPM.Domain.Common;
using RewindPM.Infrastructure.EventStore;
using RewindPM.Infrastructure.Persistence;
using RewindPM.Infrastructure.Serialization;

namespace RewindPM.Infrastructure;

/// <summary>
/// Infrastructure層のサービスをDIコンテナに登録するための拡張メソッド
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Infrastructure層のサービスをDIコンテナに登録する
    /// </summary>
    /// <param name="services">サービスコレクション</param>
    /// <param name="connectionString">EventStoreデータベースの接続文字列</param>
    /// <returns>サービスコレクション</returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        // EventStoreDbContextの登録
        services.AddDbContext<EventStoreDbContext>(options =>
            options.UseSqlite(connectionString));

        // DomainEventSerializerの登録（シングルトン：ステートレスなため）
        services.AddSingleton<DomainEventSerializer>();

        // IEventStoreの実装としてSqliteEventStoreを登録（スコープド：DbContextを使用するため）
        services.AddScoped<IEventStore, SqliteEventStore>();

        return services;
    }
}

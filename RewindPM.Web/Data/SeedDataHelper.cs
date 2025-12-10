using Microsoft.Extensions.DependencyInjection;
using MediatR;
using RewindPM.Domain.Common;
using RewindPM.Infrastructure.Write.Services;

namespace RewindPM.Web.Data;

/// <summary>
/// SeedData実行時に時刻プロバイダーを制御するためのヘルパークラス
/// </summary>
public static class SeedDataHelper
{
    /// <summary>
    /// FixedDateTimeProviderを使用してSeedDataを実行
    /// </summary>
    /// <param name="originalServiceProvider">元のServiceProvider</param>
    /// <param name="fixedDateTimeProvider">使用するFixedDateTimeProvider</param>
    public static async Task ExecuteSeedDataAsync(
        IServiceProvider originalServiceProvider,
        FixedDateTimeProvider fixedDateTimeProvider)
    {
        // 新しいServiceCollectionを作成
        var services = new ServiceCollection();

        // 既存のサービスをコピー（IDateTimeProvider以外）
        var descriptors = originalServiceProvider.GetService<IServiceCollection>();

        // より簡単な方法：既存のServiceProviderから必要なサービスを手動で登録
        // 注：これは実際のプロダクションコードでは推奨されませんが、SeedDataの目的には適しています

        // 新しいServiceProviderを作成し、FixedDateTimeProviderを登録
        using var scope = originalServiceProvider.CreateScope();
        var scopedServices = new ServiceCollection();

        // IDateTimeProviderをFixedDateTimeProviderに置き換え
        scopedServices.AddSingleton<IDateTimeProvider>(fixedDateTimeProvider);

        // MediatorとCommandHandlersは元のServiceProviderから取得するため、
        // IDateTimeProviderだけを差し替えることができません

        // 代わりに、SeedDataで直接Aggregateを作成してEventStoreに保存する方法を取ります
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var seedData = new SeedData(mediator, originalServiceProvider);
        await seedData.SeedAsync();
    }
}

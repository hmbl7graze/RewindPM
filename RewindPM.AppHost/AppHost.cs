using Microsoft.Extensions.DependencyInjection;

var builder = DistributedApplication.CreateBuilder(args);

var webfrontend = builder.AddProject<Projects.RewindPM_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health");

builder.Eventing.Subscribe<Aspire.Hosting.ApplicationModel.ResourceEndpointsAllocatedEvent>((@event, ct) =>
{
    // webfrontendリソースのエンドポイントが割り当てられたときに実行
    if (@event.Resource.Name == "webfrontend")
    {
        // バックグラウンドでブラウザを開く
        _ = Task.Run(async () =>
        {
            try
            {
                // ResourceNotificationServiceを取得
                var notificationService = @event.Services.GetRequiredService<Aspire.Hosting.ApplicationModel.ResourceNotificationService>();
                
                // リソースが起動するまで待機
                await notificationService.WaitForResourceAsync(@event.Resource.Name, Aspire.Hosting.ApplicationModel.KnownResourceStates.Running, ct);
                
                // 少し余分に待機
                await Task.Delay(1000, ct);
                
                // リソースの状態を監視してURLを取得
                await foreach (var resourceEvent in notificationService.WatchAsync(ct))
                {
                    if (resourceEvent.Resource.Name == "webfrontend" && 
                        resourceEvent.Snapshot.Urls.Length > 0)
                    {
                        var url = resourceEvent.Snapshot.Urls[0].Url;
                        Console.WriteLine($"[AppHost] ブラウザを開きます: {url}");
                        
                        var psi = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = url,
                            UseShellExecute = true
                        };
                        System.Diagnostics.Process.Start(psi);
                        break; // 一度だけ実行
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AppHost] ブラウザの起動に失敗しました: {ex.Message}");
            }
        }, ct);
    }
    
    return Task.CompletedTask;
});

builder.Build().Run();

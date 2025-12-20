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
        // バックグラウンドでブラウザを開く（イベントハンドラーのキャンセルトークンとは独立して実行）
        _ = Task.Run(async () =>
        {
            try
            {
                // ブラウザ起動用のタイムアウト付きキャンセレーショントークンを作成
                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
                var launchToken = cts.Token;
                
                // ResourceNotificationServiceを取得
                var notificationService = @event.Services.GetRequiredService<Aspire.Hosting.ApplicationModel.ResourceNotificationService>();
                
                // リソースが起動するまで待機
                await notificationService.WaitForResourceAsync(@event.Resource.Name, Aspire.Hosting.ApplicationModel.KnownResourceStates.Running, launchToken);
                
                // 少し余分に待機
                await Task.Delay(1000, launchToken);
                
                // リソースの状態を監視してURLを取得
                await foreach (var resourceEvent in notificationService.WatchAsync(launchToken))
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
                        var process = System.Diagnostics.Process.Start(psi);
                        if (process == null)
                        {
                            Console.WriteLine("[AppHost] ブラウザの起動に失敗しました: Process.Startがnullを返しました");
                        }
                        break; // 一度だけ実行
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[AppHost] ブラウザの起動がタイムアウトしました");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AppHost] ブラウザの起動に失敗しました: {ex.Message}");
            }
        });
    }
    
    return Task.CompletedTask;
});

builder.Build().Run();

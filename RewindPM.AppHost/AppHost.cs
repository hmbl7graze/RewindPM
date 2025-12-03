var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.RewindPM_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.RewindPM_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();

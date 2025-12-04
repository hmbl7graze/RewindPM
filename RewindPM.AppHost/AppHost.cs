var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.RewindPM_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health");

builder.Build().Run();

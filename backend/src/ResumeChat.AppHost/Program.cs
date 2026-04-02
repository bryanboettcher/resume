var builder = DistributedApplication.CreateBuilder(args);

var qdrant = builder.AddQdrant("qdrant")
    .WithDataVolume("resumechat-qdrant");

var api = builder.AddProject<Projects.ResumeChat_Api>("api")
    .WithReference(qdrant)
    .WaitFor(qdrant);

builder.Build().Run();

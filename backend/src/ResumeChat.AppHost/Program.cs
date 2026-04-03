var builder = DistributedApplication.CreateBuilder(args);

var qdrant = builder.AddQdrant("qdrant")
    .WithDataVolume("resumechat-qdrant");

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("resumechat-pgdata");
var db = postgres.AddDatabase("resumechat");

var api = builder.AddProject<Projects.ResumeChat_Api>("api")
    .WithReference(qdrant)
    .WithReference(db)
    .WaitFor(qdrant)
    .WaitFor(postgres);

builder.Build().Run();

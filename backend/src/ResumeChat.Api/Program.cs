using ResumeChat.Api.Endpoints;
using ResumeChat.Api.Extensions;
using ResumeChat.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddApplicationServices();

var app = builder.Build();

app.UseRateLimiter();
app.UseMiddleware<ApiKeyMiddleware>();
app.MapApplicationEndpoints();
app.MapDefaultEndpoints();

app.Run();

public partial class Program;

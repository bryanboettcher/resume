using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using ResumeChat.Rag;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ICompletionProvider, HardcodedCompletionProvider>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("chat", limiter =>
    {
        limiter.PermitLimit = 10;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });
});

var app = builder.Build();

app.UseRateLimiter();

app.Use(async (context, next) =>
{
    if (!context.Request.Path.StartsWithSegments("/api"))
    {
        await next();
        return;
    }

    // Health endpoint is unauthenticated
    if (context.Request.Path.Equals("/api/chat/health"))
    {
        await next();
        return;
    }

    var configuredKey = app.Configuration["ApiKey"];
    if (string.IsNullOrEmpty(configuredKey))
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        return;
    }

    var providedKey = context.Request.Headers["X-Api-Key"].FirstOrDefault();
    if (providedKey != configuredKey)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return;
    }

    await next();
});

app.MapGet("/api/chat/health", () => Results.Ok(new { status = "healthy" }));

app.MapPost("/api/chat", async (ChatRequest request, ICompletionProvider provider, HttpContext context, CancellationToken ct) =>
{
    context.Response.ContentType = "text/event-stream";

    await foreach (var chunk in provider.CompleteAsync(request.Message, ct))
    {
        await context.Response.WriteAsync($"data: {chunk}\n\n", ct);
        await context.Response.Body.FlushAsync(ct);
    }

    await context.Response.WriteAsync("data: [DONE]\n\n", ct);
    await context.Response.Body.FlushAsync(ct);
}).RequireRateLimiting("chat");

app.Run();

public record ChatRequest(string Message);

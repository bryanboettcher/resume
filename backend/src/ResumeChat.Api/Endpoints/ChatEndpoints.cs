using ResumeChat.Rag;

namespace ResumeChat.Api.Endpoints;

public static class ChatEndpoints
{
    public static void MapTo(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/chat/health", HandleHealth)
            .Produces(StatusCodes.Status200OK);

        app.MapPost("/api/chat", HandleChat)
            .RequireRateLimiting("chat")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status429TooManyRequests);
    }

    private static IResult HandleHealth() => Results.Ok(new { status = "healthy" });

    private static async Task HandleChat(
        ChatRequest request,
        ICompletionProvider provider,
        HttpContext context,
        CancellationToken ct)
    {
        context.Response.ContentType = "text/event-stream";

        await foreach (var chunk in provider.CompleteAsync(request.Message, ct).ConfigureAwait(false))
        {
            await context.Response.WriteAsync($"data: {chunk}\n\n", ct).ConfigureAwait(false);
            await context.Response.Body.FlushAsync(ct).ConfigureAwait(false);
        }

        await context.Response.WriteAsync("data: [DONE]\n\n", ct).ConfigureAwait(false);
        await context.Response.Body.FlushAsync(ct).ConfigureAwait(false);
    }
}

public record ChatRequest(string Message);

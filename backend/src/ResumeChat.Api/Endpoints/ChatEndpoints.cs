using ResumeChat.Rag;
using ResumeChat.Rag.Models;
using ResumeChat.Rag.Retrieval;

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

    private static async Task<IResult> HandleChat(
        ChatRequest request,
        IRetrievalProvider retrieval,
        ICompletionProvider completion,
        HttpContext context,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return Results.BadRequest("Message is required.");

        if (request.Message.Length > 2048)
            return Results.BadRequest("Message must be 2048 characters or fewer.");

        var relevantContext = await retrieval.RetrieveAsync(request.Message, topK: 5, ct).ConfigureAwait(false);
        var completionRequest = new CompletionRequest(request.Message, relevantContext);

        context.Response.ContentType = "text/event-stream";

        await foreach (var chunk in completion.CompleteAsync(completionRequest, ct).ConfigureAwait(false))
        {
            var escaped = chunk.Replace("\n", "\ndata: ");
            await context.Response.WriteAsync($"data: {escaped}\n\n", ct).ConfigureAwait(false);
            await context.Response.Body.FlushAsync(ct).ConfigureAwait(false);
        }

        await context.Response.WriteAsync("data: [DONE]\n\n", ct).ConfigureAwait(false);
        await context.Response.Body.FlushAsync(ct).ConfigureAwait(false);
        return Results.Empty;
    }
}

public record ChatRequest(string Message);

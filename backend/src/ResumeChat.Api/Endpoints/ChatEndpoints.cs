using System.Runtime.CompilerServices;
using ResumeChat.Api.Validation;
using ResumeChat.Rag;
using ResumeChat.Rag.Classification;
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
            .AddEndpointFilter<ValidationFilter<ChatRequest>>()
            .RequireRateLimiting("chat")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status429TooManyRequests);
    }

    private static IResult HandleHealth() => Results.Ok(new { status = "healthy" });

    private static async Task<IResult> HandleChat(
        ChatRequest request,
        IThreatClassifier classifier,
        IRetrievalProvider retrieval,
        ICompletionProvider completion,
        HttpContext context,
        ILogger<ChatRequest> logger,
        CancellationToken ct)
    {
        using var activity = RagDiagnostics.ActivitySource.StartActivity("chat.request");
        RagDiagnostics.ChatRequests.Add(1);

        activity?.SetTag("chat.message_length", request.Message.Length);
        logger.LogInformation("Chat request received ({MessageLength} chars)", request.Message.Length);

        try
        {
            var threat = await classifier.ClassifyAsync(request.Message, ct).ConfigureAwait(false);

            context.Response.Headers["X-Threat-Score"] = threat.ThreatScore.ToString();

            if (threat.IsThreat)
                return await StreamSse(SingleChunk(ChatResponses.Unrelated, ct));

            var relevantContext = await retrieval.RetrieveAsync(request.Message, topK: 5, ct).ConfigureAwait(false);
            var completionRequest = new CompletionRequest(request.Message, relevantContext);

            activity?.SetTag("chat.context_count", relevantContext.Count);

            return await StreamSse(completion.CompleteAsync(completionRequest, ct));

            async Task<IResult> StreamSse(IAsyncEnumerable<string> chunks)
            {
                context.Response.ContentType = "text/event-stream";

                await foreach (var chunk in chunks.ConfigureAwait(false))
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
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            RagDiagnostics.ChatErrors.Add(1);
            activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }

    private static async IAsyncEnumerable<string> SingleChunk(
        string value, [EnumeratorCancellation] CancellationToken ct = default)
    {
        await Task.CompletedTask;
        ct.ThrowIfCancellationRequested();
        yield return value;
    }
}

public record ChatRequest(string Message);

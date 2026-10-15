using System.Runtime.CompilerServices;
using ResumeChat.Api.Extensions;
using ResumeChat.Api.Validation;
using ResumeChat.Rag;
using ResumeChat.Rag.Completion;
using ResumeChat.Rag.Models;
using ResumeChat.Rag.Orchestration;
using ResumeChat.Rag.Response;

namespace ResumeChat.Api.Endpoints;

public static class ChatEndpoints
{
    public static void MapTo(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/chat/health", HandleHealth)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status503ServiceUnavailable);

        app.MapPost("/api/chat", HandleChat)
            .AddEndpointFilter<ValidationFilter<ChatRequest>>()
            .RequireRateLimiting("chat")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status429TooManyRequests);
    }

    private static IResult HandleHealth(IResponseProvider responseProvider, CompletionHealth health)
    {
        var meta = responseProvider as ICompletionMetadata;
        var snapshot = health.Snapshot();
        var response = new
        {
            status = snapshot.Healthy ? "healthy" : "unhealthy",
            provider = meta?.Provider,
            model = meta?.Model,
            lastSuccess = snapshot.LastSuccess,
            lastFailure = snapshot.LastFailure,
            lastError = snapshot.LastError
        };

        return snapshot.Healthy
            ? Results.Ok(response)
            : Results.Json(response, statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    private static async Task<IResult> HandleChat(
        ChatRequest request,
        IChatOrchestrator orchestrator,
        HttpContext context,
        ILogger<ChatRequest> logger,
        CancellationToken ct)
    {
        using var activity = RagDiagnostics.ActivitySource.StartActivity("chat.request");
        RagDiagnostics.ChatRequests.Add(1);

        activity?.SetTag("chat.message_length", request.Message.Length);
        activity?.SetTag("chat.user_message", request.Message);
        logger.LogInformation("Chat request: {UserMessage}", request.Message);

        try
        {
            var result = await orchestrator.ProcessChatAsync(request, ct);

            context.Response.Headers["X-Threat-Score"] = result.ThreatScore.ToString();

            if (result.CacheHit)
                context.Response.Headers["X-Cache-Hit"] = "true";

            if (result.IsThreat)
            {
                await SingleChunk(ChatResponses.Unrelated, ct)
                    .StreamAsSseAsync(context, cancellationToken: ct);
                return Results.Empty;
            }

            await result.Tokens.StreamAsSseAsync(context, onComplete: fullResponse =>
            {
                activity?.SetTag("chat.response_length", fullResponse.Length);
                activity?.SetTag("chat.response_preview", fullResponse.Length > 500
                    ? fullResponse[..500] + "..."
                    : fullResponse);
                logger.LogInformation("Chat response ({ResponseLength} chars): {ResponsePreview}",
                    fullResponse.Length,
                    fullResponse.Length > 200 ? fullResponse[..200] + "..." : fullResponse);
            }, cancellationToken: ct);

            return Results.Empty;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            RagDiagnostics.ChatErrors.Add(1);
            activity?.SetStatus(System.Diagnostics.ActivityStatusCode.Error, ex.Message);
            logger.LogError(ex, "Chat request failed: {Reason}", ex.Message);

            if (context.Response.HasStarted)
                return Results.Empty;

            return ex is CompletionUnavailableException
                ? Results.Problem(title: "Completion provider unavailable", statusCode: StatusCodes.Status503ServiceUnavailable)
                : Results.Problem(title: "Chat request failed", statusCode: StatusCodes.Status500InternalServerError);
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

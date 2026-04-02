using Microsoft.Extensions.Options;
using ResumeChat.Api.Options;
using ResumeChat.Rag.Ingestion;

namespace ResumeChat.Api.Endpoints;

public static class IngestionEndpoints
{
    public static void MapTo(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/admin/ingest", HandleIngest)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        app.MapGet("/api/admin/ingest/status", HandleStatus)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> HandleIngest(
        IngestionService ingestion,
        IOptions<CorpusOptions> corpusOptions,
        HttpContext context,
        CancellationToken ct)
    {
        var corpusDir = corpusOptions.Value.Directory;
        if (!Directory.Exists(corpusDir))
            return Results.Problem($"Corpus directory not found: {corpusDir}", statusCode: 500);

        context.Response.ContentType = "text/event-stream";

        try
        {
            await foreach (var progress in ingestion.IngestCorpusAsync(corpusDir, ct).ConfigureAwait(false))
            {
                await context.Response.WriteAsync(
                    $"data: [{progress.ChunksProcessed}] {progress.Status}\n\n", ct).ConfigureAwait(false);
                await context.Response.Body.FlushAsync(ct).ConfigureAwait(false);
            }

            await context.Response.WriteAsync("data: [DONE]\n\n", ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            await context.Response.WriteAsync("data: [CANCELLED]\n\n", CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await context.Response.WriteAsync($"data: [ERROR] {ex.Message}\n\n", CancellationToken.None).ConfigureAwait(false);
        }

        await context.Response.Body.FlushAsync(CancellationToken.None).ConfigureAwait(false);
        return Results.Empty;
    }

    private static IResult HandleStatus()
    {
        // Placeholder — could track last ingestion time, chunk count, etc.
        return Results.Ok(new { status = "ready" });
    }
}

using Microsoft.Extensions.Options;
using ResumeChat.Api.Options;
using ResumeChat.Storage.Services;

namespace ResumeChat.Api.Endpoints;

public static class CorpusSyncEndpoints
{
    public static void MapTo(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/admin/sync", HandleSync)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> HandleSync(
        CorpusSyncService syncService,
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
            await foreach (var progress in syncService.SyncAsync(corpusDir, ct).ConfigureAwait(false))
            {
                var status = progress.Skipped ? "skipped" : "synced";
                await context.Response.WriteAsync(
                    $"data: [{status}] {progress.SourceFile} ({progress.ChunkCount} chunks)\n\n", ct).ConfigureAwait(false);
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
}

using Microsoft.Extensions.Options;
using ResumeChat.Api.Options;
using ResumeChat.Rag.Chunking;
using ResumeChat.Rag.Embedding;
using ResumeChat.Rag.Models;
using ResumeChat.Rag.VectorStore;
using ResumeChat.Storage.Repositories;
using ResumeChat.Storage.Services;

namespace ResumeChat.Api.Endpoints;

public static class CorpusSyncEndpoints
{
    public static void MapTo(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/admin/sync", HandleSync)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        app.MapPost("/api/admin/corpus", HandleUpload)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        app.MapGet("/api/admin/corpus", HandleList)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        app.MapGet("/api/admin/corpus/{id:long}", HandleGetDocument)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);
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

    private sealed record CorpusUploadRequest(string SourcePath, string Content, bool Embed = true);

    private static async Task<IResult> HandleUpload(
        CorpusUploadRequest request,
        CorpusSyncService syncService,
        IChunkingStrategy chunker,
        IEmbeddingProvider embedder,
        IVectorStore vectorStore,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.SourcePath) || string.IsNullOrWhiteSpace(request.Content))
            return Results.BadRequest("sourcePath and content are required");

        var result = await syncService.UpsertDocumentAsync(request.SourcePath, request.Content, ct).ConfigureAwait(false);

        var embedded = 0;
        if (request.Embed && !result.Skipped)
        {
            var metadata = new DocumentMetadata(request.SourcePath, null, []);
            var chunks = chunker.Chunk(request.Content, metadata);

            var probe = await embedder.EmbedAsync("probe", ct).ConfigureAwait(false);
            await vectorStore.EnsureCollectionAsync(probe.Length, ct).ConfigureAwait(false);

            foreach (var chunk in chunks)
            {
                var embedding = await embedder.EmbedAsync(chunk.Text, ct).ConfigureAwait(false);
                var embeddedChunk = new EmbeddedChunk(chunk, embedding);
                await vectorStore.UpsertAsync(embeddedChunk, ct).ConfigureAwait(false);
                embedded++;
            }
        }

        return Results.Ok(new
        {
            result.Status,
            result.SourceFile,
            result.Skipped,
            result.ChunkCount,
            embedded
        });
    }

    private static async Task<IResult> HandleList(
        ICorpusRepository repository,
        CancellationToken ct)
    {
        var docs = await repository.GetAllDocumentsAsync(ct).ConfigureAwait(false);

        return Results.Ok(docs.Select(d => new
        {
            d.Id,
            d.SourceFile,
            d.Title,
            tags = d.Tags,
            chunkCount = d.Chunks.Count,
            d.ContentHash,
            d.LastModified
        }));
    }

    private static async Task<IResult> HandleGetDocument(
        long id,
        ICorpusRepository repository,
        CancellationToken ct)
    {
        var docs = await repository.GetAllDocumentsAsync(ct).ConfigureAwait(false);
        var doc = docs.FirstOrDefault(d => d.Id == id);
        if (doc is null) return Results.NotFound();

        return Results.Ok(new
        {
            doc.Id,
            doc.SourceFile,
            doc.Title,
            tags = doc.Tags,
            doc.ContentText,
            doc.ContentHash,
            doc.LastModified,
            chunks = doc.Chunks.Select(c => new
            {
                c.ChunkIndex,
                c.SectionHeading,
                preview = c.ChunkText.Length > 200 ? c.ChunkText[..200] + "..." : c.ChunkText
            })
        });
    }
}

using ResumeChat.Rag.Models;
using ResumeChat.Rag.Pipeline;
using ResumeChat.Rag.Retrieval;

namespace ResumeChat.Api.Endpoints;

public static class DebugRetrievalEndpoints
{
    public static void MapTo(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/debug/retrieval", HandleQuery)
            .Produces(StatusCodes.Status200OK);

        app.MapGet("/api/debug/retrieval/pipeline", HandlePipelineQuery)
            .Produces(StatusCodes.Status200OK);
    }

    private static async Task<IResult> HandleQuery(
        string query,
        IRetrievalProvider retrieval,
        int topK = 5,
        int? dimensions = null,
        CancellationToken ct = default)
    {
        var request = new RetrievalRequest(query, topK, dimensions);
        var results = await retrieval.RetrieveAsync(request, ct);

        return Results.Ok(new
        {
            query,
            topK,
            dimensions,
            results = FormatResults(results)
        });
    }

    private static async Task<IResult> HandlePipelineQuery(
        string query,
        IQueryTransformer transformer,
        CancellationToken ct = default)
    {
        var payload = await transformer.TransformAsync(new ChatRequest(query), ct);

        return Results.Ok(new
        {
            query,
            processedQuery = payload.ProcessedMessage,
            documentCount = payload.Documents.Count,
            results = FormatResults(payload.Documents)
        });
    }

    private static object FormatResults(IReadOnlyList<ScoredChunk> results) =>
        results.Select(r => new
        {
            score = r.Score,
            source = r.Chunk.Metadata.SourceFile,
            section = r.Chunk.SectionHeading,
            tags = r.Chunk.Metadata.Tags,
            preview = r.Chunk.Text.Length > 150 ? r.Chunk.Text[..150] + "..." : r.Chunk.Text
        });
}

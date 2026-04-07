using Microsoft.Extensions.Options;
using ResumeChat.Api.Options;
using ResumeChat.Rag.Ingestion;
using ResumeChat.Rag.Pipeline;
using ResumeChat.Rag.VectorStore;

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
        IIngestionPipeline pipeline,
        IOptions<CorpusOptions> corpusOptions,
        HttpContext context,
        CancellationToken ct)
    {
        var corpusDir = corpusOptions.Value.Directory;
        var isDbBacked = pipeline is not Rag.Ingestion.CorpusIngestionPipeline;
        if (!isDbBacked && !Directory.Exists(corpusDir))
            return Results.Problem($"Corpus directory not found: {corpusDir}", statusCode: 500);

        context.Response.ContentType = "text/event-stream";

        try
        {
            await foreach (var progress in ingestion.IngestCorpusAsync(corpusDir, ct))
            {
                await context.Response.WriteAsync(
                    $"data: [{progress.ChunksProcessed}] {progress.Status}\n\n", ct);
                await context.Response.Body.FlushAsync(ct);
            }

            await context.Response.WriteAsync("data: [DONE]\n\n", ct);
        }
        catch (OperationCanceledException)
        {
            await context.Response.WriteAsync("data: [CANCELLED]\n\n", CancellationToken.None);
        }
        catch (Exception ex)
        {
            await context.Response.WriteAsync($"data: [ERROR] {ex.Message}\n\n", CancellationToken.None);
        }

        await context.Response.Body.FlushAsync(CancellationToken.None);
        return Results.Empty;
    }

    private static async Task<IResult> HandleStatus(
        IVectorStore vectorStore,
        IOptions<CorpusOptions> corpusOptions,
        IOptions<RetrievalOptions> retrievalOptions,
        IConfiguration configuration,
        CancellationToken ct)
    {
        var collection = await vectorStore.GetCollectionInfoAsync(ct);

        return Results.Ok(new
        {
            corpus = new
            {
                directory = corpusOptions.Value.Directory,
                directoryExists = Directory.Exists(corpusOptions.Value.Directory)
            },
            vectorStore = new
            {
                collection = collection.Name,
                pointCount = collection.PointCount,
                vectorDimensions = collection.VectorSize
            },
            pipeline = new
            {
                completionProvider = configuration["Completion:Provider"] ?? "Hardcoded",
                guardProvider = configuration["Guard:Provider"] ?? "Passthrough",
                retrieval = new
                {
                    dimensions = retrievalOptions.Value.Dimensions,
                    topK = retrievalOptions.Value.TopK
                }
            }
        });
    }
}

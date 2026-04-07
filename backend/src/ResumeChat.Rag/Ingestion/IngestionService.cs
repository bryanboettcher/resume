using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using ResumeChat.Rag.Embedding;
using ResumeChat.Rag.VectorStore;

namespace ResumeChat.Rag.Ingestion;

public sealed record IngestionProgress(string Status, int ChunksProcessed, string? CurrentFile = null);

public sealed class IngestionService
{
    private readonly IIngestionPipeline _pipeline;
    private readonly IVectorStore _vectorStore;
    private readonly IEmbeddingProvider _embedder;
    private readonly ILogger<IngestionService> _logger;

    public IngestionService(
        IIngestionPipeline pipeline,
        IVectorStore vectorStore,
        IEmbeddingProvider embedder,
        ILogger<IngestionService> logger)
    {
        _pipeline = pipeline;
        _vectorStore = vectorStore;
        _embedder = embedder;
        _logger = logger;
    }

    public async IAsyncEnumerable<IngestionProgress> IngestCorpusAsync(
        string corpusDirectory,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var activity = RagDiagnostics.ActivitySource.StartActivity("rag.ingest");
        activity?.SetTag("rag.ingest.corpus_directory", corpusDirectory);
        RagDiagnostics.IngestionInProgress.Add(1);

        try
        {
            yield return new IngestionProgress("Detecting embedding dimensions", 0);

            var probe = await _embedder.EmbedAsync("probe", cancellationToken);
            var vectorSize = probe.Length;

            _logger.LogInformation("Embedding dimensions: {VectorSize}", vectorSize);
            yield return new IngestionProgress($"Ensuring Qdrant collection (vector size: {vectorSize})", 0);

            await _vectorStore.EnsureCollectionAsync(vectorSize, cancellationToken);

            yield return new IngestionProgress("Starting ingestion", 0);

            var count = 0;
            string? lastFile = null;

            await foreach (var embedded in _pipeline.IngestAsync(corpusDirectory, cancellationToken)
                               )
            {
                await _vectorStore.UpsertAsync(embedded, cancellationToken);
                count++;
                RagDiagnostics.IngestionChunks.Add(1);

                var currentFile = embedded.Chunk.Metadata.SourceFile;
                if (currentFile != lastFile)
                {
                    yield return new IngestionProgress($"Processing {currentFile}", count);
                    lastFile = currentFile;
                }
            }

            activity?.SetTag("rag.ingest.total_chunks", count);
            _logger.LogInformation("Ingestion complete: {TotalChunks} chunks upserted", count);
            yield return new IngestionProgress("Ingestion complete", count);
        }
        finally
        {
            RagDiagnostics.IngestionInProgress.Add(-1);
        }
    }
}

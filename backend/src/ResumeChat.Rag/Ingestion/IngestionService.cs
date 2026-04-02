using System.Runtime.CompilerServices;
using ResumeChat.Rag.Embedding;
using ResumeChat.Rag.VectorStore;

namespace ResumeChat.Rag.Ingestion;

public sealed record IngestionProgress(string Status, int ChunksProcessed, string? CurrentFile = null);

public sealed class IngestionService
{
    private readonly IIngestionPipeline _pipeline;
    private readonly IVectorStore _vectorStore;
    private readonly IEmbeddingProvider _embedder;

    public IngestionService(IIngestionPipeline pipeline, IVectorStore vectorStore, IEmbeddingProvider embedder)
    {
        _pipeline = pipeline;
        _vectorStore = vectorStore;
        _embedder = embedder;
    }

    public async IAsyncEnumerable<IngestionProgress> IngestCorpusAsync(
        string corpusDirectory,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return new IngestionProgress("Detecting embedding dimensions", 0);

        var probe = await _embedder.EmbedAsync("probe", cancellationToken).ConfigureAwait(false);
        var vectorSize = probe.Length;

        yield return new IngestionProgress($"Ensuring Qdrant collection (vector size: {vectorSize})", 0);

        await _vectorStore.EnsureCollectionAsync(vectorSize, cancellationToken).ConfigureAwait(false);

        yield return new IngestionProgress("Starting ingestion", 0);

        var count = 0;
        string? lastFile = null;

        await foreach (var embedded in _pipeline.IngestAsync(corpusDirectory, cancellationToken).ConfigureAwait(false))
        {
            await _vectorStore.UpsertAsync(embedded, cancellationToken).ConfigureAwait(false);
            count++;

            var currentFile = embedded.Chunk.Metadata.SourceFile;
            if (currentFile != lastFile)
            {
                yield return new IngestionProgress($"Processing {currentFile}", count);
                lastFile = currentFile;
            }
        }

        yield return new IngestionProgress("Ingestion complete", count);
    }
}

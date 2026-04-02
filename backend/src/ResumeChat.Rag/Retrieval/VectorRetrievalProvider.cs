using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ResumeChat.Rag.Embedding;
using ResumeChat.Rag.Models;
using ResumeChat.Rag.VectorStore;

namespace ResumeChat.Rag.Retrieval;

public sealed class VectorRetrievalProvider : IRetrievalProvider
{
    private readonly IEmbeddingProvider _embedder;
    private readonly IVectorStore _vectorStore;
    private readonly ILogger<VectorRetrievalProvider> _logger;

    public VectorRetrievalProvider(
        IEmbeddingProvider embedder,
        IVectorStore vectorStore,
        ILogger<VectorRetrievalProvider> logger)
    {
        _embedder = embedder;
        _vectorStore = vectorStore;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ScoredChunk>> RetrieveAsync(
        string query,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        using var activity = RagDiagnostics.ActivitySource.StartActivity("rag.retrieve");
        activity?.SetTag("rag.query_length", query.Length);
        activity?.SetTag("rag.top_k", topK);

        var startTimestamp = Stopwatch.GetTimestamp();

        var queryEmbedding = await _embedder.EmbedAsync(query, cancellationToken).ConfigureAwait(false);
        var results = await _vectorStore.SearchAsync(queryEmbedding, topK, cancellationToken).ConfigureAwait(false);

        var elapsedMs = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
        activity?.SetTag("rag.result_count", results.Count);

        RagDiagnostics.RetrievalDuration.Record(elapsedMs);
        RagDiagnostics.RetrievalResultCount.Record(results.Count);
        if (results.Count > 0)
            RagDiagnostics.TopRetrievalScore.Record(results[0].Score);

        _logger.LogInformation("Retrieved {ResultCount} chunks for query ({QueryLength} chars) in {ElapsedMs:F1}ms",
            results.Count, query.Length, elapsedMs);

        return results;
    }
}

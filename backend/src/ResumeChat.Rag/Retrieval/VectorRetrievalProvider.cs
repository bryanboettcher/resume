using ResumeChat.Rag.Embedding;
using ResumeChat.Rag.Models;
using ResumeChat.Rag.VectorStore;

namespace ResumeChat.Rag.Retrieval;

public sealed class VectorRetrievalProvider : IRetrievalProvider
{
    private readonly IEmbeddingProvider _embedder;
    private readonly IVectorStore _vectorStore;

    public VectorRetrievalProvider(IEmbeddingProvider embedder, IVectorStore vectorStore)
    {
        _embedder = embedder;
        _vectorStore = vectorStore;
    }

    public async Task<IReadOnlyList<ScoredChunk>> RetrieveAsync(
        string query,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        var queryEmbedding = await _embedder.EmbedAsync(query, cancellationToken).ConfigureAwait(false);
        return await _vectorStore.SearchAsync(queryEmbedding, topK, cancellationToken).ConfigureAwait(false);
    }
}

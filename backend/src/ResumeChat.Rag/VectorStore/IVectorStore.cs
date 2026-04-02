using ResumeChat.Rag.Models;

namespace ResumeChat.Rag.VectorStore;

public interface IVectorStore
{
    Task UpsertAsync(EmbeddedChunk chunk, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ScoredChunk>> SearchAsync(ReadOnlyMemory<float> queryEmbedding, int topK, CancellationToken cancellationToken = default);
    Task EnsureCollectionAsync(int vectorSize, CancellationToken cancellationToken = default);
}

namespace ResumeChat.Rag.Embedding;

public interface IEmbeddingProvider
{
    Task<ReadOnlyMemory<float>> EmbedAsync(string text, CancellationToken cancellationToken = default);
}

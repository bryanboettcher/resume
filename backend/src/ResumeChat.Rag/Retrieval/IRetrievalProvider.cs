using ResumeChat.Rag.Models;

namespace ResumeChat.Rag.Retrieval;

public interface IRetrievalProvider
{
    Task<IReadOnlyList<ScoredChunk>> RetrieveAsync(string query, int topK = 5, CancellationToken cancellationToken = default);
}

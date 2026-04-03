using ResumeChat.Rag.Models;

namespace ResumeChat.Rag.Retrieval;

public interface IRetrievalProvider
{
    Task<IReadOnlyList<ScoredChunk>> RetrieveAsync(RetrievalRequest request, CancellationToken cancellationToken = default);
}

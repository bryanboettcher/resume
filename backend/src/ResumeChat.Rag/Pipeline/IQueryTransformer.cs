using ResumeChat.Rag.Models;

namespace ResumeChat.Rag.Pipeline;

public interface IQueryTransformer
{
    Task<QueryPayload?> TransformAsync(ChatRequest request, CancellationToken cancellationToken = default);
}

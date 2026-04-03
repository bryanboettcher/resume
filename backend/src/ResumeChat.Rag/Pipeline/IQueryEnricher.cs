using ResumeChat.Rag.Models;

namespace ResumeChat.Rag.Pipeline;

public interface IQueryEnricher
{
    int Order { get; }
    Task<ChatQuery> EnrichAsync(ChatQuery query, CancellationToken cancellationToken = default);
}

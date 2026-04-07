using ResumeChat.Rag.Models;

namespace ResumeChat.Rag.Pipeline;

public interface IQueryEnricher
{
    int Order { get; }
    ValueTask<ChatQuery> EnrichAsync(ChatQuery query, CancellationToken cancellationToken = default);
}

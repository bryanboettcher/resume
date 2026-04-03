using ResumeChat.Rag.Models;

namespace ResumeChat.Rag.Response;

public interface IResponseProvider
{
    IAsyncEnumerable<string> GetResponseAsync(QueryPayload payload, CancellationToken cancellationToken = default);
}

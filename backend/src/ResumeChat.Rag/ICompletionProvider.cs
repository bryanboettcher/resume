using ResumeChat.Rag.Models;

namespace ResumeChat.Rag;

public interface ICompletionProvider
{
    IAsyncEnumerable<string> CompleteAsync(CompletionRequest request, CancellationToken cancellationToken = default);
}

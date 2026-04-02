using ResumeChat.Rag.Models;

namespace ResumeChat.Rag;

public sealed class HardcodedCompletionProvider : ICompletionProvider
{
    public async IAsyncEnumerable<string> CompleteAsync(
        CompletionRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var word in request.UserMessage.Split(' '))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(50, cancellationToken);
            yield return word + " ";
        }
    }
}

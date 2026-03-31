namespace ResumeChat.Rag;

public interface ICompletionProvider
{
    IAsyncEnumerable<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default);
}

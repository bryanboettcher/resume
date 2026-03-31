namespace ResumeChat.Rag;

public class HardcodedCompletionProvider : ICompletionProvider
{
    public async IAsyncEnumerable<string> CompleteAsync(
        string prompt,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var word in prompt.Split(' '))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(50, cancellationToken);
            yield return word + " ";
        }
    }
}

using System.Runtime.CompilerServices;
using ResumeChat.Rag.Models;

namespace ResumeChat.Rag.Response;

public sealed class CannedResponseProvider : IResponseProvider
{
    public async IAsyncEnumerable<string> GetResponseAsync(
        QueryPayload payload,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var word in payload.OriginalMessage.Split(' '))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(50, cancellationToken);
            yield return word + " ";
        }
    }
}

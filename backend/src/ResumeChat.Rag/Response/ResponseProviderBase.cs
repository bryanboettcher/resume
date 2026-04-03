using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using ResumeChat.Rag.Completion;
using ResumeChat.Rag.Models;

namespace ResumeChat.Rag.Response;

public abstract class ResponseProviderBase : IResponseProvider, ICompletionMetadata
{
    private readonly ILogger _logger;

    protected ResponseProviderBase(ILogger logger)
    {
        _logger = logger;
    }

    protected abstract string ProviderName { get; }
    protected abstract string ModelName { get; }

    string ICompletionMetadata.Provider => ProviderName;
    string ICompletionMetadata.Model => ModelName;

    public async IAsyncEnumerable<string> GetResponseAsync(
        QueryPayload payload,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var activity = RagDiagnostics.ActivitySource.StartActivity("rag.response");
        activity?.SetTag("rag.response.model", ModelName);
        activity?.SetTag("rag.response.provider", ProviderName);
        activity?.SetTag("rag.response.context_chunks", payload.Documents.Count);

        var totalStart = Stopwatch.GetTimestamp();
        var firstTokenRecorded = false;

        _logger.LogInformation("Starting {Provider} response with model {Model} ({ContextChunks} context chunks)",
            ProviderName, ModelName, payload.Documents.Count);

        await foreach (var token in StreamTokensAsync(payload, cancellationToken).ConfigureAwait(false))
        {
            if (!firstTokenRecorded)
            {
                RagDiagnostics.CompletionFirstTokenDuration.Record(
                    Stopwatch.GetElapsedTime(totalStart).TotalMilliseconds);
                firstTokenRecorded = true;
            }

            yield return token;
        }

        var totalMs = Stopwatch.GetElapsedTime(totalStart).TotalMilliseconds;
        RagDiagnostics.CompletionTotalDuration.Record(totalMs);
        _logger.LogInformation("Response finished in {ElapsedMs:F1}ms", totalMs);

        activity?.Dispose();
    }

    protected abstract IAsyncEnumerable<string> StreamTokensAsync(
        QueryPayload payload, CancellationToken cancellationToken);
}

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using ResumeChat.Rag.Completion;
using ResumeChat.Rag.Models;

namespace ResumeChat.Rag.Response;

public abstract class ResponseProviderBase : IResponseProvider, ICompletionMetadata
{
    private readonly ILogger _logger;
    private readonly CompletionHealth _health;

    protected ResponseProviderBase(ILogger logger, CompletionHealth health)
    {
        _logger = logger;
        _health = health;
    }

    protected abstract string ProviderName { get; }
    protected abstract string ModelName { get; }

    string ICompletionMetadata.Provider => ProviderName;
    string ICompletionMetadata.Model => ModelName;

    public async IAsyncEnumerable<string> GetResponseAsync(
        QueryPayload payload,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var activity = RagDiagnostics.ActivitySource.StartActivity("rag.response");
        activity?.SetTag("rag.response.model", ModelName);
        activity?.SetTag("rag.response.provider", ProviderName);
        activity?.SetTag("rag.response.context_chunks", payload.Documents.Count);

        await foreach (var token in StreamWithMetrics(payload, cancellationToken))
        {
            yield return token;
        }
    }

    private async IAsyncEnumerable<string> StreamWithMetrics(
        QueryPayload payload,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var totalStart = Stopwatch.GetTimestamp();
        var firstTokenRecorded = false;

        _logger.LogInformation("Starting {Provider} response with model {Model} ({ContextChunks} context chunks)",
            ProviderName, ModelName, payload.Documents.Count);

        await using var tokens = StreamTokensAsync(payload, cancellationToken).GetAsyncEnumerator(cancellationToken);
        while (true)
        {
            string token;
            try
            {
                if (!await tokens.MoveNextAsync())
                    break;
                token = tokens.Current;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _health.RecordFailure(ex);
                throw new CompletionUnavailableException(ProviderName, ModelName, ex);
            }

            if (!firstTokenRecorded)
            {
                RagDiagnostics.CompletionFirstTokenDuration.Record(
                    Stopwatch.GetElapsedTime(totalStart).TotalMilliseconds);
                firstTokenRecorded = true;
            }

            yield return token;
        }

        _health.RecordSuccess();
        var totalMs = Stopwatch.GetElapsedTime(totalStart).TotalMilliseconds;
        RagDiagnostics.CompletionTotalDuration.Record(totalMs);
        _logger.LogInformation("Response finished in {ElapsedMs:F1}ms", totalMs);
    }

    protected abstract IAsyncEnumerable<string> StreamTokensAsync(
        QueryPayload payload, CancellationToken cancellationToken);
}

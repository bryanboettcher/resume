using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using ResumeChat.Rag.Models;

namespace ResumeChat.Rag.Completion;

public abstract class CompletionProviderBase : ICompletionProvider, ICompletionMetadata
{
    private readonly CompletionSecurityOptions _security;
    private readonly ILogger _logger;

    protected CompletionProviderBase(CompletionSecurityOptions security, ILogger logger)
    {
        _security = security;
        _logger = logger;
    }

    protected abstract string ProviderName { get; }
    protected abstract string ModelName { get; }

    string ICompletionMetadata.Provider => ProviderName;
    string ICompletionMetadata.Model => ModelName;

    public async IAsyncEnumerable<string> CompleteAsync(
        CompletionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var activity = StartActivity(request);
        var totalStart = Stopwatch.GetTimestamp();
        var firstTokenRecorded = false;

        LogStarted(request);
        var systemPrompt = BuildSystemPrompt(request);

        await foreach (var token in StreamTokensAsync(systemPrompt, request, cancellationToken)
                           .ConfigureAwait(false))
        {
            RecordFirstToken(ref firstTokenRecorded, totalStart);
            yield return token;
        }

        RecordFinished(totalStart);
    }

    protected abstract IAsyncEnumerable<string> StreamTokensAsync(
        string systemPrompt, CompletionRequest request, CancellationToken cancellationToken);

    protected static Activity? StartActivity(string providerName, string modelName, CompletionRequest request)
    {
        var activity = RagDiagnostics.ActivitySource.StartActivity("rag.complete");
        activity?.SetTag("rag.complete.model", modelName);
        activity?.SetTag("rag.complete.provider", providerName);
        activity?.SetTag("rag.complete.context_chunks", request.Context.Count);
        return activity;
    }

    protected static void RecordFirstToken(ref bool recorded, long startTimestamp)
    {
        if (recorded) return;
        RagDiagnostics.CompletionFirstTokenDuration.Record(
            Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds);
        recorded = true;
    }

    protected static void RecordFinished(long startTimestamp, ILogger logger)
    {
        var totalMs = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
        RagDiagnostics.CompletionTotalDuration.Record(totalMs);
        logger.LogInformation("Completion finished in {ElapsedMs:F1}ms", totalMs);
    }

    private Activity? StartActivity(CompletionRequest request) =>
        StartActivity(ProviderName, ModelName, request);

    private void RecordFinished(long startTimestamp) =>
        RecordFinished(startTimestamp, _logger);

    private string BuildSystemPrompt(CompletionRequest request) =>
        SystemPromptBuilder.Build(request, _security.Canary);

    private void LogStarted(CompletionRequest request) =>
        _logger.LogInformation("Starting {Provider} completion with model {Model} ({ContextChunks} context chunks)",
            ProviderName, ModelName, request.Context.Count);
}

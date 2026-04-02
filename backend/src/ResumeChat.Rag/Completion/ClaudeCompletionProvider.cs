using System.Diagnostics;
using System.Runtime.CompilerServices;
using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ResumeChat.Rag.Models;

namespace ResumeChat.Rag.Completion;

public sealed class ClaudeCompletionProvider : CompletionProviderBase
{
    private readonly AnthropicClient _client;
    private readonly ClaudeCompletionOptions _options;

    public ClaudeCompletionProvider(
        IOptions<ClaudeCompletionOptions> options,
        IOptions<CompletionSecurityOptions> security,
        ILogger<ClaudeCompletionProvider> logger)
        : base(security.Value, logger)
    {
        _options = options.Value;
        _client = new AnthropicClient { ApiKey = _options.ApiKey };
    }

    protected override string ProviderName => "Claude";
    protected override string ModelName => _options.Model;

    protected override async IAsyncEnumerable<string> StreamTokensAsync(
        string systemPrompt,
        CompletionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var activity = RagDiagnostics.ActivitySource.StartActivity("rag.complete");
        activity?.SetTag("rag.complete.model", _options.Model);
        activity?.SetTag("rag.complete.provider", ProviderName);
        activity?.SetTag("rag.complete.context_chunks", request.Context.Count);

        var totalStart = Stopwatch.GetTimestamp();
        var firstTokenRecorded = false;

        Logger.LogInformation("Starting Claude completion with model {Model} ({ContextChunks} context chunks)",
            _options.Model, request.Context.Count);

        var parameters = new MessageCreateParams
        {
            Model = _options.Model,
            MaxTokens = _options.MaxTokens,
            System = systemPrompt,
            Messages = [new() { Role = "user", Content = request.UserMessage }]
        };

        await foreach (var evt in _client.Messages.CreateStreaming(parameters, cancellationToken)
                           .ConfigureAwait(false))
        {
            if (evt.TryPickContentBlockDelta(out var delta) &&
                delta.Delta.TryPickText(out var textDelta))
            {
                if (!firstTokenRecorded)
                {
                    RagDiagnostics.CompletionFirstTokenDuration.Record(
                        Stopwatch.GetElapsedTime(totalStart).TotalMilliseconds);
                    firstTokenRecorded = true;
                }

                yield return textDelta.Text;
            }
        }

        var totalMs = Stopwatch.GetElapsedTime(totalStart).TotalMilliseconds;
        RagDiagnostics.CompletionTotalDuration.Record(totalMs);
        Logger.LogInformation("Claude completion finished in {ElapsedMs:F1}ms", totalMs);
    }
}

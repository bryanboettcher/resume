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
                yield return textDelta.Text;
            }
        }
    }
}

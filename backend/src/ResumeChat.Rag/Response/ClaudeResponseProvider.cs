using System.Runtime.CompilerServices;
using Anthropic;
using Anthropic.Core;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ResumeChat.Rag.Completion;
using ResumeChat.Rag.Models;

namespace ResumeChat.Rag.Response;

public sealed class ClaudeResponseProvider : ResponseProviderBase
{
    private readonly AnthropicClient _client;
    private readonly ClaudeResponseOptions _options;
    private readonly CompletionSecurityOptions _security;

    public ClaudeResponseProvider(
        HttpClient httpClient,
        IOptions<ClaudeResponseOptions> options,
        IOptions<CompletionSecurityOptions> security,
        ILogger<ClaudeResponseProvider> logger)
        : base(logger)
    {
        _options = options.Value;
        _security = security.Value;
        _client = new AnthropicClient(new ClientOptions
        {
            ApiKey = _options.ApiKey,
            HttpClient = httpClient
        });
    }

    protected override string ProviderName => "Claude";
    protected override string ModelName => _options.Model;

    protected override async IAsyncEnumerable<string> StreamTokensAsync(
        QueryPayload payload,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var systemPrompt = SystemPromptBuilder.Build(payload, _security.Canary);

        var messages = new List<MessageParam>();
        if (payload.History is { Count: > 0 })
        {
            foreach (var exchange in payload.History)
            {
                messages.Add(new() { Role = "user", Content = exchange.Prompt });
                messages.Add(new() { Role = "assistant", Content = exchange.Response });
            }
        }
        messages.Add(new() { Role = "user", Content = payload.OriginalMessage });

        var parameters = new MessageCreateParams
        {
            Model = _options.Model,
            MaxTokens = _options.MaxTokens,
            System = systemPrompt,
            Messages = messages
        };

        await foreach (var evt in _client.Messages.CreateStreaming(parameters, cancellationToken))
        {
            if (evt.TryPickContentBlockDelta(out var delta) &&
                delta.Delta.TryPickText(out var textDelta))
            {
                yield return textDelta.Text;
            }
        }
    }
}

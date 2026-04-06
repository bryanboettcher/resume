using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ResumeChat.Rag.Completion;
using ResumeChat.Rag.Models;

namespace ResumeChat.Rag.Response;

public sealed class OllamaResponseProvider : ResponseProviderBase
{
    private readonly HttpClient _httpClient;
    private readonly OllamaResponseOptions _options;
    private readonly CompletionSecurityOptions _security;

    public OllamaResponseProvider(
        HttpClient httpClient,
        IOptions<OllamaResponseOptions> options,
        IOptions<CompletionSecurityOptions> security,
        ILogger<OllamaResponseProvider> logger)
        : base(logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _security = security.Value;
    }

    protected override string ProviderName => "Ollama";
    protected override string ModelName => _options.Model;

    protected override async IAsyncEnumerable<string> StreamTokensAsync(
        QueryPayload payload,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var systemPrompt = SystemPromptBuilder.Build(payload, _security.Canary);

        var messages = new List<object> { new { role = "system", content = systemPrompt } };
        if (payload.History is { Count: > 0 })
        {
            foreach (var exchange in payload.History)
            {
                messages.Add(new { role = "user", content = exchange.Prompt });
                messages.Add(new { role = "assistant", content = exchange.Response });
            }
        }
        messages.Add(new { role = "user", content = payload.OriginalMessage });

        var body = new
        {
            model = _options.Model,
            messages,
            stream = true
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl.TrimEnd('/')}/api/chat")
        {
            Content = JsonContent.Create(body)
        };

        using var response = await _httpClient.SendAsync(
            httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(stream);

        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(line))
                continue;

            var chunk = JsonSerializer.Deserialize<OllamaChatChunk>(line);
            if (chunk?.Message?.Content is { Length: > 0 } content)
                yield return content;

            if (chunk?.Done == true)
                yield break;
        }
    }

    private sealed record OllamaChatChunk(
        [property: JsonPropertyName("message")] OllamaChatMessage? Message,
        [property: JsonPropertyName("done")] bool Done);

    private sealed record OllamaChatMessage(
        [property: JsonPropertyName("content")] string Content);
}

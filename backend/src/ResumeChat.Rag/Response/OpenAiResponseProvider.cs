using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ResumeChat.Rag.Completion;
using ResumeChat.Rag.Models;

namespace ResumeChat.Rag.Response;

public sealed class OpenAiResponseProvider : ResponseProviderBase
{
    private const string DataPrefix = "data:";
    private const string DoneSentinel = "[DONE]";

    private readonly HttpClient _httpClient;
    private readonly OpenAiResponseOptions _options;
    private readonly CompletionSecurityOptions _security;

    public OpenAiResponseProvider(
        HttpClient httpClient,
        IOptions<OpenAiResponseOptions> options,
        IOptions<CompletionSecurityOptions> security,
        ILogger<OpenAiResponseProvider> logger)
        : base(logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _security = security.Value;
    }

    protected override string ProviderName => "OpenAI";
    protected override string ModelName => _options.Model;

    protected override async IAsyncEnumerable<string> StreamTokensAsync(
        QueryPayload payload,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var body = new
        {
            model = _options.Model,
            messages = ChatMessages.Build(payload, _security.Canary),
            max_tokens = _options.MaxTokens,
            stream = true
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_options.BaseUrl.TrimEnd('/')}/chat/completions")
        {
            Content = JsonContent.Create(body)
        };
        if (_options.ApiKey.Length > 0)
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        using var response = await _httpClient.SendAsync(
            httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        var started = false;
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!line.StartsWith(DataPrefix, StringComparison.Ordinal))
                continue;

            var data = line[DataPrefix.Length..].Trim();
            if (data == DoneSentinel)
                yield break;

            var chunk = JsonSerializer.Deserialize<ChatCompletionChunk>(data);
            if (chunk?.Choices is not [{ Delta.Content: { Length: > 0 } content }, ..])
                continue;

            if (!started)
            {
                content = content.TrimStart();
                if (content.Length == 0)
                    continue;
                started = true;
            }

            yield return content;
        }
    }

    private sealed record ChatCompletionChunk(
        [property: JsonPropertyName("choices")] IReadOnlyList<ChatCompletionChoice>? Choices);

    private sealed record ChatCompletionChoice(
        [property: JsonPropertyName("delta")] ChatCompletionDelta? Delta);

    private sealed record ChatCompletionDelta(
        [property: JsonPropertyName("content")] string? Content);
}

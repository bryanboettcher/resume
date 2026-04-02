using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using ResumeChat.Rag.Models;

namespace ResumeChat.Rag.Completion;

public sealed class ClaudeCompletionProvider : ICompletionProvider
{
    private readonly HttpClient _httpClient;
    private readonly ClaudeCompletionOptions _options;
    private readonly CompletionSecurityOptions _security;

    public ClaudeCompletionProvider(
        HttpClient httpClient,
        IOptions<ClaudeCompletionOptions> options,
        IOptions<CompletionSecurityOptions> security)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _security = security.Value;
    }

    public async IAsyncEnumerable<string> CompleteAsync(
        CompletionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var systemPrompt = SystemPromptBuilder.Build(request, _security.Canary);

        var body = new
        {
            model = _options.Model,
            max_tokens = _options.MaxTokens,
            system = systemPrompt,
            messages = new[]
            {
                new { role = "user", content = request.UserMessage }
            },
            stream = true
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages")
        {
            Content = JsonContent.Create(body)
        };
        httpRequest.Headers.Add("x-api-key", _options.ApiKey);
        httpRequest.Headers.Add("anthropic-version", "2023-06-01");

        using var response = await _httpClient.SendAsync(
            httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var reader = new StreamReader(stream);

        while (await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false) is { } line)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: "))
                continue;

            var json = line["data: ".Length..];

            var evt = JsonSerializer.Deserialize<ClaudeStreamEvent>(json);
            if (evt?.Type == "content_block_delta" && evt.Delta?.Text is { Length: > 0 } text)
                yield return text;
            else if (evt?.Type == "message_stop")
                yield break;
        }
    }

    private sealed record ClaudeStreamEvent(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("delta")] ClaudeDelta? Delta);

    private sealed record ClaudeDelta(
        [property: JsonPropertyName("text")] string? Text);
}

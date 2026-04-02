using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ResumeChat.Corpus.Cli;

/// <summary>
/// IOllamaAnalyzer implementation parameterized on prompt text rather than hardcoding it,
/// enabling per-language prompt tuning through the dispatcher without a class explosion.
/// </summary>
sealed class PromptingTriageProvider(OllamaOptions options, string triagePromptTemplate, string fullAnalysisPromptTemplate)
    : IOllamaAnalyzer, IDisposable
{
    private readonly HttpClient _http = new() { BaseAddress = new Uri(options.BaseUrl), Timeout = TimeSpan.FromMinutes(5) };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task<TriageResult?> TriageAsync(string filePath, string language, string content, CancellationToken ct)
    {
        var observation = await TriageDetailAsync(filePath, language, content, ct).ConfigureAwait(false);
        if (observation is null) return null;

        // Any dimension true → analyze; all false → skip.
        var anyTrue = observation.HasLogic || observation.HasDomainRules || observation.HasComposition || observation.HasDataModeling;

        return new TriageResult
        {
            Interest = anyTrue ? "medium" : "low",
            SkipAnalysis = !anyTrue,
            Reason = observation.Reasoning,
        };
    }

    public async Task<TriageObservation?> TriageDetailAsync(string filePath, string language, string content, CancellationToken ct)
    {
        var prompt = triagePromptTemplate
            .Replace("<<FILE_PATH>>", filePath)
            .Replace("<<LANGUAGE>>", language)
            .Replace("<<CONTENT>>", TruncateForPrompt(content));
        var json = await GenerateAsync(prompt, ct).ConfigureAwait(false);
        if (json is null) return null;

        try
        {
            return JsonSerializer.Deserialize<TriageObservation>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public async Task<FullAnalysisResult?> AnalyzeAsync(string filePath, string language, string content, CancellationToken ct)
    {
        var prompt = fullAnalysisPromptTemplate
            .Replace("<<FILE_PATH>>", filePath)
            .Replace("<<LANGUAGE>>", language)
            .Replace("<<CONTENT>>", TruncateForPrompt(content));
        var json = await GenerateAsync(prompt, ct).ConfigureAwait(false);
        if (json is null) return null;

        try
        {
            return JsonSerializer.Deserialize<FullAnalysisResult>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private async Task<string?> GenerateAsync(string prompt, CancellationToken ct)
    {
        var request = new OllamaGenerateRequest
        {
            Model = options.Model,
            Prompt = prompt,
            Stream = false,
            Format = "json",
        };

        using var response = await _http.PostAsJsonAsync("/api/generate", request, JsonOptions, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode) return null;

        var result = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(JsonOptions, ct).ConfigureAwait(false);
        if (result?.Response is null) return null;

        var text = result.Response.Trim();

        // Strip markdown code fences that some models emit around JSON responses.
        if (text.StartsWith("```"))
        {
            var firstNewline = text.IndexOf('\n');
            if (firstNewline >= 0) text = text[(firstNewline + 1)..];
            if (text.EndsWith("```")) text = text[..^3];
            text = text.Trim();
        }

        return text;
    }

    private static string TruncateForPrompt(string content)
    {
        const int maxChars = 48_000;
        if (content.Length <= maxChars) return content;
        return content[..maxChars] + "\n\n[... truncated ...]";
    }

    public void Dispose() => _http.Dispose();
}

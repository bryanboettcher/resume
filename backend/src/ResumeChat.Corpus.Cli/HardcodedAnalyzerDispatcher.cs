using System.Collections.Concurrent;

namespace ResumeChat.Corpus.Cli;

/// <summary>
/// Dispatches to a PromptingTriageProvider selected by language. Instances are cached
/// per language so the HttpClient is not recreated on every file.
/// </summary>
sealed class HardcodedAnalyzerDispatcher(OllamaOptions options) : IAnalyzerDispatcher
{
    private readonly ConcurrentDictionary<string, IOllamaAnalyzer> _cache = new(StringComparer.OrdinalIgnoreCase);

    public IOllamaAnalyzer GetAnalyzer(string? language)
    {
        var key = language ?? "default";
        return _cache.GetOrAdd(key, k => new PromptingTriageProvider(options, SelectTriagePrompt(k), TriagePrompts.FullAnalysis));
    }

    private static string SelectTriagePrompt(string language) => language.ToLowerInvariant() switch
    {
        "csharp" => TriagePrompts.CSharp,
        "typescript" => TriagePrompts.TypeScript,
        "yaml" => TriagePrompts.Yaml,
        "html" => TriagePrompts.Html,
        "sql" => TriagePrompts.Sql,
        "markdown" => TriagePrompts.Markdown,
        _ => TriagePrompts.Default,
    };
}

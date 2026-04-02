namespace ResumeChat.Corpus.Cli;

/// <summary>
/// Parsed CLI filter and override options for analysis commands. Null/zero means "no value applied".
/// </summary>
sealed record AnalysisFilter(
    string? Repo,
    string? BranchPrefix,
    string? Language,
    int? Limit,
    string? OllamaUrl,
    int? Concurrency,
    string? Model)
{
    public static readonly AnalysisFilter None = new(null, null, null, null, null, null, null);

    /// <summary>
    /// Parses --repo, --branch, --language, --limit, --ollama-url, --concurrency, and --model from args
    /// starting at the given offset. Unknown flags are silently ignored.
    /// </summary>
    public static AnalysisFilter Parse(string[] args, int startIndex = 1)
    {
        string? repo = null;
        string? branch = null;
        string? language = null;
        int? limit = null;
        string? ollamaUrl = null;
        int? concurrency = null;
        string? model = null;

        for (var i = startIndex; i < args.Length - 1; i++)
        {
            switch (args[i])
            {
                case "--repo":
                    repo = args[++i];
                    break;
                case "--branch":
                    branch = args[++i];
                    break;
                case "--language":
                    language = args[++i];
                    break;
                case "--limit":
                    if (int.TryParse(args[++i], out var n) && n > 0)
                        limit = n;
                    break;
                case "--ollama-url":
                    ollamaUrl = args[++i];
                    break;
                case "--concurrency":
                    if (int.TryParse(args[++i], out var c) && c > 0)
                        concurrency = c;
                    break;
                case "--model":
                    model = args[++i];
                    break;
            }
        }

        return new AnalysisFilter(repo, branch, language, limit, ollamaUrl, concurrency, model);
    }

    public bool IsEmpty =>
        Repo is null && BranchPrefix is null && Language is null && Limit is null &&
        OllamaUrl is null && Concurrency is null && Model is null;

}

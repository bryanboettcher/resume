namespace ResumeChat.Corpus.Cli;

sealed class TriageResult
{
    public string Interest { get; init; } = "low";
    public bool SkipAnalysis { get; init; } = true;
    public string Reason { get; init; } = string.Empty;
}

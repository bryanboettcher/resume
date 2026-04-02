namespace ResumeChat.Corpus.Cli;

sealed class FullAnalysisResult
{
    public string Purpose { get; init; } = string.Empty;
    public string[] DomainConcepts { get; init; } = [];
    public PatternEntry[] Patterns { get; init; } = [];
    public string[] NotableTechniques { get; init; } = [];
    public string[] Frameworks { get; init; } = [];
    public string[] Interactions { get; init; } = [];
    public string Complexity { get; init; } = "low";
    public string[] ResumeKeywords { get; init; } = [];
}

sealed class PatternEntry
{
    public string Name { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;
}

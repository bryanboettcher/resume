namespace ResumeChat.Corpus.Cli;

sealed class TriageObservation
{
    public string Reasoning { get; init; } = string.Empty;
    public bool HasLogic { get; init; }
    public bool HasDomainRules { get; init; }
    public bool HasComposition { get; init; }
    public bool HasDataModeling { get; init; }
}

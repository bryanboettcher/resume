namespace ResumeChat.Corpus.Cli;

interface IOllamaAnalyzer
{
    Task<TriageResult?> TriageAsync(string filePath, string language, string content, CancellationToken ct);
    Task<TriageObservation?> TriageDetailAsync(string filePath, string language, string content, CancellationToken ct);
    Task<FullAnalysisResult?> AnalyzeAsync(string filePath, string language, string content, CancellationToken ct);
}

namespace ResumeChat.Corpus.Cli;

interface IAnalysisRunner
{
    Task<AnalysisStats> RunTriageAsync(AnalysisFilter filter, CancellationToken ct);
    Task<AnalysisStats> RunFullAnalysisAsync(AnalysisFilter filter, CancellationToken ct);
}

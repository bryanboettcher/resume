namespace ResumeChat.Rag.Classification;

public sealed class PassthroughThreatClassifier : IThreatClassifier
{
    public Task<ThreatResult> ClassifyAsync(string message, CancellationToken cancellationToken = default) =>
        Task.FromResult(ThreatResult.Safe());
}

namespace ResumeChat.Rag.Classification;

public interface IThreatClassifier
{
    Task<ThreatResult> ClassifyAsync(string message, CancellationToken cancellationToken = default);
}

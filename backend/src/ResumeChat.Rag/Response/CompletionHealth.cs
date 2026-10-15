namespace ResumeChat.Rag.Response;

public sealed record CompletionHealthSnapshot(
    bool Healthy,
    DateTimeOffset? LastSuccess,
    DateTimeOffset? LastFailure,
    string? LastError);

/// <summary>
/// Tracks the outcome of the most recent completions so health reporting reflects whether the
/// LLM is actually answering, not just whether a provider is configured.
/// </summary>
public sealed class CompletionHealth(TimeProvider timeProvider)
{
    private readonly Lock _gate = new();
    private DateTimeOffset? _lastSuccess;
    private DateTimeOffset? _lastFailure;
    private string? _lastError;

    public void RecordSuccess()
    {
        lock (_gate)
            _lastSuccess = timeProvider.GetUtcNow();
    }

    public void RecordFailure(Exception exception)
    {
        lock (_gate)
        {
            _lastFailure = timeProvider.GetUtcNow();
            _lastError = exception.GetType().Name;
        }
    }

    public CompletionHealthSnapshot Snapshot()
    {
        lock (_gate)
        {
            var healthy = _lastFailure is null || _lastSuccess > _lastFailure;
            return new CompletionHealthSnapshot(healthy, _lastSuccess, _lastFailure, _lastError);
        }
    }
}

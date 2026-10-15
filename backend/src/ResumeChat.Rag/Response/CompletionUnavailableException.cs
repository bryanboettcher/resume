namespace ResumeChat.Rag.Response;

public sealed class CompletionUnavailableException(string provider, string model, Exception innerException)
    : Exception($"{provider} completion failed (model {model}): {innerException.Message}", innerException)
{
    public string Provider { get; } = provider;
    public string Model { get; } = model;
}

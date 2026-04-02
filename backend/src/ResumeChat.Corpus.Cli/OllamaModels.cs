namespace ResumeChat.Corpus.Cli;

sealed class OllamaGenerateRequest
{
    public string Model { get; init; } = string.Empty;
    public string Prompt { get; init; } = string.Empty;
    public bool Stream { get; init; }
    public string? Format { get; init; }
}

sealed class OllamaGenerateResponse
{
    public string? Response { get; init; }
}

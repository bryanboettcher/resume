using System.ComponentModel.DataAnnotations;

namespace ResumeChat.Rag.Classification;

public sealed class OllamaThreatClassifierOptions
{
    public const string SectionName = "Ollama:Guard";

    [Required, MinLength(1)]
    public string BaseUrl { get; set; } = string.Empty;

    [Required, MinLength(1)]
    public string Model { get; set; } = "qwen3:4b";

    [Range(1, 60)]
    public int TimeoutSeconds { get; set; } = 10;
}

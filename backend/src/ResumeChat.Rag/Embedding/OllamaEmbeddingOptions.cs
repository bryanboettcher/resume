using System.ComponentModel.DataAnnotations;

namespace ResumeChat.Rag.Embedding;

public sealed class OllamaEmbeddingOptions
{
    public const string SectionName = "Ollama:Embedding";

    [Required, MinLength(1)]
    public string BaseUrl { get; set; } = string.Empty;

    [Required, MinLength(1)]
    public string Model { get; set; } = "nomic-embed-text";
}

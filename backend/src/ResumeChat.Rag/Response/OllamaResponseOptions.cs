using System.ComponentModel.DataAnnotations;

namespace ResumeChat.Rag.Response;

public sealed class OllamaResponseOptions
{
    public const string SectionName = "Ollama:Response";

    [Required, MinLength(1)]
    public string BaseUrl { get; set; } = string.Empty;

    [Required, MinLength(1)]
    public string Model { get; set; } = "llama3.2";
}

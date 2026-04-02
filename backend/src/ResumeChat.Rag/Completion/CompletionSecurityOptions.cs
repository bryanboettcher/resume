using System.ComponentModel.DataAnnotations;

namespace ResumeChat.Rag.Completion;

public sealed class CompletionSecurityOptions
{
    public const string SectionName = "Security";

    [Required]
    [MinLength(16)]
    public string Canary { get; set; } = string.Empty;
}

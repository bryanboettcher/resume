using System.ComponentModel.DataAnnotations;

namespace ResumeChat.Api.Options;

public sealed class CorpusOptions
{
    public const string SectionName = "Corpus";

    [Required, MinLength(1)]
    public string Directory { get; set; } = string.Empty;
}

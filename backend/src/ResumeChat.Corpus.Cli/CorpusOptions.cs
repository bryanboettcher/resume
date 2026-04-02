using System.ComponentModel.DataAnnotations;

namespace ResumeChat.Corpus.Cli;

sealed class CorpusOptions
{
    public const string SectionName = "Corpus";

    [Required]
    public string ConnectionString { get; init; } = string.Empty;

    [Required, MinLength(1)]
    public SourceConfig[] Sources { get; init; } = [];
}

sealed class SourceConfig
{
    [Required]
    public string Repo { get; init; } = string.Empty;

    [Required]
    public string Path { get; init; } = string.Empty;

    public string Branch { get; init; } = "main";
}

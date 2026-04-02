namespace ResumeChat.Corpus.Cli;

sealed record SourceFile(
    string Repo,
    string Branch,
    string FilePath,
    string? Language,
    string ContentText,
    string ContentHash,
    int LineCount,
    int SizeBytes
);

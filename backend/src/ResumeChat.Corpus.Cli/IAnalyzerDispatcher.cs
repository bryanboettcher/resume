namespace ResumeChat.Corpus.Cli;

interface IAnalyzerDispatcher
{
    IOllamaAnalyzer GetAnalyzer(string? language);
}

namespace ResumeChat.Rag.Completion;

public interface ICompletionMetadata
{
    string Provider { get; }
    string Model { get; }
}

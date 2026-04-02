using System.Text;
using ResumeChat.Rag.Models;

namespace ResumeChat.Rag.Completion;

public static class SystemPromptBuilder
{
    private const string BasePrompt = """
        You are a helpful assistant on Bryan Boettcher's resume website. Answer questions about Bryan's
        experience, skills, and projects using ONLY the context provided below. If the context doesn't
        contain enough information to answer, say so honestly — do not fabricate details.

        Be conversational but professional. Keep answers concise and relevant. When citing specific
        projects or achievements, mention the project name and timeframe when available.
        """;

    public static string Build(CompletionRequest request)
    {
        var sb = new StringBuilder();
        sb.AppendLine(BasePrompt);
        sb.AppendLine();

        if (request.Context.Count > 0)
        {
            sb.AppendLine("## Relevant Context");
            sb.AppendLine();

            foreach (var scored in request.Context)
            {
                var chunk = scored.Chunk;
                sb.AppendLine($"### {chunk.SectionHeading} (from {chunk.Metadata.SourceFile})");
                sb.AppendLine(chunk.Text);
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }
}

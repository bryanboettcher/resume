using ResumeChat.Rag.Models;

namespace ResumeChat.Rag.Chunking;

public sealed class MarkdownSectionChunkingStrategy : IChunkingStrategy
{
    private const int MaxTokenEstimate = 400;
    private const int TokensPerWord = 1; // conservative: ~0.75 tokens/word for English, round up

    public IReadOnlyList<DocumentChunk> Chunk(string content, DocumentMetadata metadata)
    {
        var cleaned = StripFrontmatter(content);
        var sections = SplitOnHeadings(cleaned);
        var chunks = new List<DocumentChunk>();

        foreach (var (heading, body) in sections)
        {
            if (string.IsNullOrWhiteSpace(body))
                continue;

            var trimmed = body.Trim();
            if (EstimateTokens(trimmed) <= MaxTokenEstimate)
            {
                chunks.Add(new DocumentChunk(trimmed, heading, chunks.Count, metadata));
            }
            else
            {
                foreach (var sub in SplitAtParagraphs(trimmed, heading))
                {
                    chunks.Add(new DocumentChunk(sub, heading, chunks.Count, metadata));
                }
            }
        }

        return chunks;
    }

    private static string StripFrontmatter(string content)
    {
        if (!content.StartsWith("---"))
            return content;

        var endIndex = content.IndexOf("---", 3, StringComparison.Ordinal);
        if (endIndex < 0)
            return content;

        return content[(endIndex + 3)..].TrimStart('\r', '\n');
    }

    private static List<(string Heading, string Body)> SplitOnHeadings(string content)
    {
        var sections = new List<(string Heading, string Body)>();
        var lines = content.Split('\n');
        var currentHeading = "Introduction";
        var currentBody = new List<string>();

        foreach (var line in lines)
        {
            if (line.StartsWith("## ") || line.StartsWith("# "))
            {
                if (currentBody.Count > 0)
                {
                    sections.Add((currentHeading, string.Join('\n', currentBody)));
                    currentBody.Clear();
                }

                currentHeading = line.TrimStart('#', ' ');
            }
            else if (line.TrimStart().StartsWith("---"))
            {
                // skip horizontal rules, they're just visual dividers
            }
            else
            {
                currentBody.Add(line);
            }
        }

        if (currentBody.Count > 0)
            sections.Add((currentHeading, string.Join('\n', currentBody)));

        return sections;
    }

    private IReadOnlyList<string> SplitAtParagraphs(string text, string heading)
    {
        var paragraphs = text.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
        var result = new List<string>();
        var current = new List<string>();
        var currentTokens = 0;

        foreach (var para in paragraphs)
        {
            var paraTokens = EstimateTokens(para);

            if (currentTokens + paraTokens > MaxTokenEstimate && current.Count > 0)
            {
                result.Add(string.Join("\n\n", current).Trim());
                current.Clear();
                currentTokens = 0;
            }

            current.Add(para.Trim());
            currentTokens += paraTokens;
        }

        if (current.Count > 0)
            result.Add(string.Join("\n\n", current).Trim());

        return result;
    }

    private static int EstimateTokens(string text) =>
        text.Split((char[]?) null, StringSplitOptions.RemoveEmptyEntries).Length * TokensPerWord;
}

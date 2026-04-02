using ResumeChat.Rag.Chunking;
using ResumeChat.Rag.Models;

namespace ResumeChat.Rag.Tests;

public sealed class MarkdownSectionChunkingStrategyTests
{
    private readonly MarkdownSectionChunkingStrategy _strategy = new();
    private readonly DocumentMetadata _meta = new("test.md", "Test", []);

    [Fact]
    public void Chunk_SimpleSections_SplitsOnHeadings()
    {
        var content = """
                      ## Section One
                      Content for section one.

                      ## Section Two
                      Content for section two.
                      """;

        var chunks = _strategy.Chunk(content, _meta);

        Assert.Equal(2, chunks.Count);
        Assert.Equal("Section One", chunks[0].SectionHeading);
        Assert.Equal("Section Two", chunks[1].SectionHeading);
        Assert.Contains("Content for section one", chunks[0].Text);
        Assert.Contains("Content for section two", chunks[1].Text);
    }

    [Fact]
    public void Chunk_StripsFrontmatter()
    {
        var content = """
                      ---
                      skill: Testing
                      tags: [test]
                      ---

                      # Title

                      ## Section
                      Actual content here.
                      """;

        var chunks = _strategy.Chunk(content, _meta);

        Assert.Single(chunks);
        Assert.DoesNotContain("skill:", chunks[0].Text);
        Assert.Contains("Actual content here", chunks[0].Text);
    }

    [Fact]
    public void Chunk_SkipsHorizontalRules()
    {
        var content = """
                      ## Section
                      Before the rule.

                      ---

                      ## Next Section
                      After the rule.
                      """;

        var chunks = _strategy.Chunk(content, _meta);

        Assert.Equal(2, chunks.Count);
        Assert.DoesNotContain("---", chunks[0].Text);
    }

    [Fact]
    public void Chunk_LargeSection_SplitsAtParagraphs()
    {
        // Build a section with enough words to exceed the 400-token estimate
        var bigParagraph1 = string.Join(' ', Enumerable.Repeat("word", 250));
        var bigParagraph2 = string.Join(' ', Enumerable.Repeat("more", 250));

        var content = $"## Big Section\n{bigParagraph1}\n\n{bigParagraph2}";

        var chunks = _strategy.Chunk(content, _meta);

        Assert.True(chunks.Count >= 2, "Large section should be split into multiple chunks");
        Assert.All(chunks, c => Assert.Equal("Big Section", c.SectionHeading));
    }

    [Fact]
    public void Chunk_EmptySection_IsSkipped()
    {
        var content = """
                      ## Empty

                      ## Has Content
                      This one has content.
                      """;

        var chunks = _strategy.Chunk(content, _meta);

        Assert.Single(chunks);
        Assert.Equal("Has Content", chunks[0].SectionHeading);
    }

    [Fact]
    public void Chunk_AssignsSequentialIndexes()
    {
        var content = """
                      ## A
                      Content A.

                      ## B
                      Content B.

                      ## C
                      Content C.
                      """;

        var chunks = _strategy.Chunk(content, _meta);

        Assert.Equal(3, chunks.Count);
        Assert.Equal(0, chunks[0].ChunkIndex);
        Assert.Equal(1, chunks[1].ChunkIndex);
        Assert.Equal(2, chunks[2].ChunkIndex);
    }

    [Fact]
    public void Chunk_PreservesMetadata()
    {
        var meta = new DocumentMetadata("evidence/perf.md", "Performance", ["perf", "bench"]);
        var content = """
                      ## Section
                      Some content.
                      """;

        var chunks = _strategy.Chunk(content, meta);

        Assert.Single(chunks);
        Assert.Same(meta, chunks[0].Metadata);
    }
}

using ResumeChat.Rag.Chunking;
using ResumeChat.Rag.Models;

namespace ResumeChat.Rag.Tests;

[TestFixture]
public sealed class MarkdownSectionChunkingStrategyTests
{
    private readonly MarkdownSectionChunkingStrategy _strategy = new();
    private readonly DocumentMetadata _meta = new("test.md", "Test", []);

    [Test]
    public void Chunk_SimpleSections_SplitsOnHeadings()
    {
        var content = """
                      ## Section One
                      Content for section one.

                      ## Section Two
                      Content for section two.
                      """;

        var chunks = _strategy.Chunk(content, _meta);

        chunks.Count.ShouldBe(2);
        chunks[0].SectionHeading.ShouldBe("Section One");
        chunks[1].SectionHeading.ShouldBe("Section Two");
        chunks[0].Text.ShouldContain("Content for section one");
        chunks[1].Text.ShouldContain("Content for section two");
    }

    [Test]
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

        chunks.ShouldHaveSingleItem();
        chunks[0].Text.ShouldNotContain("skill:");
        chunks[0].Text.ShouldContain("Actual content here");
    }

    [Test]
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

        chunks.Count.ShouldBe(2);
        chunks[0].Text.ShouldNotContain("---");
    }

    [Test]
    public void Chunk_LargeSection_SplitsAtParagraphs()
    {
        // Build a section with enough words to exceed the 400-token estimate
        var bigParagraph1 = string.Join(' ', Enumerable.Repeat("word", 250));
        var bigParagraph2 = string.Join(' ', Enumerable.Repeat("more", 250));

        var content = $"## Big Section\n{bigParagraph1}\n\n{bigParagraph2}";

        var chunks = _strategy.Chunk(content, _meta);

        (chunks.Count >= 2).ShouldBeTrue("Large section should be split into multiple chunks");
        foreach (var c in chunks)
            c.SectionHeading.ShouldBe("Big Section");
    }

    [Test]
    public void Chunk_EmptySection_IsSkipped()
    {
        var content = """
                      ## Empty

                      ## Has Content
                      This one has content.
                      """;

        var chunks = _strategy.Chunk(content, _meta);

        chunks.ShouldHaveSingleItem();
        chunks[0].SectionHeading.ShouldBe("Has Content");
    }

    [Test]
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

        chunks.Count.ShouldBe(3);
        chunks[0].ChunkIndex.ShouldBe(0);
        chunks[1].ChunkIndex.ShouldBe(1);
        chunks[2].ChunkIndex.ShouldBe(2);
    }

    [Test]
    public void Chunk_PreservesMetadata()
    {
        var meta = new DocumentMetadata("evidence/perf.md", "Performance", ["perf", "bench"]);
        var content = """
                      ## Section
                      Some content.
                      """;

        var chunks = _strategy.Chunk(content, meta);

        chunks.ShouldHaveSingleItem();
        chunks[0].Metadata.ShouldBeSameAs(meta);
    }
}

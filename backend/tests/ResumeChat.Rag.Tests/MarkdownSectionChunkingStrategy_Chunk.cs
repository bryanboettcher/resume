using ResumeChat.Rag.Chunking;
using ResumeChat.Rag.Models;

namespace ResumeChat.Rag.Tests;

public abstract class MarkdownSectionChunkingStrategy_Chunk
{
    protected DocumentMetadata Meta = new("test.md", "Test", []);
    protected IReadOnlyList<DocumentChunk> Result = null!;

    protected abstract string Content { get; }

    [SetUp]
    public void BaseSetUp()
    {
        Arrange();

        Result = new MarkdownSectionChunkingStrategy().Chunk(Content, Meta);
    }

    protected virtual void Arrange() { }

    public class When_content_has_simple_sections : MarkdownSectionChunkingStrategy_Chunk
    {
        protected override string Content => """
                                             ## Section One
                                             Content for section one.

                                             ## Section Two
                                             Content for section two.
                                             """;

        [Test]
        public void It_should_produce_one_chunk_per_heading()
        {
            Result.Count.ShouldBe(2);
        }

        [Test]
        public void It_should_use_first_heading()
        {
            Result[0].SectionHeading.ShouldBe("Section One");
        }

        [Test]
        public void It_should_use_second_heading()
        {
            Result[1].SectionHeading.ShouldBe("Section Two");
        }

        [Test]
        public void It_should_keep_first_section_content()
        {
            Result[0].Text.ShouldContain("Content for section one");
        }

        [Test]
        public void It_should_keep_second_section_content()
        {
            Result[1].Text.ShouldContain("Content for section two");
        }
    }

    public class When_content_has_frontmatter : MarkdownSectionChunkingStrategy_Chunk
    {
        protected override string Content => """
                                             ---
                                             skill: Testing
                                             tags: [test]
                                             ---

                                             # Title

                                             ## Section
                                             Actual content here.
                                             """;

        [Test]
        public void It_should_produce_single_chunk()
        {
            Result.ShouldHaveSingleItem();
        }

        [Test]
        public void It_should_strip_frontmatter()
        {
            Result[0].Text.ShouldNotContain("skill:");
        }

        [Test]
        public void It_should_keep_section_content()
        {
            Result[0].Text.ShouldContain("Actual content here");
        }
    }

    public class When_content_has_horizontal_rule : MarkdownSectionChunkingStrategy_Chunk
    {
        protected override string Content => """
                                             ## Section
                                             Before the rule.

                                             ---

                                             ## Next Section
                                             After the rule.
                                             """;

        [Test]
        public void It_should_produce_one_chunk_per_heading()
        {
            Result.Count.ShouldBe(2);
        }

        [Test]
        public void It_should_skip_the_rule()
        {
            Result[0].Text.ShouldNotContain("---");
        }
    }

    public class When_section_exceeds_token_cap : MarkdownSectionChunkingStrategy_Chunk
    {
        protected override string Content =>
            $"## Big Section\n{string.Join(' ', Enumerable.Repeat("word", 250))}\n\n{string.Join(' ', Enumerable.Repeat("more", 250))}";

        [Test]
        public void It_should_split_into_multiple_chunks()
        {
            Result.Count.ShouldBeGreaterThanOrEqualTo(2);
        }

        [Test]
        public void It_should_keep_heading_on_every_chunk()
        {
            Result.ShouldAllBe(c => c.SectionHeading == "Big Section");
        }
    }

    public class When_section_is_empty : MarkdownSectionChunkingStrategy_Chunk
    {
        protected override string Content => """
                                             ## Empty

                                             ## Has Content
                                             This one has content.
                                             """;

        [Test]
        public void It_should_produce_single_chunk()
        {
            Result.ShouldHaveSingleItem();
        }

        [Test]
        public void It_should_skip_the_empty_section()
        {
            Result[0].SectionHeading.ShouldBe("Has Content");
        }
    }

    public class When_content_has_multiple_sections : MarkdownSectionChunkingStrategy_Chunk
    {
        protected override string Content => """
                                             ## A
                                             Content A.

                                             ## B
                                             Content B.

                                             ## C
                                             Content C.
                                             """;

        [Test]
        public void It_should_produce_one_chunk_per_heading()
        {
            Result.Count.ShouldBe(3);
        }

        [Test]
        public void It_should_assign_sequential_indexes()
        {
            Result.Select(c => c.ChunkIndex).ShouldBe([0, 1, 2]);
        }
    }

    public class When_metadata_provided : MarkdownSectionChunkingStrategy_Chunk
    {
        protected override string Content => """
                                             ## Section
                                             Some content.
                                             """;

        protected override void Arrange()
        {
            Meta = new DocumentMetadata("evidence/perf.md", "Performance", ["perf", "bench"]);
        }

        [Test]
        public void It_should_produce_single_chunk()
        {
            Result.ShouldHaveSingleItem();
        }

        [Test]
        public void It_should_carry_metadata_instance()
        {
            Result[0].Metadata.ShouldBeSameAs(Meta);
        }
    }
}

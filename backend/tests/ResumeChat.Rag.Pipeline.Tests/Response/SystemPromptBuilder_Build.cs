using ResumeChat.Rag.Models;
using ResumeChat.Rag.Response;
using Shouldly;

namespace ResumeChat.Rag.Pipeline.Tests.Response;

public abstract class SystemPromptBuilder_Build
{
    private static ScoredChunk MakeChunk(string heading, string source, string text = "sample evidence text") =>
        new(new DocumentChunk(text, heading, 0, new DocumentMetadata(source, null, [])), 0.85f);

    private static QueryPayload EmptyPayload() =>
        QueryPayload.FromQuery(
            ChatQuery.FromRequest(new ChatRequest("test question")),
            []);

    private static QueryPayload PayloadWithDocuments(IReadOnlyList<ScoredChunk> docs) =>
        QueryPayload.FromQuery(
            ChatQuery.FromRequest(new ChatRequest("test question")),
            docs);

    public class When_documents_present : SystemPromptBuilder_Build
    {
        private string _result = null!;

        [SetUp]
        public void SetUp()
        {
            var docs = new List<ScoredChunk>
            {
                MakeChunk("Kubernetes Deployment Strategy", "evidence/kubernetes.md"),
                MakeChunk("MassTransit Saga Orchestration", "projects/kansys.md"),
            };
            _result = SystemPromptBuilder.Build(PayloadWithDocuments(docs), "test-canary-token");
        }

        [Test]
        public void Output_contains_EVIDENCE_section()
            => _result.ShouldContain("EVIDENCE:");

        [Test]
        public void Output_contains_first_chunk_section_heading()
            => _result.ShouldContain("Kubernetes Deployment Strategy");

        [Test]
        public void Output_contains_first_chunk_source_file()
            => _result.ShouldContain("evidence/kubernetes.md");

        [Test]
        public void Output_contains_second_chunk_section_heading()
            => _result.ShouldContain("MassTransit Saga Orchestration");

        [Test]
        public void Output_contains_second_chunk_source_file()
            => _result.ShouldContain("projects/kansys.md");
    }

    public class When_no_documents : SystemPromptBuilder_Build
    {
        private string _result = null!;

        [SetUp]
        public void SetUp()
        {
            _result = SystemPromptBuilder.Build(EmptyPayload(), "irrelevant-canary");
        }

        [Test]
        public void Output_does_not_contain_EVIDENCE_section()
            => _result.ShouldNotContain("EVIDENCE:");

        [Test]
        public void Output_contains_base_prompt_content()
            => _result.ShouldContain("Bryan Boettcher");

        [Test]
        public void Output_contains_canary_anchor()
            => _result.ShouldContain("SECURITY ANCHOR:");
    }

    public class When_canary_provided : SystemPromptBuilder_Build
    {
        private string _result = null!;
        private const string Canary = "xK9$mP2#qR7!nL4@wV";

        [SetUp]
        public void SetUp()
        {
            _result = SystemPromptBuilder.Build(EmptyPayload(), Canary);
        }

        [Test]
        public void Output_contains_security_anchor_with_canary_value()
            => _result.ShouldContain($"SECURITY ANCHOR: {Canary}");
    }
}

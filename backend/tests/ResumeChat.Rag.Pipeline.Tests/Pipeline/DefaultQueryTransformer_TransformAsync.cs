using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using ResumeChat.Rag.Models;
using ResumeChat.Rag.Pipeline;
using ResumeChat.Rag.Retrieval;
using Shouldly;

namespace ResumeChat.Rag.Pipeline.Tests.Pipeline;

public abstract class DefaultQueryTransformer_TransformAsync
{
    protected IRetrievalProvider Retrieval = null!;
    protected List<IQueryEnricher> Enrichers = null!;
    protected DefaultQueryTransformer Subject = null!;
    protected QueryPayload Result = null!;

    private static readonly IReadOnlyList<ScoredChunk> EmptyDocs = [];

    private static ScoredChunk MakeChunk(string text) =>
        new(new DocumentChunk(text, "## Test", 0,
            new DocumentMetadata("test.md", "Test", [])), 0.9f);

    [SetUp]
    public void BaseSetUp()
    {
        Retrieval = Substitute.For<IRetrievalProvider>();
        Enrichers = [];

        Retrieval.RetrieveAsync(Arg.Any<RetrievalRequest>(), Arg.Any<CancellationToken>())
            .Returns(EmptyDocs);

        Arrange();

        Subject = new DefaultQueryTransformer(
            Enrichers,
            Retrieval,
            Options.Create(new RetrievalOptions()),
            NullLogger<DefaultQueryTransformer>.Instance);
    }

    protected virtual void Arrange() { }

    protected async Task Act(string message)
    {
        Result = await Subject.TransformAsync(new ChatRequest(message), CancellationToken.None);
    }

    // --- Scenario classes ---

    public class When_documents_are_retrieved : DefaultQueryTransformer_TransformAsync
    {
        private static readonly IReadOnlyList<ScoredChunk> _docs =
            [MakeChunk("doc one"), MakeChunk("doc two")];

        protected override void Arrange()
        {
            Retrieval.RetrieveAsync(Arg.Any<RetrievalRequest>(), Arg.Any<CancellationToken>())
                .Returns(_docs);
        }

        [SetUp]
        public async Task SetUp() => await Act("tell me about Bryan");

        [Test]
        public void It_should_return_the_retrieved_documents() =>
            Result.Documents.ShouldBe(_docs);

        [Test]
        public void It_should_preserve_original_message() =>
            Result.OriginalMessage.ShouldBe("tell me about Bryan");
    }

    public class When_enrichers_modify_query : DefaultQueryTransformer_TransformAsync
    {
        protected override void Arrange()
        {
            Enrichers.Add(new LambdaEnricher(10, q => q with { ProcessedMessage = "enriched" }));
        }

        [SetUp]
        public async Task SetUp() => await Act("original query");

        [Test]
        public async Task It_should_call_retrieval_with_enriched_query() =>
            await Retrieval.Received(1)
                .RetrieveAsync(
                    Arg.Is<RetrievalRequest>(r => r.Query == "enriched"),
                    Arg.Any<CancellationToken>());
    }

    public class When_multiple_enrichers_registered : DefaultQueryTransformer_TransformAsync
    {
        protected override void Arrange()
        {
            // Order 20 appends " [20]", Order 10 appends " [10]"
            // Order 10 should run first, so final message = "original [10] [20]"
            Enrichers.Add(new LambdaEnricher(20, q => q with { ProcessedMessage = q.ProcessedMessage + " [20]" }));
            Enrichers.Add(new LambdaEnricher(10, q => q with { ProcessedMessage = q.ProcessedMessage + " [10]" }));
        }

        [SetUp]
        public async Task SetUp() => await Act("original");

        [Test]
        public async Task It_should_call_retrieval_with_order_10_applied_first() =>
            await Retrieval.Received(1)
                .RetrieveAsync(
                    Arg.Is<RetrievalRequest>(r => r.Query == "original [10] [20]"),
                    Arg.Any<CancellationToken>());
    }

    public class When_no_enrichers_registered : DefaultQueryTransformer_TransformAsync
    {
        [SetUp]
        public async Task SetUp() => await Act("hello");

        [Test]
        public async Task It_should_call_retrieval_with_original_message() =>
            await Retrieval.Received(1)
                .RetrieveAsync(
                    Arg.Is<RetrievalRequest>(r => r.Query == "hello"),
                    Arg.Any<CancellationToken>());

        [Test]
        public void It_should_have_matching_original_and_processed_in_result()
        {
            Result.OriginalMessage.ShouldBe("hello");
            Result.ProcessedMessage.ShouldBe("hello");
        }
    }

    public class When_history_is_provided : DefaultQueryTransformer_TransformAsync
    {
        private readonly IReadOnlyList<ChatExchange> _history =
            [new("previous question", "previous answer")];

        [SetUp]
        public async Task SetUp()
        {
            Result = await Subject.TransformAsync(
                new ChatRequest("follow up", _history), CancellationToken.None);
        }

        [Test]
        public void It_should_pass_history_through_to_payload() =>
            Result.History.ShouldBe(_history);
    }

    // Lightweight test double — avoids mocking a tiny interface
    private sealed class LambdaEnricher : IQueryEnricher
    {
        private readonly Func<ChatQuery, ChatQuery> _transform;

        public LambdaEnricher(int order, Func<ChatQuery, ChatQuery> transform)
        {
            Order = order;
            _transform = transform;
        }

        public int Order { get; }

        public ValueTask<ChatQuery> EnrichAsync(ChatQuery query, CancellationToken cancellationToken = default) =>
            new(_transform(query));
    }
}

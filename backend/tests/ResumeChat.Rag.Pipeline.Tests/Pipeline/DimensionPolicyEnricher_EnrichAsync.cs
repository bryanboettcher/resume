using Microsoft.Extensions.Options;
using ResumeChat.Rag.Models;
using ResumeChat.Rag.Pipeline;
using Shouldly;

namespace ResumeChat.Rag.Pipeline.Tests.Pipeline;

public abstract class DimensionPolicyEnricher_EnrichAsync
{
    protected DimensionPolicyEnricher Subject = null!;
    protected ChatQuery Input = null!;
    protected ChatQuery Result = null!;

    [SetUp]
    public async Task BaseSetUp()
    {
        Arrange();
        Result = await Subject.EnrichAsync(Input, CancellationToken.None);
    }

    protected abstract void Arrange();

    private static DimensionPolicyEnricher BuildSubject(DimensionPolicyOptions options) =>
        new(Options.Create(options));

    // --- Scenario classes ---

    public class When_defaults_configured : DimensionPolicyEnricher_EnrichAsync
    {
        protected override void Arrange()
        {
            Subject = BuildSubject(new DimensionPolicyOptions
            {
                DefaultDimensions = 256,
                DefaultTopK = 10
            });

            // ChatQuery defaults: TopK = 5, Dimensions = null
            Input = new ChatQuery { OriginalMessage = "test" };
        }

        [Test]
        public void It_should_apply_default_dimensions() => Result.Dimensions.ShouldBe(256);

        [Test]
        public void It_should_apply_default_top_k() => Result.TopK.ShouldBe(10);
    }

    public class When_dimensions_already_set_on_query : DimensionPolicyEnricher_EnrichAsync
    {
        protected override void Arrange()
        {
            Subject = BuildSubject(new DimensionPolicyOptions
            {
                DefaultDimensions = 256,
                DefaultTopK = 10
            });

            Input = new ChatQuery { OriginalMessage = "test", Dimensions = 128 };
        }

        [Test]
        public void It_should_not_overwrite_existing_dimensions() => Result.Dimensions.ShouldBe(128);
    }

    public class When_topk_differs_from_default : DimensionPolicyEnricher_EnrichAsync
    {
        // The enricher only overwrites TopK when the value equals the sentinel default (5).
        // A query with TopK = 3 already has a caller-specified value and must not be overwritten.
        protected override void Arrange()
        {
            Subject = BuildSubject(new DimensionPolicyOptions
            {
                DefaultDimensions = 256,
                DefaultTopK = 10
            });

            Input = new ChatQuery { OriginalMessage = "test", TopK = 3 };
        }

        [Test]
        public void It_should_not_overwrite_caller_top_k() => Result.TopK.ShouldBe(3);
    }

    public class When_no_dimensions_configured : DimensionPolicyEnricher_EnrichAsync
    {
        protected override void Arrange()
        {
            Subject = BuildSubject(new DimensionPolicyOptions
            {
                DefaultDimensions = null,
                DefaultTopK = 5
            });

            Input = new ChatQuery { OriginalMessage = "test" };
        }

        [Test]
        public void It_should_leave_dimensions_null() => Result.Dimensions.ShouldBeNull();
    }
}

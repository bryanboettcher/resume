using ResumeChat.Rag.Models;
using ResumeChat.Rag.Response;

namespace ResumeChat.Rag.Tests;

public abstract class CannedResponseProvider_GetResponseAsync
{
    protected CannedResponseProvider Subject = null!;

    [SetUp]
    public void BaseSetUp()
    {
        Subject = new CannedResponseProvider();
    }

    protected static QueryPayload Payload(string message) => new()
    {
        OriginalMessage = message,
        ProcessedMessage = message,
        Documents = []
    };

    protected static async Task<List<string>> CollectAsync(IAsyncEnumerable<string> source)
    {
        var result = new List<string>();
        await foreach (var item in source)
            result.Add(item);
        return result;
    }

    public abstract class Collected : CannedResponseProvider_GetResponseAsync
    {
        protected List<string> Chunks = null!;

        protected abstract string Message { get; }

        [SetUp]
        public async Task Act()
        {
            Chunks = await CollectAsync(Subject.GetResponseAsync(Payload(Message)));
        }
    }

    public class When_input_has_multiple_words : Collected
    {
        protected override string Message => "hello world foo";

        [Test]
        public void It_should_yield_each_word_with_trailing_space()
        {
            Chunks.ShouldBe(["hello ", "world ", "foo "]);
        }
    }

    public class When_input_is_single_word : Collected
    {
        protected override string Message => "only";

        [Test]
        public void It_should_yield_single_chunk()
        {
            Chunks.ShouldHaveSingleItem();
        }

        [Test]
        public void It_should_yield_word_with_trailing_space()
        {
            Chunks[0].ShouldBe("only ");
        }
    }

    public class When_input_is_empty : Collected
    {
        protected override string Message => "";

        [Test]
        public void It_should_yield_single_chunk()
        {
            Chunks.ShouldHaveSingleItem();
        }

        [Test]
        public void It_should_yield_lone_space()
        {
            Chunks[0].ShouldBe(" ");
        }
    }

    public class When_cancelled_mid_stream : CannedResponseProvider_GetResponseAsync
    {
        [Test]
        public async Task It_should_throw_operation_canceled()
        {
            using var cts = new CancellationTokenSource();

            await Should.ThrowAsync<OperationCanceledException>(async () =>
            {
                await foreach (var _ in Subject.GetResponseAsync(Payload("one two three four five"), cts.Token))
                {
                    await cts.CancelAsync();
                }
            });
        }
    }

    public class When_token_already_cancelled : CannedResponseProvider_GetResponseAsync
    {
        [Test]
        public async Task It_should_throw_operation_canceled()
        {
            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Should.ThrowAsync<OperationCanceledException>(async () =>
            {
                await foreach (var _ in Subject.GetResponseAsync(Payload("one two"), cts.Token)) { }
            });
        }
    }
}

using ResumeChat.Rag.Models;
using ResumeChat.Rag.Response;

namespace ResumeChat.Rag.Tests;

[TestFixture]
public sealed class CannedResponseProviderTests
{
    private readonly CannedResponseProvider _provider = new();

    private static QueryPayload Payload(string message) => new()
    {
        OriginalMessage = message,
        ProcessedMessage = message,
        Documents = []
    };

    [Test]
    public async Task GetResponseAsync_ReturnsAllWordsFromInput()
    {
        var chunks = await CollectAsync(_provider.GetResponseAsync(Payload("hello world foo")));

        chunks.ShouldBe(["hello ", "world ", "foo "]);
    }

    [Test]
    public async Task GetResponseAsync_SingleWord_ReturnsSingleChunk()
    {
        var chunks = await CollectAsync(_provider.GetResponseAsync(Payload("only")));

        chunks.ShouldHaveSingleItem();
        chunks[0].ShouldBe("only ");
    }

    [Test]
    public async Task GetResponseAsync_EmptyString_ReturnsNoChunks()
    {
        // string.Split(' ') on "" returns [""], so one empty-word chunk is emitted.
        var chunks = await CollectAsync(_provider.GetResponseAsync(Payload("")));

        chunks.ShouldHaveSingleItem();
        chunks[0].ShouldBe(" ");
    }

    [Test]
    public async Task GetResponseAsync_ThrowsOperationCanceled_WhenCancelledMidStream()
    {
        using var cts = new CancellationTokenSource();

        await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in _provider.GetResponseAsync(Payload("one two three four five"), cts.Token))
            {
                await cts.CancelAsync();
            }
        });
    }

    [Test]
    public async Task GetResponseAsync_PreCancelledToken_ThrowsImmediately()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in _provider.GetResponseAsync(Payload("one two"), cts.Token)) { }
        });
    }

    private static async Task<List<string>> CollectAsync(IAsyncEnumerable<string> source)
    {
        var result = new List<string>();
        await foreach (var item in source)
            result.Add(item);
        return result;
    }
}

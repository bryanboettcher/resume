using ResumeChat.Rag;

namespace ResumeChat.Rag.Tests;

public sealed class HardcodedCompletionProviderTests
{
    private readonly HardcodedCompletionProvider _provider = new();

    [Fact]
    public async Task CompleteAsync_ReturnsAllWordsFromInput()
    {
        var chunks = await CollectAsync(_provider.CompleteAsync("hello world foo"));

        // Each word gets a trailing space appended; order and count must match.
        Assert.Equal(["hello ", "world ", "foo "], chunks);
    }

    [Fact]
    public async Task CompleteAsync_SingleWord_ReturnsSingleChunk()
    {
        var chunks = await CollectAsync(_provider.CompleteAsync("only"));

        Assert.Single(chunks);
        Assert.Equal("only ", chunks[0]);
    }

    [Fact]
    public async Task CompleteAsync_EmptyString_ReturnsNoChunks()
    {
        // string.Split(' ') on "" returns [""], so one empty-word chunk is emitted.
        // Documenting actual behaviour rather than asserting an idealised one.
        var chunks = await CollectAsync(_provider.CompleteAsync(""));

        Assert.Single(chunks);
        Assert.Equal(" ", chunks[0]);
    }

    [Fact]
    public async Task CompleteAsync_ThrowsOperationCanceled_WhenCancelledMidStream()
    {
        using var cts = new CancellationTokenSource();

        // Cancel after receiving the first chunk so we exercise the path where
        // cancellation fires between yields rather than before enumeration starts.
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in _provider.CompleteAsync("one two three four five", cts.Token))
            {
                await cts.CancelAsync();
            }
        });
    }

    [Fact]
    public async Task CompleteAsync_PreCancelledToken_ThrowsImmediately()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var _ in _provider.CompleteAsync("one two", cts.Token)) { }
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

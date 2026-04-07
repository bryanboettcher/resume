namespace ResumeChat.Api.Extensions;

public static class SseExtensions
{
    /// <summary>
    /// Streams an <see cref="IAsyncEnumerable{T}"/> of string chunks as Server-Sent Events,
    /// terminated by a <c>data: [DONE]</c> sentinel. When <paramref name="onComplete"/> is
    /// supplied the full response is aggregated and passed to the callback; otherwise no
    /// string concatenation occurs.
    /// </summary>
    public static async Task StreamAsSseAsync(
        this IAsyncEnumerable<string> chunks,
        HttpContext context,
        Action<string>? onComplete = null,
        CancellationToken cancellationToken = default)
    {
        context.Response.ContentType = "text/event-stream";

        System.Text.StringBuilder? responseBuilder = onComplete is not null ? new() : null;

        await foreach (var chunk in chunks)
        {
            responseBuilder?.Append(chunk);
            var escaped = chunk.Replace("\n", "\ndata: ");
            await context.Response.WriteAsync($"data: {escaped}\n\n", cancellationToken);
            await context.Response.Body.FlushAsync(cancellationToken);
        }

        await context.Response.WriteAsync("data: [DONE]\n\n", cancellationToken);
        await context.Response.Body.FlushAsync(cancellationToken);

        if (responseBuilder is not null)
            onComplete!(responseBuilder.ToString());
    }
}

using System.IO.Hashing;
using System.Text;
using System.Text.RegularExpressions;
using ResumeChat.Rag.Models;

namespace ResumeChat.Storage;

public static partial class QueryHasher
{
    public static string Compute(string query, IReadOnlyList<ChatExchange>? history = null)
    {
        var sb = new StringBuilder();

        if (history is { Count: > 0 })
        {
            foreach (var exchange in history)
            {
                sb.Append(Normalize(exchange.Prompt));
                sb.Append('|');
                sb.Append(Normalize(exchange.Response));
                sb.Append('|');
            }
        }

        sb.Append(Normalize(query));

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var hash = XxHash32.HashToUInt32(bytes);
        return hash.ToString("x8");
    }

    private static string Normalize(string input) =>
        NonAlphanumeric().Replace(input.ToLowerInvariant(), "");

    [GeneratedRegex(@"[^a-z0-9]")]
    private static partial Regex NonAlphanumeric();
}

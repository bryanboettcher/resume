using System.IO.Hashing;
using System.Text.RegularExpressions;

namespace ResumeChat.Storage;

public static partial class QueryHasher
{
    public static string Compute(string query)
    {
        var normalized = NonAlphanumeric().Replace(query.ToLowerInvariant(), "");
        var bytes = System.Text.Encoding.UTF8.GetBytes(normalized);
        var hash = XxHash32.HashToUInt32(bytes);
        return hash.ToString("x8");
    }

    [GeneratedRegex(@"[^a-z0-9]")]
    private static partial Regex NonAlphanumeric();
}

using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ResumeChat.Rag.Completion;
using ResumeChat.Rag.Models;

namespace ResumeChat.Rag.Response;

public sealed class ClaudeCliResponseProvider : ResponseProviderBase
{
    private readonly ClaudeCliResponseOptions _options;
    private readonly CompletionSecurityOptions _security;

    public ClaudeCliResponseProvider(
        IOptions<ClaudeCliResponseOptions> options,
        IOptions<CompletionSecurityOptions> security,
        ILogger<ClaudeCliResponseProvider> logger)
        : base(logger)
    {
        _options = options.Value;
        _security = security.Value;
    }

    protected override string ProviderName => "ClaudeCli";
    protected override string ModelName => _options.Model;

    protected override async IAsyncEnumerable<string> StreamTokensAsync(
        QueryPayload payload,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var systemPrompt = SystemPromptBuilder.Build(payload, _security.Canary);

        // Build conversation as a single prompt with history context
        var userMessage = payload.OriginalMessage;
        if (payload.History is { Count: > 0 })
        {
            var parts = new List<string>();
            foreach (var exchange in payload.History)
            {
                parts.Add($"User: {exchange.Prompt}");
                parts.Add($"Assistant: {exchange.Response}");
            }
            parts.Add($"User: {userMessage}");
            userMessage = string.Join("\n\n", parts);
        }

        var psi = new ProcessStartInfo
        {
            FileName = _options.ClaudePath,
            ArgumentList =
            {
                "-p",
                "--model", _options.Model,
                "--tools", "",
                "--system-prompt", systemPrompt
            },
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start claude CLI process");

        // Write user message to stdin and close
        await process.StandardInput.WriteAsync(userMessage);
        process.StandardInput.Close();

        // Stream stdout in chunks
        var buffer = new char[256];
        int bytesRead;
        while ((bytesRead = await process.StandardOutput.ReadAsync(buffer, cancellationToken)) > 0)
        {
            yield return new string(buffer, 0, bytesRead);
        }

        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            var stderr = await process.StandardError.ReadToEndAsync(cancellationToken);
            throw new InvalidOperationException(
                $"claude CLI exited with code {process.ExitCode}: {stderr}");
        }
    }
}

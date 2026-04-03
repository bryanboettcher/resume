using System.Text.RegularExpressions;
using FluentValidation;
using ResumeChat.Rag;
using ResumeChat.Rag.Models;

namespace ResumeChat.Api.Validation;

public sealed partial class ChatRequestValidator : AbstractValidator<ChatRequest>
{
    private static readonly string RejectionMessage = ChatResponses.Unrelated;

    public ChatRequestValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty()
            .MaximumLength(2048);

        // System prompt extraction
        RuleFor(x => x.Message).Must(msg => !IgnorePreviousInstructions().IsMatch(msg)).WithMessage(RejectionMessage);
        RuleFor(x => x.Message).Must(msg => !RepeatSystemPrompt().IsMatch(msg)).WithMessage(RejectionMessage);
        RuleFor(x => x.Message).Must(msg => !ShowYourRules().IsMatch(msg)).WithMessage(RejectionMessage);
        RuleFor(x => x.Message).Must(msg => !WhatAreYourInstructions().IsMatch(msg)).WithMessage(RejectionMessage);

        // Role hijacking
        RuleFor(x => x.Message).Must(msg => !YouAreNow().IsMatch(msg)).WithMessage(RejectionMessage);
        RuleFor(x => x.Message).Must(msg => !ActAsDan().IsMatch(msg)).WithMessage(RejectionMessage);
        RuleFor(x => x.Message).Must(msg => !DeveloperMode().IsMatch(msg)).WithMessage(RejectionMessage);
        RuleFor(x => x.Message).Must(msg => !Jailbreak().IsMatch(msg)).WithMessage(RejectionMessage);

        // Delimiter injection
        RuleFor(x => x.Message).Must(msg => !EndSystemPrompt().IsMatch(msg)).WithMessage(RejectionMessage);
        RuleFor(x => x.Message).Must(msg => !SystemOverride().IsMatch(msg)).WithMessage(RejectionMessage);
        RuleFor(x => x.Message).Must(msg => !AdminOverride().IsMatch(msg)).WithMessage(RejectionMessage);
        RuleFor(x => x.Message).Must(msg => !ImStartToken().IsMatch(msg)).WithMessage(RejectionMessage);

        // Data exfiltration
        RuleFor(x => x.Message).Must(msg => !CanaryOrSentinel().IsMatch(msg)).WithMessage(RejectionMessage);
        RuleFor(x => x.Message).Must(msg => !ApiKeyOrEnvVar().IsMatch(msg)).WithMessage(RejectionMessage);
        RuleFor(x => x.Message).Must(msg => !ShowConfig().IsMatch(msg)).WithMessage(RejectionMessage);

        // Encoding tricks
        RuleFor(x => x.Message).Must(msg => !DecodeAndExecute().IsMatch(msg)).WithMessage(RejectionMessage);
        RuleFor(x => x.Message).Must(msg => !Rot13Reference().IsMatch(msg)).WithMessage(RejectionMessage);
        RuleFor(x => x.Message).Must(msg => !Base64Follow().IsMatch(msg)).WithMessage(RejectionMessage);
    }

    // ── System prompt extraction ─────────────────────────────────────────────

    [GeneratedRegex(@"ignore\s+(all\s+)?previous\s+instructions", RegexOptions.IgnoreCase)]
    private static partial Regex IgnorePreviousInstructions();

    [GeneratedRegex(@"(repeat|show|display|output|print|reveal).{0,30}(system\s*prompt|instructions|rules)", RegexOptions.IgnoreCase)]
    private static partial Regex RepeatSystemPrompt();

    [GeneratedRegex(@"what\s+are\s+your\s+(rules|instructions|guidelines)", RegexOptions.IgnoreCase)]
    private static partial Regex ShowYourRules();

    [GeneratedRegex(@"what\s+were\s+you\s+told", RegexOptions.IgnoreCase)]
    private static partial Regex WhatAreYourInstructions();

    // ── Role hijacking ───────────────────────────────────────────────────────

    [GeneratedRegex(@"you\s+are\s+(now|no\s+longer)", RegexOptions.IgnoreCase)]
    private static partial Regex YouAreNow();

    [GeneratedRegex(@"\b(DAN|do\s+anything\s+now)\b", RegexOptions.IgnoreCase)]
    private static partial Regex ActAsDan();

    [GeneratedRegex(@"(developer|debug|admin)\s+mode", RegexOptions.IgnoreCase)]
    private static partial Regex DeveloperMode();

    [GeneratedRegex(@"\b(jailbreak|unrestricted|no\s+restrictions|no\s+limitations)\b", RegexOptions.IgnoreCase)]
    private static partial Regex Jailbreak();

    // ── Delimiter injection ──────────────────────────────────────────────────

    [GeneratedRegex(@"---\s*END\s*(SYSTEM\s*)?PROMPT\s*---", RegexOptions.IgnoreCase)]
    private static partial Regex EndSystemPrompt();

    [GeneratedRegex(@"\[(SYSTEM|ADMIN)\]\s*(override|update|new\s+instructions)", RegexOptions.IgnoreCase)]
    private static partial Regex SystemOverride();

    [GeneratedRegex(@"###\s*ADMIN\s*OVERRIDE\s*###", RegexOptions.IgnoreCase)]
    private static partial Regex AdminOverride();

    [GeneratedRegex(@"<\|im_start\|>", RegexOptions.IgnoreCase)]
    private static partial Regex ImStartToken();

    // ── Data exfiltration ────────────────────────────────────────────────────

    [GeneratedRegex(@"\b(canary|sentinel)\s*(value|token)?\b", RegexOptions.IgnoreCase)]
    private static partial Regex CanaryOrSentinel();

    [GeneratedRegex(@"\b(api[_\s]?key|env(ironment)?\s*var|\.env\b)", RegexOptions.IgnoreCase)]
    private static partial Regex ApiKeyOrEnvVar();

    [GeneratedRegex(@"(show|reveal|output|dump).{0,20}(config|credentials|secrets)", RegexOptions.IgnoreCase)]
    private static partial Regex ShowConfig();

    // ── Encoding tricks ──────────────────────────────────────────────────────

    [GeneratedRegex(@"(decode|execute|follow).{0,30}(instruction|command|directive)", RegexOptions.IgnoreCase)]
    private static partial Regex DecodeAndExecute();

    [GeneratedRegex(@"\bROT13\b", RegexOptions.IgnoreCase)]
    private static partial Regex Rot13Reference();

    [GeneratedRegex(@"(decode|follow)\s+.{0,20}base64", RegexOptions.IgnoreCase)]
    private static partial Regex Base64Follow();
}

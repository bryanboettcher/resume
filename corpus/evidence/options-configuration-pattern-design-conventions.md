---
title: Options Configuration Pattern — Design Conventions (Section Names, Hierarchy, Validation)
tags: [options-pattern, configuration, aspnet-core, csharp, validate-on-start, data-annotations, dependency-injection, direct-mail, rag-pipeline]
related:
  - evidence/options-configuration-pattern.md
  - evidence/options-configuration-pattern-advanced.md
  - evidence/dependency-injection-composition.md
  - projects/call-trader-madera.md
  - projects/resume-chatbot.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/options-configuration-pattern.md
---

# Options Configuration Pattern — Design Conventions

Bryan's codebases apply consistent conventions to options classes: colocated section name constants, hierarchical binding, fail-fast validation, and conditional registration. These conventions appear across the Madera direct mail platform, KbStore e-commerce platform, and the Resume Chat RAG pipeline.

---

## Evidence: Static Section Name Convention

Every options class colocates its configuration section name as a `static` constant on the class itself, rather than scattering section names through registration code. The Madera codebase uses a `static string Section` property (matching the `IConfigurationSectionProvider` interface), while the Resume Chat codebase uses a `const string SectionName` field:

```csharp
// Madera pattern — static property via interface constraint
public sealed class ConvosoOptions : IConnectionStringProvider, IPipelineConfiguration,
    IConfigurationSectionProvider, ICloneable
{
    public static string Section => "Convoso";
    // ...
}

// Resume Chat pattern — const field
public sealed class OllamaEmbeddingOptions
{
    public const string SectionName = "Ollama:Embedding";
    // ...
}
```

The Madera `IConfigurationSectionProvider` interface enables the generic `DataflowsRegistry.Configure<TPipelineData, TOptions, TRuntimeData>()` method to bind options without knowing section names at compile time:

```csharp
services.AddOptions<TOptions>()
        .BindConfiguration(TOptions.Section);
```

This generic constraint (`where TOptions : class, IPipelineConfiguration, IConfigurationSectionProvider`) means each dataflow domain (Convoso, DirectMail, Ringba, Dispos) provides its own section name through its options class. The registry never hard-codes a section string — the type system carries that information.

---

## Evidence: Colon-Separated Hierarchical Sections

The Resume Chat options demonstrate hierarchical section binding using colon-separated paths. A single `Ollama` configuration block in `appsettings.json` feeds two separate options classes:

```csharp
public sealed class OllamaEmbeddingOptions
{
    public const string SectionName = "Ollama:Embedding";
    // BaseUrl, Model
}

public sealed class OllamaCompletionOptions
{
    public const string SectionName = "Ollama:Completion";
    // BaseUrl, Model
}
```

This maps to configuration like `Ollama__Embedding__BaseUrl` in environment variables, letting embedding and completion services each have independent URLs and models while sharing a logical namespace.

---

## Evidence: DataAnnotations with ValidateOnStart

The Resume Chat API applies `ValidateDataAnnotations()` and `ValidateOnStart()` to every options registration. This catches missing or invalid configuration at application startup rather than at first use:

```csharp
// From WebApplicationBuilderExtensions.cs — every options binding follows this chain
builder.Services.AddOptions<ApiKeyOptions>()
    .BindConfiguration(ApiKeyOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

The options classes use `[Required]` and `[MinLength(1)]` annotations:

```csharp
public sealed class ApiKeyOptions
{
    public const string SectionName = "ApiKey";

    [Required]
    [MinLength(1)]
    public string Key { get; set; } = string.Empty;
}
```

The `= string.Empty` default combined with `[Required, MinLength(1)]` means the app will start only if a real value is bound from configuration. A Kubernetes pod missing an environment variable crashes on startup instead of serving requests that silently fail on first use.

---

## Evidence: Conditional Validation by Provider

The Resume Chat completion provider registration only validates the options for the provider that is actually selected:

```csharp
var completionProvider = builder.Configuration["Completion:Provider"];
switch (completionProvider)
{
    case "Claude":
        builder.Services.AddOptions<ClaudeCompletionOptions>()
            .BindConfiguration(ClaudeCompletionOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        builder.Services.AddHttpClient<ICompletionProvider, ClaudeCompletionProvider>();
        break;

    case "Ollama":
        builder.Services.AddOptions<OllamaCompletionOptions>()
            .BindConfiguration(OllamaCompletionOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        builder.Services.AddHttpClient<ICompletionProvider, OllamaCompletionProvider>();
        break;

    default:
        builder.Services.AddSingleton<ICompletionProvider, HardcodedCompletionProvider>();
        break;
}
```

A development environment running Ollama does not need a Claude API key in its configuration, and vice versa. Only the actually-used provider's options are validated.

---

## Key Files

- `resume:backend/src/ResumeChat.Api/Extensions/WebApplicationBuilderExtensions.cs` — ValidateDataAnnotations + ValidateOnStart chain for all RAG options
- `resume:backend/src/ResumeChat.Api/Options/ApiKeyOptions.cs` — DataAnnotations validation with [Required, MinLength(1)]
- `resume:backend/src/ResumeChat.Rag/Embedding/OllamaEmbeddingOptions.cs` — Hierarchical section name "Ollama:Embedding"
- `madera-apps:Madera/Madera.Dataflows.Convoso/ConvosoRegistry.cs` — Per-dataflow options class with IConfigurationSectionProvider
- `madera-apps:Madera/Madera.Dataflows.Common/Registries/DataflowsRegistry.cs` — Generic options binding via TOptions.Section constraint

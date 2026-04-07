---
title: "Microsoft.Extensions.Options Configuration Pattern"
tags: [options-pattern, configuration, aspnet-core, csharp, validate-on-start, data-annotations, dependency-injection, sensitive-data, direct-mail, rag-pipeline]
children:
  - evidence/options-configuration-pattern-design-conventions.md
  - evidence/options-configuration-pattern-advanced.md
related:
  - evidence/dependency-injection-composition.md
  - evidence/dependency-injection-composition-options-pattern.md
  - evidence/dependency-injection-composition-aspire-extension-methods.md
  - projects/call-trader-madera.md
  - projects/resume-chatbot.md
category: evidence
contact: resume@bryanboettcher.com
---

# Microsoft.Extensions.Options Configuration Pattern — Index

Bryan's codebases use the `Microsoft.Extensions.Options` pattern as the primary mechanism for binding configuration to strongly-typed classes across three production systems: the Madera direct mail platform (Call-Trader, 14 projects), the KbStore e-commerce platform, and the Resume Chat RAG pipeline. The pattern appears in 15+ options classes spanning infrastructure (bus transport, message data storage), domain concerns (per-dataflow ETL pipeline settings, address normalization), authentication (JWT, API keys), and AI/RAG services (embedding, vector store, completion providers).

Design principles applied consistently:

1. **Fail-fast validation**: `ValidateOnStart()` on every options binding ensures misconfigured deployments crash immediately rather than failing on first request.
2. **Interface constraints on options types**: Madera's `IConfigurationSectionProvider`, `IConnectionStringProvider`, and `IPipelineConfiguration` interfaces let generic registration code operate polymorphically without knowing concrete section names.
3. **Safe observability**: The `ICloneable` + `[SensitiveData]` + `Sanitize()` convention across all Madera options classes means the configuration dump endpoint never leaks secrets.
4. **Conditional registration**: Only the selected completion provider's options are registered and validated, preventing startup failures from unrelated missing configuration.

The full evidence is split into focused documents:

## Child Documents

- **[Design Conventions](options-configuration-pattern-design-conventions.md)** — Static section name constants colocated on options classes (`IConfigurationSectionProvider` interface in Madera, `const string SectionName` in Resume Chat). Colon-separated hierarchical binding for `Ollama:Embedding` / `Ollama:Completion`. DataAnnotations + `ValidateOnStart()` fail-fast pattern with `[Required, MinLength(1)]`. Conditional validation by provider: only the active completion provider's options are validated.

- **[Advanced Usage](options-configuration-pattern-advanced.md)** — `[SensitiveData]` + `ICloneable` convention for safe configuration dumps with `SanitizeTypes` enum. `IConfigureOptions<T>` for complex binding scenarios. `IConfigureNamedOptions<JwtBearerOptions>` for JWT configuration with injected dependencies. Options consumption in MassTransit `ConsumerDefinition` (options driving batch size, rate limits, and retry policies). Resolve-time factory lambdas selecting implementations from options values. Options class inheritance (`AddressNormalizerOptions` base, `LobNormalizerOptions` derived).

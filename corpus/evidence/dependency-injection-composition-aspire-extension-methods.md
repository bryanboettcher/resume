---
title: DI Composition — Extension Method Composition and .NET Aspire
tags: [dependency-injection, extension-methods, aspnet-core, aspire, kbstore, csharp, rag, completion-provider, config-driven]
related:
  - evidence/dependency-injection-composition.md
  - evidence/dependency-injection-composition-registry-pattern.md
  - evidence/dependency-injection-composition-options-pattern.md
  - evidence/aspnet-minimal-api-patterns.md
  - projects/kbstore-ecommerce.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/dependency-injection-composition.md
---

# DI Composition — Extension Method Composition and .NET Aspire

Two smaller-scale examples of DI composition: the Resume Chat API uses extension method chaining with config-driven completion provider selection for a focused single-service application, and KbStore uses .NET Aspire's `DistributedApplication` builder to compose multi-service infrastructure declaratively.

---

## Evidence: Resume Chat — Extension Method Composition

The resume chatbot API (`ResumeChat.Api/Extensions/WebApplicationBuilderExtensions.cs`, 86 lines) shows a simpler variant of the same principles for a smaller application. `AddApplicationServices()` orchestrates subsystem registration:

```csharp
public static WebApplicationBuilder AddApplicationServices(this WebApplicationBuilder builder)
{
    builder.Services.AddOptions<ApiKeyOptions>()
        .BindConfiguration(ApiKeyOptions.SectionName)
        .ValidateDataAnnotations()
        .ValidateOnStart();

    builder.Services.AddResumeChatRateLimiting(builder.Configuration);
    builder.AddRagServices();
    return builder;
}
```

`AddRagServices()` registers the entire RAG pipeline (embedding, vector store, chunking, ingestion, retrieval) and uses config-driven provider selection for the completion backend — `"Claude"`, `"Ollama"`, or default hardcoded — including conditional `ValidateOnStart()` so only the selected provider's configuration is validated.

---

## Evidence: .NET Aspire Composition in KbStore

KbStore (`kb-platform/src/infrastructure/KbStore.AppHost/Program.cs`) uses .NET Aspire's `DistributedApplication` builder to compose infrastructure and service dependencies declaratively. Rather than wiring DI manually, the AppHost defines resources and their relationships:

```csharp
var catalogService = builder.AddProject<KbStore_Catalog>("domain-catalog")
    .WithReference(broker)
    .WithReference(databaseCatalog)
    .WaitFor(broker)
    .WaitFor(databaseCatalog);
```

The domain services use `ServiceCollectionExtensions` for their internal DI. KbStore's Storefront domain (`KbStore.Storefront.Services/Extensions/ServiceCollectionExtensions.cs`) includes a `useMocks` parameter that swaps the entire dependency graph between mock and real implementations, with an optional `seedTestData` flag that registers a `TestDataSeederHostedService`.

---

## Key Files

- `resume (this repo):backend/src/ResumeChat.Api/Extensions/WebApplicationBuilderExtensions.cs` — Config-driven completion provider selection with conditional ValidateOnStart
- `kb-platform:src/infrastructure/KbStore.AppHost/Program.cs` — .NET Aspire distributed application composition
- `kb-platform:src/services/KbStore.Storefront.Services/Extensions/ServiceCollectionExtensions.cs` — Mock/real implementation swapping via registration parameter

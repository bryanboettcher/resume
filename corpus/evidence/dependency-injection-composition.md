---
title: Dependency Injection Composition & Service Registration Patterns
tags: [dependency-injection, ioc, composition-root, service-registration, lamar, aspnet-core, masstransit, options-pattern, assembly-scanning, aspire]
related:
  - evidence/dotnet-csharp-expertise.md
  - evidence/distributed-systems-architecture.md
  - projects/call-trader-madera.md
  - projects/kbstore-ecommerce.md
children:
  - evidence/dependency-injection-composition-registry-pattern.md
  - evidence/dependency-injection-composition-assembly-scanner.md
  - evidence/dependency-injection-composition-generic-pipeline.md
  - evidence/dependency-injection-composition-options-pattern.md
  - evidence/dependency-injection-composition-aspire-extension-methods.md
category: evidence
contact: resume@bryanboettcher.com
---

# Dependency Injection Composition & Service Registration Patterns — Index

In applications with dozens of services, data pipelines, message transports, and cross-cutting concerns, the composition root — where all dependencies get wired together — is where architectural decisions become concrete. A poorly organized composition root turns into an unreadable wall of `services.AddSingleton<>()` calls. Bryan's codebases show a disciplined, layered approach to DI composition across two production systems: the Madera direct mail platform (14 projects, 625+ source files) and the KbStore e-commerce platform (DDD bounded contexts with .NET Aspire).

The full evidence is split into focused documents:

## Child Documents

- **[Registry Pattern in Madera](dependency-injection-composition-registry-pattern.md)** — A hierarchy of static `Registry` classes, each owning a bounded subsystem. The workflow host and web server host compose different registry subsets from the same codebase. Covers the 12 distinct registries and their responsibilities.

- **[Custom Assembly Scanner](dependency-injection-composition-assembly-scanner.md)** — A custom `ScanAssembly` extension method (97 lines) that performs convention-based registration with namespace exclusions (`Dapper`, `Microsoft`, `MassTransit`, `Madera.Contracts`) and two attribute escape hatches: `[Lifetime]` and `[ExcludeFromScanner]`.

- **[Generic Pipeline Composition and MassTransit Partial Classes](dependency-injection-composition-generic-pipeline.md)** — `DataflowsRegistry.Configure<TPipelineData, TOptions, TRuntimeData>()` eliminates per-domain ETL boilerplate with resolve-time factory lambdas. MassTransit configuration split across a `partial class` with transport, scheduling, message data, and serialization in separate files.

- **[Config-Driven Provider Selection and Options Pattern](dependency-injection-composition-options-pattern.md)** — Switching implementations (InMemory/RabbitMQ/Azure Service Bus, local/blob message data) at registration time based on configuration. Every options class implements `ICloneable` with a sanitized `Clone()` for a safe configuration dump endpoint.

- **[Extension Method Composition and .NET Aspire](dependency-injection-composition-aspire-extension-methods.md)** — Resume Chat API using chained extension methods with conditional `ValidateOnStart()` per completion provider. KbStore using Aspire's `DistributedApplication` builder for declarative multi-service composition with `useMocks` parameter for test/real swapping.

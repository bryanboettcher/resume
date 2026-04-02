---
title: DI Composition — Registry Pattern in Madera
tags: [dependency-injection, ioc, composition-root, service-registration, aspnet-core, masstransit, registry-pattern, direct-mail, csharp]
related:
  - evidence/dependency-injection-composition.md
  - evidence/dependency-injection-composition-assembly-scanner.md
  - evidence/dependency-injection-composition-generic-pipeline.md
  - evidence/distributed-systems-architecture.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/dependency-injection-composition.md
---

# DI Composition — Registry Pattern in Madera

In applications with dozens of services, data pipelines, message transports, and cross-cutting concerns, the composition root is where architectural decisions become concrete. The Madera direct mail platform (14 projects, 625+ source files) uses a hierarchy of static `Registry` classes — each owning a bounded subsystem — rather than a monolithic startup blob.

---

## Evidence: Registry Pattern in Madera

The top-level `Program.cs` in the Workflows host (`Madera/Madera.Workflows/Program.cs`) calls registries in dependency order:

```csharp
private static void ConfigureContainer(HostBuilderContext context, IServiceCollection services)
{
    CommonRegistry.Configure(context, services);
    LobRegistry.Configure(context, services);

    services.ScanAssembly(typeof(CommonRegistry).Assembly);

    WorkflowsRegistry.Configure(context, services);
    DirectMailRegistry.Configure(context, services);
    RingbaRegistry.Configure(context, services);
    DispoRegistry.Configure(context, services);
    ConvosoRegistry.Configure(context, services);

    MigrationRegistry.Configure(context, services);

    HealthCheckRegistry.Configure(context, services);
    ObservabilityRegistry.Configure(context, services);
}
```

The web server host (`Madera/Madera.UI.Server/Program.cs`) composes a different subset for its lighter responsibilities:

```csharp
private static void ConfigureContainer(HostBuilderContext context, IServiceCollection services)
{
    CommonRegistry.Configure(context, services);
    ServerRegistry.Configure(context, services);
    ReportingRegistry.Configure(context, services);

    OpenApiRegistry.Configure(context, services);
    AuthenticationRegistry.Configure(context, services);
    HealthCheckRegistry.Configure(context, services);
    ObservabilityRegistry.Configure(context, services);
}
```

The workflow host (background jobs + message consumers) gets the full set of dataflow registries plus MassTransit backend configuration. The web server gets only UI-facing registries plus MassTransit webserver configuration. Both share cross-cutting registries like `CommonRegistry`, `HealthCheckRegistry`, and `ObservabilityRegistry`. Adding a new dataflow (e.g., `ConvosoRegistry`) means adding one line to the workflow host's `ConfigureContainer` — the registry encapsulates all of that domain's consumers, activities, futures, pipeline components, and saga state machines.

### Registry Count and Scope

The codebase contains at least 12 distinct registry classes, each with a clear responsibility boundary:

| Registry | Scope |
|---|---|
| `CommonRegistry` | Filesystem abstractions, Dapper type handlers, `ISystemClock`, `IFileSystem` decomposition |
| `ObservabilityRegistry` | OpenTelemetry tracing/metrics/logging, optional Azure Monitor |
| `HealthCheckRegistry` | SQL, AMQP, Azure, DNS health checks with config-driven dashboard |
| `ServerRegistry` | Kestrel, CORS, JSON serialization, antiforgery, MassTransit host options |
| `AuthenticationRegistry` | JWT + API Key dual auth |
| `OpenApiRegistry` | Scalar API reference, OpenAPI generation |
| `ReportingRegistry` | Reporting DbContext and services |
| `DirectMailRegistry` | Direct mail pipeline, 4 saga state machines, address normalization |
| `RingbaRegistry` | Ringba call tracking pipeline |
| `DispoRegistry` | Disposition import pipeline (with per-vendor generic registration) |
| `ConvosoRegistry` | Convoso call tracking pipeline |
| `DataflowsRegistry` | Shared generic pipeline infrastructure (source/sink/progress selection) |

When a service misbehaves, you look at the registry for that domain, not a 500-line `Program.cs`. Each registry owns its subsystem's lifetime decisions, options binding, and factory registrations.

---

## Key Files

- `madera-apps:Madera/Madera.Workflows/Program.cs` — Workflow host composition root
- `madera-apps:Madera/Madera.UI.Server/Program.cs` — Web server composition root
- `madera-apps:Madera/Madera.Dataflows.DirectMail/DirectMailRegistry.cs` — Largest domain registry (183 lines), 4 saga state machines, scripting transforms, address normalization provider selection
- `madera-apps:Madera/Madera.Common/Registries/HealthCheckRegistry.cs` — Conditional health check registration (221 lines)
- `madera-apps:Madera/Madera.Common/Registries/ObservabilityRegistry.cs` — OpenTelemetry tracing/metrics with optional Azure Monitor

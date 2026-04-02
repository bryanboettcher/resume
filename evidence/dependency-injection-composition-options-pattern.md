---
title: DI Composition — Config-Driven Provider Selection and Options Pattern
tags: [dependency-injection, options-pattern, config-driven, sensitive-data, aspnet-core, csharp, health-checks, icloneable, direct-mail]
related:
  - evidence/dependency-injection-composition.md
  - evidence/dependency-injection-composition-registry-pattern.md
  - evidence/dependency-injection-composition-aspire-extension-methods.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/dependency-injection-composition.md
---

# DI Composition — Config-Driven Provider Selection and Options Pattern

Two recurring patterns across the Madera codebase handle runtime flexibility in DI: configuration-driven provider selection that switches implementations at registration time based on settings, and an options pattern where every options class implements `ICloneable` with sanitized output for safe configuration endpoint dumping.

---

## Evidence: Config-Driven Provider Selection

A recurring pattern across the codebase is configuration-driven provider selection at DI registration time. The transport configuration (`MassTransitStartup_Transport.cs`, 144 lines) selects between InMemory, RabbitMQ, and Azure Service Bus based on `BusTransportOptions.Provider`. The scheduling configuration selects between Hangfire and transport-level schedulers. The message data configuration selects between local filesystem and Azure Blob Storage.

Each provider selection follows the same shape: read an options object, switch on an enum, instantiate the appropriate implementation. The health check registry (`HealthCheckRegistry.cs`, 221 lines) takes this further by conditionally registering checks based on which configuration sections exist — SQL databases, AMQP, Azure Storage, DNS for external APIs — using local functions for each check type:

```csharp
Mssql("Madera", "Madera:ConnectionString");
Mssql("DirectMail", "DirectMail:ConnectionString");
// ...
switch (config.GetValue<string>("Transport:Provider"))
{
    case "RabbitMq": Amqp("RabbitMq", "Transport:ConnectionString"); break;
    case "AzureServiceBus": ServiceBus("AzureServiceBus", "Transport:ConnectionString"); break;
}
```

Each local function checks for empty connection strings before registering, so the health check set adapts to whatever infrastructure is actually configured.

---

## Evidence: Options Pattern with Sensitive Data Protection

Every options class implements `ICloneable` with a `Clone()` method that sanitizes sensitive data. This enables safe configuration dumping (there is a `MapConfigurationEndpoint()` on both hosts) without leaking connection strings or API keys:

```csharp
public sealed class RingbaOptions : IConnectionStringProvider, IPipelineConfiguration, IConfigurationSectionProvider, ICloneable
{
    [SensitiveData]
    public required string ConnectionString { get; init; }
    [SensitiveData]
    public required string AccountId { get; init; }
    [SensitiveData]
    public required string UserToken { get; init; }
    
    public object Clone()
    {
        return new
        {
            ConnectionString = ConnectionString.Sanitize(SanitizeTypes.ConnectionString),
            AccountId = AccountId.Sanitize(SanitizeTypes.Default),
            UserToken = UserToken.Sanitize(SanitizeTypes.ApiKey),
            // ... non-sensitive fields pass through unchanged
        };
    }
}
```

Options classes also implement interface constraints (`IConnectionStringProvider`, `IPipelineConfiguration`, `IConfigurationSectionProvider`) that the generic `DataflowsRegistry` leverages. The `IConfigurationSectionProvider` interface exposes `static string Section` for self-binding via `BindConfiguration(TOptions.Section)`, keeping section names colocated with their options types rather than scattered across startup code.

---

## Key Files

- `madera-apps:Madera/Madera.Common/Registries/HealthCheckRegistry.cs` — Conditional health check registration (221 lines)
- `madera-apps:Madera/Madera.Common/Startup/MassTransitStartup_Transport.cs` — Config-driven bus transport selection

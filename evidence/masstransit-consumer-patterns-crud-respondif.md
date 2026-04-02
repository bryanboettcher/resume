---
title: MassTransit Consumer Patterns — CRUD Consumers and RespondIf
tags: [masstransit, consumers, csharp, message-driven, dapper, crud, dual-use, respondif, sealed-class]
related:
  - evidence/masstransit-consumer-patterns.md
  - evidence/masstransit-consumer-patterns-consumer-definitions.md
  - evidence/masstransit-consumer-patterns-job-consumers.md
  - evidence/masstransit-consumer-patterns-advanced.md
  - evidence/masstransit-contract-design.md
  - evidence/dapper-async-data-access.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/masstransit-consumer-patterns.md
---

# MassTransit Consumer Patterns — CRUD Consumers and RespondIf

The Madera direct mail platform uses a consistent structural pattern for all reference data entity management: a single sealed class implementing multiple `IConsumer<T>` interfaces, one per CRUD operation. This document covers that pattern and the `RespondIf` extension that enables dual-use consumers.

---

## Evidence: Multi-Interface CRUD Consumer Pattern

The Madera codebase uses a consistent pattern for reference data entities (Brokers, Verticals, Publishers, MailHouses, Creatives): a single sealed class implements `IConsumer<T>` for all four CRUD+validate operations on that entity type.

```csharp
// Madera/Madera.Dataflows.DirectMail/Domains/Verticals/VerticalConsumers.cs
public sealed class VerticalConsumers
    : IConsumer<CreateVerticalRequest>
    , IConsumer<UpdateVerticalRequest>
    , IConsumer<DeleteVerticalRequest>
    , IConsumer<ValidateVerticalRequest>
```

Each of the five entity consumer classes (`BrokerConsumers`, `VerticalConsumers`, `PublisherConsumers`, `MailHouseConsumers`, `CreativeConsumers`) follows the same internal structure:

1. Constructor takes `IConnectionProvider<DirectMailOptions>` (phantom-type routed Dapper connection) and `ILogger<T>`.
2. Each `Consume` method opens a connection, performs the Dapper.Contrib operation (`InsertAsync`, `UpdateAsync`, `DeleteAsync`, `GetAsync`), publishes a domain event (`BrokerCreatedEvent`, `VerticalUpdatedEvent`, etc.), and responds to the caller.
3. Private nested `DbModel` classes annotated with `[Table("ref.Brokers")]`, `[Key]`, and `[Computed]` attributes keep the database mapping colocated with the consumer — no separate repository layer for reference data.

The Update consumers do property-level change detection before calling `UpdateAsync`:

```csharp
// Madera/Madera.Dataflows.DirectMail/Domains/Verticals/VerticalConsumers.cs
var model = await connection.GetAsync<DbVerticalModel>(msg.Id);

if (model.Name != msg.Name)
    model.Name = msg.Name;
if (model.DisplayName != msg.DisplayName)
    model.DisplayName = msg.DisplayName;
```

This avoids blind overwrites when only a subset of fields changed.

---

## Evidence: RespondIf — Dual-Use Consumer Extension

A custom `RespondIf<TResponse>` extension method on `ConsumeContext` enables consumers to work in both fire-and-forget (event-driven) and request/response modes without branching:

```csharp
// Madera/Madera.Common/Extensions/ContextExtensions.cs
public static Task RespondIf<TResponse>(this ConsumeContext context, object values)
    where TResponse : class
{
    return context.IsResponseAccepted<TResponse>()
        ? context.RespondAsync<TResponse>(values)
        : Task.CompletedTask;
}
```

Every CRUD consumer uses this pattern. When a saga sends a command via `Request`, the consumer responds with a typed result. When the same consumer is triggered by a published event (no response expected), `RespondIf` silently skips the response rather than faulting. This means one consumer class serves both the interactive API path (endpoint sends request, waits for response) and the saga orchestration path (state machine publishes command, doesn't wait).

---

## Key Files

- `madera-apps:Madera/Madera.Dataflows.DirectMail/Domains/Verticals/VerticalConsumers.cs` — representative CRUD consumer (166 lines)
- `madera-apps:Madera/Madera.Dataflows.DirectMail/Consumers/Core/BrokerConsumers.cs` — minimal CRUD consumer (148 lines)
- `madera-apps:Madera/Madera.Common/Extensions/ContextExtensions.cs` — `RespondIf` extension method

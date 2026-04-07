---
title: Dapper Data Access — Generic Connection Provider with Options-Based Routing
tags: [dapper, csharp, dependency-injection, options-pattern, multi-database, sql-server, phantom-types, direct-mail]
related:
  - evidence/dapper-async-data-access.md
  - evidence/dapper-async-data-access-type-handlers.md
  - evidence/dapper-async-data-access-saga-persistence.md
  - evidence/dependency-injection-composition.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/dapper-async-data-access.md
---

# Dapper Data Access — Generic Connection Provider with Options-Based Routing

The Madera/Call-Trader platform (madera-apps repository) uses Dapper as its primary data access layer across a system that processes 30M+ direct mail recipient records. A key infrastructure piece is a generic `IConnectionProvider<TOptions>` abstraction that routes different subsystems to their respective SQL Server databases without ambient context or named registrations.

---

## Evidence: Generic Connection Provider with Options-Based Routing

The codebase uses a generic `IConnectionProvider<TOptions>` interface to route different subsystems to their respective SQL Server databases:

```csharp
// Madera/Madera.Common/Persistence/IConnectionProvider.cs
public interface IConnectionProvider
{
    SqlConnection CreateConnection();
}

public interface IConnectionProvider<TOptions> : IConnectionProvider;
```

The generic type parameter `TOptions` acts as a phantom type — it carries no data at runtime but tells the DI container which connection string to resolve. Each subsystem binds its own options class:

- `IConnectionProvider<DirectMailOptions>` — direct mail import/scrub database
- `IConnectionProvider<RingbaOptions>` — call tracking database
- `IConnectionProvider<MaderaOptions>` — user identity and UI database

Consumers and services declare their database dependency via the specific generic variant:

```csharp
// VerticalConsumers.cs
public VerticalConsumers(
    IConnectionProvider<DirectMailOptions> provider,
    ILogger<VerticalConsumers> logger)

// SqlAccountService.cs
public SqlAccountService(
    IConnectionProvider<MaderaOptions> connectionProvider,
    IPasswordHasher<MaderaAccount> passwordHasher,
    ILogger<SqlAccountService> logger)

// RingbaReportsConsumer.cs
public RingbaReportsConsumer(
    IConnectionProvider<RingbaOptions> provider,
    ILogger<RingbaReportsConsumer> logger)
```

This means the DI container resolves the correct connection string per subsystem without any ambient context, service locator, or named registrations. The connection provider returns a `SqlConnection` (from `Microsoft.Data.SqlClient`) already configured with the connection string — callers use `await using` for disposal.

### Connection Lifetime Pattern

The codebase consistently uses short-lived connections created from `IConnectionProvider`. In consumers, the pattern is `await using var connection = _provider.CreateConnection();` — the connection is opened, used for one or more operations, and disposed at the end of the consumer method. In filter processors, where the return type is `IAsyncEnumerable<Guid>`, the connection is created but disposal is deferred to the caller consuming the enumerable (Dapper's `QueryUnbufferedAsync` manages connection lifetime internally).

---

## Key Files

- `madera-apps:Madera/Madera.Common/Persistence/IConnectionProvider.cs` — Generic connection provider interface with phantom type parameter for multi-database routing
- `madera-apps:Madera/Madera.Dataflows.DirectMail/DirectMailRegistry.cs` — Saga state machine configuration with DapperRepository
- `madera-apps:Madera/Madera.Dataflows.Ringba/Consumers/RingbaReportsConsumer.cs` — Example consumer using Ringba-specific connection provider
- `madera-apps:Madera/Madera.UI.Server/Identity/Services/SqlAccountService.cs` — Identity service using Madera-specific connection provider

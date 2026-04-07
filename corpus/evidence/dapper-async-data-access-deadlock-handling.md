---
title: Dapper Data Access — Deadlock Handling with MassTransit Retry
tags: [dapper, masstransit, sql-server, deadlock-handling, retry-policy, concurrency, csharp, direct-mail]
related:
  - evidence/dapper-async-data-access.md
  - evidence/dapper-async-data-access-saga-persistence.md
  - evidence/distributed-systems-architecture.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/dapper-async-data-access.md
---

# Dapper Data Access — Deadlock Handling with MassTransit Retry

The Madera platform converts SQL Server deadlock exceptions (error 1205) into typed exceptions that MassTransit's retry middleware can process. This pattern makes the retry boundary explicit and avoids swallowing or incorrectly retrying other SQL errors.

---

## Evidence: Deadlock Handling with MassTransit Retry

The `RingbaReportsConsumer` demonstrates a pattern for handling SQL Server deadlocks (error 1205) by converting them into exceptions that MassTransit's retry middleware can process:

```csharp
// RingbaReportsConsumer.cs
catch (SqlException e) when (e.Number == 1205)
{
    _logger.LogWarning(
        "Deadlock detected (Error 1205). Rethrowing as DBConcurrencyException for retry middleware.");
    throw new DBConcurrencyException("SQL deadlock detected. Operation will be retried.", e);
}
```

The corresponding consumer definition configures incremental retry specifically for `DBConcurrencyException`:

```csharp
public sealed class RingbaReportsConsumerDefinition : ConsumerDefinition<RingbaReportsConsumer>
{
    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<RingbaReportsConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        consumerConfigurator.UseMessageRetry(conf =>
        {
            conf.Handle<DBConcurrencyException>();
            conf.Incremental(6, TimeSpan.FromMilliseconds(500), TimeSpan.FromMilliseconds(500));
        });
    }
}
```

This retries up to 6 times with 500ms increments (500ms, 1000ms, 1500ms, ..., 3000ms). The `Handle<DBConcurrencyException>()` filter ensures only deadlocks trigger retry — other SQL exceptions propagate immediately to MassTransit's fault pipeline. The `SqlException.Number == 1205` check is specific to SQL Server's deadlock detection rather than catching all SQL exceptions, avoiding retry on errors that won't resolve by waiting.

---

## Key Files

- `madera-apps:Madera/Madera.Dataflows.Ringba/Consumers/RingbaReportsConsumer.cs` — Deadlock detection (SqlException 1205) with MassTransit incremental retry

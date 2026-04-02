---
title: MassTransit Consumer Patterns — ConsumerDefinition Endpoint Tuning
tags: [masstransit, consumers, csharp, message-driven, retry, consumer-definition, batch-consumer, rate-limiting, concurrency, deadlock]
related:
  - evidence/masstransit-consumer-patterns.md
  - evidence/masstransit-consumer-patterns-crud-respondif.md
  - evidence/masstransit-consumer-patterns-job-consumers.md
  - evidence/masstransit-consumer-patterns-advanced.md
  - evidence/masstransit-contract-design.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/masstransit-consumer-patterns.md
---

# MassTransit Consumer Patterns — ConsumerDefinition Endpoint Tuning

MassTransit's `ConsumerDefinition<T>` pattern is used extensively in Madera to configure transport-level behavior on a per-consumer basis. Every pipeline consumer has a paired definition class colocated in the same file, configuring concurrency limits, prefetch counts, retry policies, and batch options.

---

## Evidence: Deadlock-to-Retry Bridge (DataMigrationConsumer)

The `DataMigrationConsumer` processes SQL Server data migrations that can encounter deadlocks and timeouts. It wraps `SqlException` error codes into a typed exception, then configures two separate retry layers:

```csharp
// Madera/Madera.Dataflows.DirectMail/Domains/Imports/Consumers/DataMigrationConsumer.cs
catch (SqlException ex) when (ex.Number == 1205)
{
    throw new DataMigrationException(ex);  // deadlock
}
catch (SqlException ex) when (ex.Number == -2)
{
    throw new DataMigrationException(ex);  // timeout
}
```

The consumer definition then sets up two retry policies — a long interval retry for general failures and an exponential backoff specifically for `DataMigrationException`:

```csharp
// StartDataMigrationConsumerDefinition
consumerConfigurator.UseMessageRetry(conf => conf.Interval(6, TimeSpan.FromSeconds(1800)));

consumerConfigurator.UseMessageRetry(conf =>
{
    conf.Handle<DataMigrationException>();
    conf.Exponential(
        retryLimit: 5,
        minInterval: TimeSpan.FromSeconds(3),
        maxInterval: TimeSpan.FromSeconds(60),
        intervalDelta: TimeSpan.FromSeconds(3)
    );
});
```

The endpoint is locked to a single concurrent message (`ConcurrentMessageLimit = 1`, `PrefetchCount = 1`) because the migration procedures must run serially to avoid compounding the deadlock risk.

---

## Evidence: Rate-Limited Batch Consumer (NormalizeAddressConsumer)

The `NormalizeAddressConsumer` processes address normalization in batches against an external API (Lob.com) with strict rate limits. Its consumer definition dynamically calculates transport settings from the Lob configuration options:

```csharp
// NormalizeAddressConsumerDefinition
var messageLimit = current.BatchSize * current.AddressConcurrencyLimit;

endpointConfigurator.ConcurrentMessageLimit = messageLimit;
endpointConfigurator.PrefetchCount = messageLimit;

consumerConfigurator.Options<BatchOptions>(conf =>
{
    conf.MessageLimit = current.BatchSize;
    conf.ConcurrencyLimit = current.AddressConcurrencyLimit;
    conf.TimeLimit = current.AddressBatchInterval;
    conf.TimeLimitStart = BatchTimeLimitStart.FromLast;
});

endpointConfigurator.UseRateLimit(current.RateLimit, current.RateInterval);

endpointConfigurator.UseMessageRetry(conf =>
{
    conf.Handle<TooManyRequestsException>();
    conf.Interval(current.RetryLimit, current.RetryDelay);
});
```

This is a layered defense: MassTransit's batch consumer collects individual `NormalizeAddressCommand` messages into arrays of `BatchSize`, rate limiting prevents exceeding the API's quota, and `TooManyRequestsException` retry handles cases where the rate limit is still hit despite the limiter.

---

## Evidence: Incremental Retry for Data Purge (RemoveImportConsumer)

The `RemoveImportConsumer` handles import deletion events with an incremental retry policy — 20 retries starting at 2 seconds and growing by 4 seconds each attempt. This accommodates the cascading deletes in the SQL Server purge procedure which can take variable time depending on how many related records exist:

```csharp
// RemoveImportConsumerDefinition
consumerConfigurator.UseMessageRetry(conf =>
{
    conf.Incremental(20, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4));
});
```

---

## Evidence: Deadlock-to-DBConcurrencyException Bridge (RingbaReportsConsumer)

The Ringba reporting consumer uses a different deadlock strategy than the DirectMail pipeline. Instead of a custom exception type, it catches `SqlException` 1205 and rethrows as `DBConcurrencyException`, which MassTransit's retry middleware already handles by default:

```csharp
// Madera/Madera.Dataflows.Ringba/Consumers/Reporting/RingbaReportsConsumer.cs
catch (SqlException e) when (e.Number == 1205)
{
    throw new DBConcurrencyException("SQL deadlock detected. Operation will be retried.", e);
}
```

This consumer also includes a `GetRetryAttempt()` call in its logging, making retry behavior visible in structured logs without additional instrumentation.

---

## Key Files

- `madera-apps:Madera/Madera.Dataflows.DirectMail/Domains/Imports/Consumers/DataMigrationConsumer.cs` — deadlock retry bridge (103 lines)
- `madera-apps:Madera/Madera.AddressNormalization/Consumers/NormalizeAddressConsumer.cs` — rate-limited batch consumer (101 lines)
- `madera-apps:Madera/Madera.Dataflows.DirectMail/Domains/Imports/Consumers/RemoveImportConsumer.cs` — incremental retry purge consumer (48 lines)
- `madera-apps:Madera/Madera.Dataflows.Ringba/Consumers/RingbaReportsConsumer.cs` — deadlock-to-DBConcurrencyException bridge (93 lines)

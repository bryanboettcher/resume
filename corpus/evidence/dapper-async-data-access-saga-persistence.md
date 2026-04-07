---
title: Dapper Data Access — MassTransit Saga Persistence via Dapper
tags: [dapper, masstransit, saga-persistence, sql-server, csharp, concurrency, read-write-models, direct-mail]
related:
  - evidence/dapper-async-data-access.md
  - evidence/dapper-async-data-access-connection-provider.md
  - evidence/masstransit-contract-design.md
  - evidence/distributed-systems-architecture.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/dapper-async-data-access.md
---

# Dapper Data Access — MassTransit Saga Persistence via Dapper

The Madera platform configures MassTransit saga state machines to persist their state through custom Dapper-based `DatabaseContext<T>` implementations rather than EF Core's `DbContext`. This gives explicit control over transactional isolation and allows read/write model separation at the persistence layer.

---

## Evidence: MassTransit Saga Persistence via Dapper

The DirectMail registry configures MassTransit saga state machines to persist through custom Dapper contexts via MassTransit's `.DapperRepository()` extension:

```csharp
// DirectMailRegistry.cs
bus.AddSagaStateMachine<ImportStateMachine, DirectMailImport>()
   .DapperRepository(conf => {
       conf.UseContextFactory(_ => (c, t) => new ImportsDatabaseContext(c, t));
       conf.UseSqlServer(options.ConnectionString);
       conf.UseIsolationLevel(IsolationLevel.RepeatableRead);
   });

bus.AddSagaStateMachine<MailFileStateMachine, MailFile>()
   .DapperRepository(conf => {
       conf.UseContextFactory(_ => (c, t) => new MailFilesDatabaseContext(c, t));
       conf.UseSqlServer(options.ConnectionString);
       conf.UseIsolationLevel(IsolationLevel.RepeatableRead);
   });
```

The `UseContextFactory` call provides a factory that receives a `DbConnection` and `DbTransaction` and returns the custom context. Both contexts use `IsolationLevel.RepeatableRead` for saga consistency during concurrent message processing.

### DatabaseContext Implementation Pattern

`ImportsDatabaseContext` and `MailFilesDatabaseContext` follow an identical structural pattern. Each implements `LoadAsync`, `QueryAsync`, `InsertAsync`, `UpdateAsync`, `DeleteAsync`, and `CommitAsync` — the full lifecycle of a saga instance. The key design decisions:

**Separate Read and Write models.** Each context defines a private `DbModel` base class with two sealed inner classes: `DbModel.Read` (which includes joined reference data like publisher names and broker names) and `DbModel.Write` (which maps 1:1 to the saga table columns). The comment in the code — `"keep the separation for organization reasons, not technical"` — indicates this is deliberate even though Write inherits all base properties without adding any.

```csharp
// ImportsDatabaseContext.cs
private abstract class DbModel
{
    public required Guid CorrelationId { get; init; }
    public required int CurrentState { get; init; }
    // ... 25+ properties for saga state
    public required int PublisherId { get; init; }
    public required byte VerticalId { get; init; }
    public required int BrokerId { get; init; }

    public sealed class Read : DbModel
    {
        // Joined reference data from ref.Publishers, ref.Verticals, ref.Brokers
        public string PublisherName { get; init; } = "";
        public decimal? PublisherRemailCost { get; init; }
        public string VerticalName { get; init; } = "";
        // ...
    }

    public sealed class Write : DbModel { }
}
```

**SQL generation via `ISagaSqlFormatter<T>`.** The contexts use MassTransit's `SqlServerSagaFormatter<T>` to build INSERT/UPDATE/DELETE/SELECT SQL dynamically from the model type. The `MapPrefix` calls tell the formatter how to handle joined navigation properties:

```csharp
_modelBuilder = new SqlServerSagaFormatter<DirectMailImport>("data.Imports");
_modelBuilder.MapPrefix(x => x.Publisher);
_modelBuilder.MapPrefix(x => x.Vertical);
_modelBuilder.MapPrefix(x => x.Broker);
```

**Concurrency detection via row count.** Every mutating operation checks `rows == 0` after execution and throws `DapperConcurrencyException` if no rows were affected. This turns silent no-ops (stale saga state, deleted entity) into explicit failures that MassTransit's retry pipeline can handle:

```csharp
var rows = await ExecuteSql(model, sql).ConfigureAwait(false);
if (rows == 0)
    throw new DapperConcurrencyException(
        "Saga Update failed",
        typeof(DirectMailImport),
        instance.CorrelationId);
```

**Explicit mapping functions.** `FromModel(DbModel.Read)` converts database rows to saga instances (hydrating inline navigation models for Broker, Publisher, Vertical). `FromSaga(DirectMailImport)` converts saga instances to flat write models. This manual mapping avoids the abstraction cost and debugging opacity of AutoMapper while keeping the database shape decoupled from the domain model.

The `MailFilesDatabaseContext` extends this pattern to five navigation prefixes (Broker, Publisher, Vertical, MailHouse, Creative), showing the approach scales without structural changes.

---

## Key Files

- `madera-apps:Madera/Madera.Dataflows.DirectMail/DirectMailRegistry.cs` — Saga state machine configuration with DapperRepository
- `madera-apps:Madera/Madera.Dataflows.DirectMail/Domains/Imports/Persistance/ImportsDatabaseContext.cs` — Saga persistence context with Read/Write model separation, concurrency detection
- `madera-apps:Madera/Madera.Dataflows.DirectMail/Domains/MailFiles/Persistance/MailFilesDatabaseContext.cs` — Mail file saga persistence with five navigation prefixes

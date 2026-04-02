---
title: Dapper-Based Async Data Access Patterns
tags: [dapper, async, csharp, sql-server, masstransit, saga-persistence, repository-pattern, type-handlers, unbuffered-queries, table-valued-parameters, pagination, connection-management]
related:
  - evidence/sql-server-database-engineering.md
  - evidence/etl-pipeline-framework.md
children:
  - evidence/dapper-async-data-access-connection-provider.md
  - evidence/dapper-async-data-access-type-handlers.md
  - evidence/dapper-async-data-access-saga-persistence.md
  - evidence/dapper-async-data-access-crud-consumers.md
  - evidence/dapper-async-data-access-streaming-queries.md
  - evidence/dapper-async-data-access-pagination.md
  - evidence/dapper-async-data-access-deadlock-handling.md
category: evidence
contact: resume@bryanboettcher.com
---

# Dapper-Based Async Data Access Patterns — Index

The Madera/Call-Trader platform (madera-apps repository) deliberately uses Dapper instead of Entity Framework Core for its primary data access layer. The system processes 30M+ direct mail recipient records through a pipeline that spans MassTransit message consumers, saga state machines, filter processors, user identity services, and paginated API queries. Every one of these paths uses Dapper with a consistent set of patterns.

This document covers the C# data access patterns specifically. The SQL stored procedures these patterns invoke are covered in `evidence/sql-server-database-engineering.md`. The ETL pipeline that feeds data into these tables is covered in `evidence/etl-pipeline-framework.md`.

### Why Dapper over EF Core

1. **Saga persistence** requires transactional control (explicit `DbConnection` + `DbTransaction` pairs with `RepeatableRead` isolation) that EF Core's `SaveChanges` pattern doesn't naturally express.
2. **Stored procedure invocation** is the dominant access pattern — the SQL layer does heavy lifting, and Dapper maps procedure results to POCOs without the overhead of change tracking.
3. **Table-valued parameters** require `SqlDataRecord` construction, which is a raw ADO.NET concept. EF Core would add an abstraction layer over something that's inherently ADO.NET.
4. **Unbuffered streaming** via `QueryUnbufferedAsync` returns `IAsyncEnumerable<T>` directly, which fits the pipeline architecture.
5. **The one exception proves the rule** — the reporting DbContext uses EF Core where its change tracking and LINQ translation are actually valuable, and both implementations share the same `IQuery<TModel, TParams>` interface.

The full evidence is split into focused documents:

## Child Documents

- **[Generic Connection Provider](dapper-async-data-access-connection-provider.md)** — The `IConnectionProvider<TOptions>` phantom-type abstraction that routes different subsystems (DirectMail, Ringba, Madera identity) to their respective SQL Server databases without ambient context or named registrations.

- **[Custom Type Handlers](dapper-async-data-access-type-handlers.md)** — `DateOnlyTypeHandler` bridging .NET `DateOnly` to SQL Server `DATE`, and `MailGroupingCollectionHandler` serializing complex filter hierarchies to a JSON column. Registration patterns via `SqlMapper.AddTypeHandler()`.

- **[MassTransit Saga Persistence](dapper-async-data-access-saga-persistence.md)** — Custom `DatabaseContext<T>` implementations for `ImportStateMachine` and `MailFileStateMachine`, including Read/Write model separation, `ISagaSqlFormatter<T>` for SQL generation, and concurrency detection via row count.

- **[CRUD Consumers with Dapper.Contrib](dapper-async-data-access-crud-consumers.md)** — The consistent pattern across five reference entity types (Broker, Publisher, Vertical, Creative, MailHouse): `[Table]`, `[Key]`, `[Computed]` attributes, event publishing on success, and the `RespondIf` extension for optional request/response.

- **[Unbuffered Streaming Queries and Table-Valued Parameters](dapper-async-data-access-streaming-queries.md)** — `QueryUnbufferedAsync` returning `IAsyncEnumerable<Guid>` for filter processors that stream tens of thousands of recipient IDs. TVPs for bulk address imports and hash updates against the 10-15M address table.

- **[GridReader-Based Pagination and Stored Procedure Patterns](dapper-async-data-access-pagination.md)** — `QueryMultipleAsync` + `GridReader` for single-trip paginated queries (count + results). Stored procedure invocation patterns: extended timeouts, scalar returns, tuple returns for atomic upserts, and the dual Dapper/EF Core `IQuery<TModel, TParams>` interface.

- **[Deadlock Handling with MassTransit Retry](dapper-async-data-access-deadlock-handling.md)** — Converting `SqlException` error 1205 to `DBConcurrencyException` so MassTransit's incremental retry policy handles deadlocks specifically without retrying unrecoverable SQL errors.

---
title: Dapper Data Access — GridReader-Based Pagination and Stored Procedure Patterns
tags: [dapper, csharp, pagination, gridreader, iasyncenumerable, stored-procedures, sql-server, direct-mail]
related:
  - evidence/dapper-async-data-access.md
  - evidence/dapper-async-data-access-connection-provider.md
  - evidence/dapper-async-data-access-streaming-queries.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/dapper-async-data-access.md
---

# Dapper Data Access — GridReader-Based Pagination and Stored Procedure Patterns

The Madera platform uses Dapper's `QueryMultipleAsync` with `SqlMapper.GridReader` to return both a total row count and the result page in a single database round trip. Stored procedure invocations follow consistent patterns for timeouts, scalar returns, unbuffered streaming, and tuple results.

---

## Evidence: GridReader-Based Pagination

The UI layer uses Dapper's `QueryMultipleAsync` with `SqlMapper.GridReader` to execute paginated queries that return both a total count and the result page in a single database round trip:

```csharp
// Querying.cs
public abstract class SqlPaginatedServiceBase<TModel, TParams>
    : IQuery<TModel, TParams>
    where TModel : class
    where TParams : BaseQueryPayload
{
    protected abstract string Query { get; }

    private Task<PaginatedResult<TModel>> QueryMultiple(TParams query)
    {
        var connection = _connectionProvider.CreateConnection();
        var results = connection.QueryMultipleAsync(Query, GetParams(query));
        return PaginatedResult<TModel>.Wrap(results, PaginationRequest.FromQuery(query as PaginatedQuery));
    }
}
```

The `PaginatedResult<TModel>.Wrap` factory reads the grid reader in two passes — total count first, then unbuffered result rows:

```csharp
// PaginatedResult.cs
public static async Task<PaginatedResult<TModel>> Wrap(
    Task<SqlMapper.GridReader> readerTask,
    PaginationRequest pagination)
{
    var reader = await readerTask;

    return new PaginatedResult<TModel>
    {
        PageNumber = pagination.PageNumber,
        PageSize = pagination.PageSize,
        TotalRows = await reader.ReadFirstAsync<int>(),
        Results = reader.ReadUnbufferedAsync<TModel>(),
    };
}
```

The `Results` property is `IAsyncEnumerable<TModel>` — the actual row data streams on demand. `PaginatedResult<T>` also supports single-item queries (via a custom `SingleRowAsyncEnumerable` that yields zero or one elements), client-side pagination from materialized collections (via `Simple`), and a `Transform<TOutput>` method that maps results without breaking the streaming chain.

The query infrastructure supports implicit conversion for primary key lookups:

```csharp
public sealed class PrimaryKeyQuery<TKey> : BaseQueryPayload
{
    public required TKey Key { get; init; }

    public static implicit operator PrimaryKeyQuery<TKey>(TKey key)
        => new() { Key = key, PageNumber = 0, PageSize = 1 };
}
```

This allows callers to pass a raw ID where a query payload is expected, triggering the single-item code path automatically.

The same `IQuery<TModel, TParams>` interface has an EF Core implementation (`EfCorePaginatedServiceBase`) in the same file, showing a conscious decision to use both ORMs where each fits: Dapper for read-heavy paginated queries against stored procedures, EF Core for the reporting DbContext where change tracking is useful.

---

## Evidence: Stored Procedure Invocation Patterns

The `SqlDirectMailImportService` shows how the data access layer invokes stored procedures at different scales:

**Long-running migrations** with extended timeouts:
```csharp
await connection.ExecuteAsync(
    Sql.MigrateImportedData,
    new { importId, verticalId },
    commandTimeout: 900  // 15 minutes
);
```

**Scalar queries** for operations that return a single value:
```csharp
return connection.ExecuteScalarAsync<int>(
    Sql.MarkAddressesForExport,
    new { importId },
    commandTimeout: 600
);
```

**Streaming queries** for address export:
```csharp
return connection.QueryUnbufferedAsync<AddressMetadata>(
    Sql.GetAddressesForExport,
    new { importId }
);
```

**Tuple returns** for atomic upsert operations:
```csharp
return connection.QueryFirstAsync<(Guid, bool)>(
    Sql.GetAddressId,
    new { address1, address2, city, state, zip, addressHash, addressFlags, gpsLocation }
);
```

The `GetAddressId` call returns a tuple of `(addressId, created)` — the stored procedure either finds an existing address by hash or inserts a new one, returning whether a creation occurred. This avoids a separate EXISTS check followed by conditional INSERT.

SQL strings are centralized in static `Sql` and `FilterSql` classes (referenced as `Sql.MigrateImportedData`, `FilterSql.GetRecipientsByLastMailDate`, etc.), keeping the raw SQL out of the service methods while remaining discoverable via IDE navigation.

---

## Key Files

- `madera-apps:Madera/Madera.UI.Server/Endpoints/PaginatedResult.cs` — GridReader-based pagination with unbuffered result streaming
- `madera-apps:Madera/Madera.UI.Server/Services/Querying.cs` — Abstract paginated query base class, dual Dapper/EF Core implementations behind shared IQuery interface
- `madera-apps:Madera/Madera.Dataflows.DirectMail/Services/IDirectMailImportService.cs` — Table-valued parameters, extended command timeouts, tuple returns, unbuffered address export

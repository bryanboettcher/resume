---
title: Dapper Data Access — Unbuffered Streaming Queries and Table-Valued Parameters
tags: [dapper, csharp, iasyncenumerable, streaming, table-valued-parameters, sql-server, bulk-operations, filter-processing, direct-mail]
related:
  - evidence/dapper-async-data-access.md
  - evidence/dapper-async-data-access-connection-provider.md
  - evidence/sql-analytical-views.md
  - evidence/etl-pipeline-framework.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/dapper-async-data-access.md
---

# Dapper Data Access — Unbuffered Streaming Queries and Table-Valued Parameters

Two high-throughput data access patterns in the Madera platform avoid materializing large result sets in memory: `QueryUnbufferedAsync` for streaming filter results, and table-valued parameters (TVPs) for bulk writes to SQL Server stored procedures.

---

## Evidence: Unbuffered Streaming Queries for Filter Processing

The mail file grouping filter system uses Dapper's `QueryUnbufferedAsync<T>` to stream recipient IDs without materializing entire result sets. Each filter processor returns `IAsyncEnumerable<Guid>` — a stream of recipient IDs that match the filter criteria:

```csharp
// DaysSinceMailingFilterProcessor.cs
protected override IAsyncEnumerable<Guid> Produce(
    DaysSinceMailingFilter filter,
    CancellationToken token)
{
    var maxLastMailDate = DateOnly.FromDateTime(
        _systemClock.UtcNow.Date.AddDays(-filter.DaysSinceMailing));

    var connection = _connectionProvider.CreateConnection();
    return connection.QueryUnbufferedAsync<Guid>(
        FilterSql.GetRecipientsByLastMailDate,
        new { minLastMailDate = (DateOnly?)null, maxLastMailDate, verticalId = filter.VerticalId }
    );
}
```

Six filter processors follow this pattern, each querying different recipient dimensions:

- `DateOfBirthFilterProcessor` — recipients within an age range
- `DaysSinceMailingFilterProcessor` — recipients not mailed within N days (uses `ISystemClock` for testability)
- `TimesMailedFilterProcessor` — recipients mailed between min/max times
- `OriginalPublisherFilterProcessor` — recipients from a specific publisher
- `ImportBatchFilterProcessor` — recipients from a specific import batch
- `ExternalDuplicatesFilterProcessor` — recipients flagged as cross-batch duplicates
- `UnscrubbedLeadsFilterProcessor` — recipients without scrub flags

All inherit from `BaseFilterProcessor<TFilter>` and override a single `Produce` method. The unbuffered approach is critical here: these queries can return tens of thousands of recipient GUIDs for a single mail file population, and the results feed into set intersection logic downstream. Buffering would mean holding multiple large `List<Guid>` collections simultaneously.

---

## Evidence: Table-Valued Parameters for Bulk Operations

The `SqlDirectMailImportService` uses SQL Server table-valued parameters (TVPs) to pass structured data into stored procedures, avoiding multiple round trips for batch operations:

```csharp
// IDirectMailImportService.cs
public Task UpdateRecipientHashes(
    ICollection<RecipientAddressHash> recipientHashes,
    CancellationToken token)
{
    var dataRecords = recipientHashes
        .Select(t => t.AsDataRecord())
        .Cast<SqlDataRecord>()
        .ToList();

    var connection = _connectionProvider.CreateConnection();
    return connection.ExecuteAsync(
        Sql.UpdateRecipientAddresses,
        new { addressData = dataRecords.AsTableValuedParameter("dbo.RecipientAddressHashType") }
    );
}
```

The `AsDataRecord()` method on each domain object produces a `SqlDataRecord` matching the TVP's schema definition in SQL Server. The `AsTableValuedParameter` extension (from `Microsoft.Data.SqlClient.Server`) wraps the record collection so Dapper passes it as a structured parameter.

The same pattern appears for bulk address imports:

```csharp
public Task BulkImportAddresses(
    IEnumerable<AddressMetadata> addresses,
    CancellationToken token)
{
    var dataRecords = addresses
        .Select(NormalizedAddressType.FromAddressMetadata)
        .DistinctBy(t => t.AddressHash)
        .Select(t => t.AsDataRecord())
        .Cast<SqlDataRecord>()
        .ToList();

    var connection = _connectionProvider.CreateConnection();
    return connection.ExecuteAsync(
        Sql.MergeNormalizedAddresses,
        new { addressData = dataRecords.AsTableValuedParameter("dbo.NormalizedAddressType") },
        commandTimeout: 600
    );
}
```

Note the `DistinctBy(t => t.AddressHash)` before converting to records — deduplication happens in C# before hitting SQL Server, reducing the work the MERGE statement needs to do. The 600-second command timeout reflects the scale of these operations (bulk-merging addresses against the existing 10-15M address table).

---

## Key Files

- `madera-apps:Madera/Madera.Dataflows.DirectMail/Services/GroupingFilterProcessing/Processors/DaysSinceMailingFilterProcessor.cs` — Unbuffered streaming query with ISystemClock for testability
- `madera-apps:Madera/Madera.Dataflows.DirectMail/Services/GroupingFilterProcessing/Processors/TimesMailedFilterProcessor.cs` — Unbuffered streaming query for recipient filtering
- `madera-apps:Madera/Madera.Dataflows.DirectMail/Services/IDirectMailImportService.cs` — Table-valued parameters, extended command timeouts, tuple returns, unbuffered address export

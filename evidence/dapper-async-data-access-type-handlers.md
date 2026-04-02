---
title: Dapper Data Access — Custom Type Handlers
tags: [dapper, csharp, type-handlers, sql-server, dateonly, json-serialization, direct-mail, masstransit]
related:
  - evidence/dapper-async-data-access.md
  - evidence/dapper-async-data-access-connection-provider.md
  - evidence/dapper-async-data-access-saga-persistence.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/dapper-async-data-access.md
---

# Dapper Data Access — Custom Type Handlers

The Madera/Call-Trader platform registers custom `SqlMapper.TypeHandler<T>` implementations at startup for types that Dapper doesn't natively support. These handlers bridge gaps between SQL Server's type system and .NET domain types — `DateOnly` for SQL `DATE` columns, and `ICollection<MailGrouping>` for a JSON-stored saga state field.

---

## Evidence: Custom Dapper Type Handlers

### DateOnly Handler

SQL Server's `DATE` type maps to `DateTime` in ADO.NET, but the domain model uses .NET's `DateOnly`. A custom handler bridges the gap:

```csharp
// Madera/Madera.Common/Persistence/DateOnlyTypeHandler.cs
public sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public static readonly DateOnlyTypeHandler Default = new();

    private DateOnlyTypeHandler() { }

    public override DateOnly Parse(object? value)
    {
        return value switch
        {
            null => DateOnly.MinValue,
            DateTime dateTime => DateOnly.FromDateTime(dateTime.Date),
            DateTimeOffset dateTimeOffset => DateOnly.FromDateTime(dateTimeOffset.Date),
            _ => DateOnly.Parse(value.ToString()!)
        };
    }

    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.DbType = DbType.Date;
        parameter.Value = value == DateOnly.MinValue
            ? DBNull.Value
            : value.ToDateTime(TimeOnly.MinValue);
    }
}
```

The `Parse` method handles multiple inbound types (`DateTime`, `DateTimeOffset`, string) because different SQL Server result shapes return the same column type differently depending on context. The `SetValue` method converts `DateOnly.MinValue` to `DBNull.Value` — a sentinel convention that avoids nullable `DateOnly?` throughout the domain model for optional date fields.

### JSON Collection Handler

The `MailFile` saga stores a collection of mail grouping filters as a JSON column. A custom handler serializes and deserializes `ICollection<MailGrouping>` to/from a `NVARCHAR` column:

```csharp
// Madera/Madera.Dataflows.DirectMail/Domains/MailFiles/Persistance/MailGroupingCollectionHandler.cs
public sealed class MailGroupingCollectionHandler : SqlMapper.TypeHandler<ICollection<MailGrouping>>
{
    public override void SetValue(IDbDataParameter parameter, ICollection<MailGrouping>? value)
    {
        parameter.DbType = DbType.String;
        parameter.Value = value is not null ? Serialize(value) : DBNull.Value;
    }

    public override ICollection<MailGrouping>? Parse(object value)
    {
        return value is not string json
            ? new List<MailGrouping>()
            : Deserialize(json);
    }
}
```

This lets the saga state machine read and write complex grouping filter hierarchies as a single JSON column, avoiding a relational join table for what is essentially an opaque configuration blob.

### Registration

Both handlers are registered globally via `SqlMapper.AddTypeHandler()` in their respective registry classes:

```csharp
// CommonRegistry.cs — shared types
SqlMapper.AddTypeHandler(DateOnlyTypeHandler.Default);
SqlMapper.AddTypeHandler(GpsCoordinatesTypeHandler.Default);

// DirectMailRegistry.cs — domain-specific types
SqlMapper.AddTypeHandler(MailGroupingCollectionHandler.Default);
```

All three handlers use the singleton pattern (private constructor, static `Default` field) since they carry no state.

---

## Key Files

- `madera-apps:Madera/Madera.Common/Persistence/DateOnlyTypeHandler.cs` — Custom Dapper type handler bridging .NET DateOnly to SQL Server DATE
- `madera-apps:Madera/Madera.Common/Registries/CommonRegistry.cs` — Global Dapper type handler registration (DateOnly, GPS coordinates)
- `madera-apps:Madera/Madera.Dataflows.DirectMail/Domains/MailFiles/Persistance/MailGroupingCollectionHandler.cs` — JSON serialization type handler for ICollection<MailGrouping>
- `madera-apps:Madera/Madera.Dataflows.DirectMail/DirectMailRegistry.cs` — Domain-specific type handler registration

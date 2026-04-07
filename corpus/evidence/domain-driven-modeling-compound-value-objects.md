---
title: Domain-Driven Modeling — Compound Value Objects (madera-apps + FastAddress)
tags: [ddd, value-objects, domain-modeling, csharp, dotnet, readonly-struct, polymorphic-serialization, validation, json-derived-type, high-performance]
related:
  - evidence/domain-driven-modeling.md
  - evidence/domain-driven-modeling-value-types.md
  - evidence/domain-driven-modeling-entity-saga.md
  - evidence/masstransit-contract-design.md
  - projects/call-trader-madera.md
  - projects/fastaddress-research.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/domain-driven-modeling.md
---

# Domain-Driven Modeling — Compound Value Objects

Beyond simple identifier wrappers, the madera-apps codebase models compound domain concepts as value objects with embedded validation and polymorphic behavior. FastAddress adds a `readonly struct` value object for high-performance search contexts.

---

## Evidence: GpsCoordinates — readonly struct with Nullable Factory

**GpsCoordinates** — a `readonly struct` for spatial data:

```csharp
// Madera/Madera.Common/Locations/GpsCoordinates.cs
public readonly struct GpsCoordinates
{
    public const int SRID = 4326;
    public static readonly GpsCoordinates Unknown = new();

    public double Latitude { get; init; }
    public double Longitude { get; init; }

    public static GpsCoordinates? From(double? latitude, double? longitude)
    {
        if (latitude is null || longitude is null) return null;
        return new GpsCoordinates { Latitude = latitude.Value, Longitude = longitude.Value };
    }
}
```

A `readonly struct` rather than a sealed class, appropriate because it carries two related values that only make sense together and benefits from stack allocation. The `SRID` constant (4326 = WGS-84) encodes geodetic domain knowledge directly in the type. The `From` factory method handles nullable database columns, returning `null` rather than forcing callers to construct coordinates from potentially-missing data.

---

## Evidence: MailGrouping — Self-Validating Aggregate with Polymorphic Filters

**MailGrouping** — a self-validating aggregate with a polymorphic filter hierarchy:

```csharp
// Madera/Madera.Contracts/Messages/DirectMail/MailFiles/Groupings/MailGrouping.cs
public class MailGrouping
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; init; }
    public ICollection<GroupingFilter> Filters { get; set; } = new List<GroupingFilter>();

    public virtual void ValidateAll()
    {
        foreach (var filter in Filters)
            filter.Validate();
    }
}
```

Each `GroupingFilter` subclass carries its own validation logic:

```csharp
// Madera/Madera.Contracts/Messages/DirectMail/MailFiles/Groupings/DateOfBirthFilter.cs
public sealed class DateOfBirthFilter : GroupingFilter
{
    public DateOnly? MinDateOfBirth { get; set; }
    public DateOnly? MaxDateOfBirth { get; set; }

    public override void Validate()
    {
        if (MinDateOfBirth == null && MaxDateOfBirth == null)
            throw new InvalidOperationException("At least one date must be provided.");
    }
}
```

The `GroupingFilter` base class uses `System.Text.Json` polymorphic serialization with 11 discriminated subtypes (`ImportBatchFilter`, `IncludedStateFilter`, `DateOfBirthFilter`, `ZipListFilter`, etc.). Each filter subtype defines its own `Validate()` override, and each has a corresponding `FilterProcessor` implementation in the dataflows layer.

---

## Evidence: PricingInfoData — Value Object Replacing a Primitive (kb-platform)

An embedded value object replacing a primitive — documented as ADR-003:

```csharp
// kb-platform:src/services/KbStore.Storefront/Domains/SellableItems/SellableItemEntity.cs
public class PricingInfoData
{
    public decimal Price { get; set; }
    public decimal? ListPrice { get; set; }
}
```

A `BasePrice` decimal was replaced with a `PricingInfoData` object that captures both the selling price and an optional list price for discount display. This is a characteristic DDD refactoring: replacing a primitive with a value object when the domain concept grows beyond what a single field can express.

---

## Evidence: AddressEncoding — readonly struct for High-Performance Search (FastAddress)

```csharp
// FastAddress:FastAddress.Components/Models/AddressEncoding.cs
public readonly struct AddressEncoding
{
    public int Id { get; init; }
    public short[] ExactTokens { get; init; }
    public float[] SemanticEmbedding { get; init; }
    public string OriginalText { get; init; }
}
```

`AddressEncoding` separates an address into exact-match tokens (house numbers, ZIP codes stored as `short[]`) and a semantic embedding (`float[]`) for fuzzy matching. The `readonly struct` choice is performance-motivated — these objects are created in bulk during address indexing and compared during search without heap allocation pressure.

---

## Key Files

- `madera-apps:Madera/Madera.Common/Locations/GpsCoordinates.cs` — Readonly struct value object with SRID constant and nullable factory
- `madera-apps:Madera/Madera.Contracts/Messages/DirectMail/MailFiles/Groupings/GroupingFilter.cs` — Polymorphic filter base with 11 subtypes and JSON discriminator
- `madera-apps:Madera/Madera.Contracts/Messages/DirectMail/MailFiles/Groupings/MailGrouping.cs` — Self-validating aggregate with filter collection
- `madera-apps:Madera/Madera.Contracts/Messages/DirectMail/MailFiles/Groupings/DateOfBirthFilter.cs` — Concrete filter with domain validation
- `FastAddress:FastAddress.Components/Models/AddressEncoding.cs` — Readonly struct encoding for SIMD-accelerated address search

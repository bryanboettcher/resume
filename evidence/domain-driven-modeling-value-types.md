---
title: Domain-Driven Modeling — Strongly-Typed Domain Identifiers (madera-apps)
tags: [ddd, value-objects, domain-modeling, csharp, dotnet, sealed-classes, implicit-operators, iequatable, primitive-obsession, masstransit]
related:
  - evidence/domain-driven-modeling.md
  - evidence/domain-driven-modeling-entity-saga.md
  - evidence/domain-driven-modeling-compound-value-objects.md
  - evidence/masstransit-contract-design.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/domain-driven-modeling.md
---

# Domain-Driven Modeling — Strongly-Typed Domain Identifiers (madera-apps)

The `Madera.Contracts.ValueTypes.DirectMail` namespace contains seven sealed value object classes, each wrapping a single primitive to give domain meaning to what would otherwise be bare `int`, `long`, `byte`, or `Guid` values. These flow through MassTransit message contracts, Dapper queries, and API payloads throughout the system.

---

## Evidence: The Seven Domain Identifier Types

| Value Type | Wrapped Primitive | Domain Concept |
|---|---|---|
| `BrokerId` | `int` | Direct mail broker identity |
| `PublisherId` | `int` | Lead publisher identity |
| `MailFileId` | `int` | Mail file batch identity |
| `AddressId` | `int` | Normalized address record identity |
| `AddressHash` | `long` | Address deduplication hash |
| `VerticalId` | `byte` | Business vertical category |
| `CorrelationId` | `Guid` | MassTransit saga correlation identity |

Every value type follows the same structural pattern:

```csharp
// Madera/Madera.Contracts/ValueTypes/DirectMail/AddressHash.cs
public sealed class AddressHash : IEquatable<AddressHash>
{
    private readonly long _value;

    private AddressHash(long value) => _value = value;

    public static implicit operator long(AddressHash other) => other._value;
    public static implicit operator AddressHash(long other) => new(other);
    public static bool operator ==(AddressHash? left, AddressHash? right) => Equals(left, right);
    public static bool operator !=(AddressHash? left, AddressHash? right) => !Equals(left, right);

    public bool Equals(AddressHash? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return _value.Equals(other._value);
    }
    public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is AddressHash other && Equals(other);
    public override int GetHashCode() => _value.GetHashCode();
}
```

The pattern is consistent across all seven types: `sealed class`, private constructor, `IEquatable<T>`, full equality operator overloads, and bidirectional implicit conversion operators. The private constructor forces creation through the implicit operator, so the type system enforces that a `BrokerId` cannot be accidentally passed where a `PublisherId` is expected — even though both wrap `int` at runtime.

`BrokerId` goes further than the others, providing nullable implicit conversions for database interop:

```csharp
// Madera/Madera.Contracts/ValueTypes/DirectMail/BrokerId.cs
public static implicit operator int?(BrokerId? other) => other?._value ?? null;
public static implicit operator BrokerId?(int? other) => other is null ? null : new(other.Value);
```

This allows Dapper and EF Core to map nullable database columns directly to `BrokerId?` properties without explicit conversion code in every consumer.

These are reference-type value objects (classes, not structs). This is a deliberate trade-off: they serialize cleanly through MassTransit's JSON message pipeline and support null semantics without `Nullable<T>` boxing, at the cost of heap allocation. In a system processing 30M+ recipient records, the domain clarity outweighs the allocation cost in the contract layer — the hot path uses primitives in SQL and bulk copy operations.

---

## Key Files

- `madera-apps:Madera/Madera.Contracts/ValueTypes/DirectMail/AddressHash.cs` — Canonical value object: sealed, private ctor, IEquatable, implicit operators
- `madera-apps:Madera/Madera.Contracts/ValueTypes/DirectMail/BrokerId.cs` — Value object with nullable implicit conversions for database interop
- `madera-apps:Madera/Madera.Contracts/ValueTypes/DirectMail/CorrelationId.cs` — Guid-wrapping value object for MassTransit saga correlation
- `madera-apps:Madera/Madera.Contracts/ValueTypes/DirectMail/VerticalId.cs` — Byte-wrapping value object for business vertical category

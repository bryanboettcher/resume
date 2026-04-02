---
title: MassTransit Contract Design — Strongly-Typed Domain Identity Value Types
tags: [masstransit, contract-design, message-driven, csharp, value-types, domain-identity, type-safety, primitive-obsession]
related:
  - evidence/masstransit-contract-design.md
  - evidence/masstransit-contract-design-project-structure.md
  - evidence/masstransit-contract-design-interface-inheritance.md
  - evidence/dotnet-csharp-expertise.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/masstransit-contract-design.md
---

# MassTransit Contract Design — Strongly-Typed Domain Identity Value Types

In the Madera direct mail platform, Bryan includes strongly-typed domain identifier wrappers in the `Madera.Contracts` project alongside the message interfaces. These value types prevent primitive obsession in message contracts — a `BrokerId` and a `PublisherId` are not interchangeable at the type level, even though both are backed by the same underlying primitive.

---

## Evidence: Domain Identity Wrappers

**Location:** `Madera/Madera.Contracts/ValueTypes/DirectMail/`

The contracts project defines sealed wrapper types for each domain identifier:

```csharp
public sealed class CorrelationId : IEquatable<CorrelationId>
{
    private readonly Guid _value;
    private CorrelationId(Guid value) => _value = value;
    public static implicit operator Guid(CorrelationId other) => other._value;
    public static implicit operator CorrelationId(Guid other) => new(other);
}
```

Similar wrappers exist for `AddressHash`, `BrokerId`, `PublisherId`, `VerticalId`, and `MailFileId`.

### Design Decisions

**Implicit conversion operators** mean these types are interchangeable with their primitives at call sites — no explicit casting syntax. Existing code using raw `Guid` values can adopt `CorrelationId` without changing every call site. But the type system still prevents accidentally passing a `BrokerId` where a `PublisherId` is expected, because implicit conversion only goes through the primitive (Guid → BrokerId and Guid → PublisherId are two distinct conversions that don't chain transitively).

**Private constructor** means the only way to create a `CorrelationId` is through the implicit conversion from `Guid`, not through `new CorrelationId(...)` directly. This keeps the construction path consistent and prevents subclassing.

**`IEquatable<T>` implementation** ensures that comparison, dictionary keys, and `==` operator behavior is defined by the wrapper's value semantics, not by reference identity. Two `CorrelationId` instances wrapping the same `Guid` are equal.

**Placement in the contracts project** is significant — these aren't application-layer helpers. They're part of the same assembly that defines the message interfaces, so any service referencing `Madera.Contracts` gets the type-safe identifiers as part of the contract surface. The domain identity types are as fundamental to the contracts as the message interfaces themselves.

### Why This Belongs in Contracts

If domain identity wrappers lived in a separate utility project, consuming services would need an additional dependency to use strongly-typed IDs in their message handlers. By including them in the contracts project, any service that can consume messages can also use typed identifiers — no extra dependencies, no friction.

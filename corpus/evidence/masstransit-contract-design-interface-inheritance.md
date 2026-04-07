---
title: MassTransit Contract Design — Interface Inheritance as Domain Modeling
tags: [masstransit, contract-design, message-driven, csharp, interface-inheritance, domain-modeling, failure-contracts, topology]
related:
  - evidence/masstransit-contract-design.md
  - evidence/masstransit-contract-design-project-structure.md
  - evidence/masstransit-contract-design-import-workflow.md
  - evidence/masstransit-contract-design-mail-file-lifecycle.md
  - evidence/distributed-systems-architecture.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/masstransit-contract-design.md
---

# MassTransit Contract Design — Interface Inheritance as Domain Modeling

In the Madera direct mail platform, Bryan uses a consistent three-tier interface inheritance pattern across all domain entity contracts. Each entity follows the same base model / command / event decomposition, with MassTransit-specific attributes controlling which interfaces become actual message types on the wire. This section also covers the structured failure contract hierarchy that replaces generic exception-based error handling.

---

## Evidence: The Base Model / Command / Event Pattern

**File:** `Madera/Madera.Contracts/Messages/DirectMail/Core/VerticalMessages.cs` (62 lines)

Every domain entity in the contracts project follows a three-tier interface pattern. Taking `VerticalMessages.cs` as an example:

```csharp
// Tier 1: The read model — defines the entity's shape
[ExcludeFromTopology, ExcludeFromImplementedTypes]
public interface VerticalModelBase
{
    int Id { get; }
    string Name { get; }
    string? DisplayName { get; }
    double? MinimumAge { get; }
    double? MaximumAge { get; }
}

// Tier 2: The command base — what you need to identify the entity
public interface VerticalCommandBase
{
    int Id { get; }
}

// Tier 3: CRUD operations — compose from the base types
public interface CreateVerticalCommand { string Name { get; } ... }
public interface CreateVerticalResponse : VerticalModelBase;
public interface VerticalCreatedEvent : VerticalModelBase;

public interface UpdateVerticalCommand : VerticalCommandBase { string Name { get; } ... }
public interface UpdateVerticalResponse : VerticalModelBase;
public interface VerticalUpdatedEvent : VerticalModelBase;

public interface DeleteVerticalCommand : VerticalCommandBase;
public interface DeleteVerticalResponse : VerticalModelBase;
public interface VerticalDeletedEvent : VerticalModelBase;
```

This pattern repeats across all six entity types (Address, Broker, Creative, MailHouse, Publisher, Vertical).

### Why `[ExcludeFromTopology]` and `[ExcludeFromImplementedTypes]`

These MassTransit-specific attributes tell the framework's topology configuration that the base interfaces are structural abstractions, not real message types that should get their own exchanges or queues. Without them, MassTransit would create RabbitMQ exchange bindings for `VerticalModelBase` and attempt to route messages to it, breaking the consumer topology. The attributes are the mechanism that makes structural inheritance safe in a message-driven system.

### Naming Without the `I` Prefix

The naming convention is deliberate. Bryan disables ReSharper's interface naming rule (`InconsistentNaming`) with an explicit comment explaining that MassTransit messages being interfaces is an implementation detail that shouldn't dictate naming. Interfaces read as `CreateVerticalCommand`, not `ICreateVerticalCommand`, because consumers deal with them as message types, not abstractions to implement.

---

## Evidence: Typed Failure Contracts

**File:** `Madera/Madera.Contracts/Messages/Common/FailureEventBase.cs` (19 lines)

Rather than a generic exception-based error model, the contracts define a structured failure hierarchy:

```csharp
public interface FailureEventBase
{
    string Message { get; }
    FailureTypes FailureType { get; }
    Exception? InnerException { get; }
}

public enum FailureTypes { Unknown, Conflict, Missing, Invalid }
```

Each domain entity has its own typed failure contract (e.g., `BrokerRequestFailure : FailureEventBase`, `VerticalRequestFailure : FailureEventBase`). This means consumers can handle failures with domain-specific granularity — a `Missing` failure on an address means something different from a `Missing` failure on a publisher, and the type system enforces that distinction. The four `FailureTypes` values (`Unknown`, `Conflict`, `Missing`, `Invalid`) align with the standard categories of domain operation failures, allowing consuming code to branch on failure cause without parsing error strings.

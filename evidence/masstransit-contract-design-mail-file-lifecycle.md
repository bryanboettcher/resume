---
title: MassTransit Contract Design — Mail File Lifecycle with Bidirectional Transitions
tags: [masstransit, contract-design, message-driven, csharp, saga, bidirectional-state, polymorphism, concurrency, optimistic-concurrency, json-polymorphism, filter-hierarchy]
related:
  - evidence/masstransit-contract-design.md
  - evidence/masstransit-contract-design-project-structure.md
  - evidence/masstransit-contract-design-interface-inheritance.md
  - evidence/masstransit-contract-design-import-workflow.md
  - evidence/distributed-systems-architecture.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/masstransit-contract-design.md
---

# MassTransit Contract Design — Mail File Lifecycle with Bidirectional Transitions

In the Madera direct mail platform, Bryan designed the mail file workflow contracts to support bidirectional state transitions — operators can move a mail file backward to reconfigure it, not just forward through the pipeline. The `MailFilesMessages.cs` contract file defines both forward and backward transition pairs, a polymorphic filter hierarchy for composable grouping rules, and an optimistic concurrency mechanism built directly into the message contracts.

---

## Evidence: Mail File Lifecycle Contracts

**File:** `Madera/Madera.Contracts/Messages/DirectMail/MailFiles/MailFilesMessages.cs` (185 lines)

### Forward and Backward State Transitions

The mail file workflow has three named states — Kneading, Shaking, and Baking — and the contracts define both forward and backward transitions:

**Forward transitions:**
```
Kneading → Shaking:  BeginMailFilePopulationRequest / PopulateMailFileRequest
Shaking → Baking:    BakeMailFileRequest / PopulateMailOutputRequest
Baking → Completed:  CompleteMailFileRequest / MailFileCompletedEvent
```

**Backward transitions:**
```
Shaking → Kneading:  ResetRecipientRequest / ClearMailFileRequest
Baking → Shaking:    ResetMailOutputRequest / ClearMailOutputRequest
```

Each transition has a two-layer contract structure. Taking the Kneading-to-Shaking transition:

```csharp
// Layer 1: Endpoint-facing (what the UI sends)
public interface BeginMailFilePopulationRequest : CorrelatedBy<Guid>;
public interface BeginMailFilePopulationFailure : FailureEventBase;
public interface BeginMailFilePopulationResponse : MailFileEventBase;

// Layer 2: Saga-internal (what the state machine dispatches)
public interface PopulateMailFileRequest : MailFileEventBase;
public interface PopulateMailFileResponse : CorrelatedBy<Guid>
{
    int TotalRecipients { get; }
}
public interface MailFilePopulatedEvent : MailFileUpdatedEvent;
```

The endpoint-facing request is a bare correlation ID — the UI just says "start populating this file." The saga-internal request carries the full `MailFileEventBase` state so the population consumer has access to all groupings, filters, and configuration without querying back. The response returns only the recipient count — the minimum data the saga needs to update its state.

---

## Evidence: Polymorphic Filter Contracts

**File:** `Madera/Madera.Contracts/Messages/DirectMail/MailFiles/Groupings/GroupingFilter.cs` (24 lines)

Mail file groupings use a polymorphic type hierarchy for composable filters. This is one of the few places in the contracts project that uses a concrete class instead of an interface:

```csharp
[JsonDerivedType(typeof(ImportBatchFilter), nameof(ImportBatchFilter))]
[JsonDerivedType(typeof(IncludedStateFilter), nameof(IncludedStateFilter))]
[JsonDerivedType(typeof(DaysSinceMailingFilter), nameof(DaysSinceMailingFilter))]
// ... 11 filter types total
[JsonPolymorphic(TypeDiscriminatorPropertyName = nameof(FilterType))]
public abstract class GroupingFilter
{
    [JsonIgnore]
    public virtual string FilterType => GetType().Name;
    public abstract void Validate();
}
```

The `[JsonDerivedType]` / `[JsonPolymorphic]` attributes enable System.Text.Json to serialize and deserialize the filter hierarchy across the message bus. Each filter type (`ImportBatchFilter`, `IncludedStateFilter`, `DaysSinceMailingFilter`, `DateOfBirthFilter`, `ZipListFilter`, etc.) is a separate class with its own filter-specific properties, all serializable through the base `GroupingFilter` contract.

The `Validate()` method on the base class pushes validation into the contract itself. A filter must be valid before it can be sent as part of a message — validation is not deferred to the consumer.

The use of abstract class rather than interface here is deliberate: `[JsonDerivedType]` requires a concrete base type because System.Text.Json's polymorphic deserialization needs a concrete root to register discriminators against. Interfaces can't carry that metadata.

---

## Evidence: Optimistic Concurrency in Contracts

**File:** `Madera/Madera.Contracts/Messages/DirectMail/MailFiles/MailGroupingsMessages.cs` (19 lines)

The groupings update contract includes a versioning mechanism:

```csharp
public sealed class UpdateMailFileGroupingsRequest : CorrelatedBy<Guid>
{
    public required Guid CorrelationId { get; init; }
    public required string GroupingsVersion { get; init; }
    public required MailGrouping[] Groupings { get; init; }
}

public interface UpdateMailFileGroupingsResponse
{
    bool IsConflict { get; }
    string GroupingsVersion { get; }
}
```

`GroupingsVersion` is a random nonce — documented as such in the saga state: "Random nonce to ensure that older updates don't overwrite newer ones." The response includes `IsConflict` so the caller knows whether the update was accepted. This is optimistic concurrency at the message contract level: the saga rejects stale updates without database-level locking, and the conflict detection is visible in the contract rather than buried in saga implementation code.

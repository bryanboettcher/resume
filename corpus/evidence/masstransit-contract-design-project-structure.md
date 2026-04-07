---
title: MassTransit Contract-First Design — Project Structure and Philosophy
tags: [masstransit, contract-design, message-driven, csharp, contract-first, domain-modeling, project-structure, direct-mail]
related:
  - evidence/masstransit-contract-design.md
  - evidence/masstransit-contract-design-interface-inheritance.md
  - evidence/masstransit-contract-design-import-workflow.md
  - evidence/masstransit-contract-design-mail-file-lifecycle.md
  - evidence/masstransit-contract-design-value-types.md
  - evidence/masstransit-contract-design-why-contract-first.md
  - evidence/distributed-systems-architecture.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/masstransit-contract-design.md
---

# MassTransit Contract-First Design — Project Structure and Philosophy

In the Madera direct mail platform, Bryan treats message contracts as the primary design artifact in a distributed system — the `Madera.Contracts` project defines every message interface before any consumer, saga, or endpoint is implemented. The structure and organization of this contracts project illustrates how domain topology maps directly to folder hierarchy, making the system's shape readable without tracing implementation code.

---

## Contract-First as Design Discipline

The `Madera.Contracts` project has zero references to implementation code. It depends only on MassTransit's core abstractions and shared value types. This constraint is deliberate: when the contracts project is implementation-free, the workflow structure, state transitions, and domain operations become readable from the contract definitions alone. The contracts are the system's public API between services — adding a new consumer or saga never requires changes to the contracts project unless the message shape changes.

---

## Evidence: Contract Project Structure

**Repository:** github.com/Call-Trader/madera-apps (private)
**Contract location:** `Madera/Madera.Contracts/`

The contracts project is organized into a deliberate hierarchy that mirrors the domain itself:

```
Madera.Contracts/
  Enumerations/
    MailFileState.cs          — Kneading, Shaking, Baking state enum
    MailFilePopulationStrategy.cs
    PaymentStatus.cs
    ScrubbedReason.cs         — Bitflag enum for import quality tracking
  Messages/
    Common/
      FailureEventBase.cs     — Typed failure with FailureTypes enum
    DirectMail/
      Core/                   — CRUD contracts for domain entities
        AddressMessages.cs
        BrokerMessages.cs
        CreativeMessages.cs
        MailHouseMessages.cs
        PublisherMessages.cs
        VerticalMessages.cs
      Importing/              — Import workflow state machine contracts
        ImportWorkflowMessages.cs
        AddressNormalizationMessages.cs
      MailFiles/              — Mail file lifecycle contracts
        MailFilesMessages.cs
        MailGroupingsMessages.cs
        MailFileRecipientsMessages.cs
        MailFileReviewMessages.cs
        Groupings/            — Polymorphic filter type hierarchy
    Convoso/
    Ringba/
    Dispos/
  ValueTypes/
    DirectMail/
      AddressHash.cs, BrokerId.cs, CorrelationId.cs, etc.
```

Core entity CRUD operations live under `Core/`, workflow orchestration messages under `Importing/` and `MailFiles/`, and each external data source (Convoso, Ringba, Dispos) has its own namespace. A developer navigating this directory understands the domain topology — six entity types, two major workflow pipelines, four external data sources — without reading any implementation code.

---

## Scale of the Contract Surface

The full `Madera.Contracts` project defines approximately 130 distinct message interfaces across 30 files:

- 6 CRUD entity domains (Address, Broker, Creative, MailHouse, Publisher, Vertical), each with Create/Update/Delete/Validate operation sets
- 1 import workflow with 8 state transition events, upload handling with `MessageData<Stream>`, and 3 restart commands
- 1 mail file lifecycle with 5 forward/backward transitions, polymorphic grouping filters, and optimistic concurrency
- 4 external data source domains (DirectMail, Convoso, Ringba, Dispos)
- Typed failure contracts with 4 failure categories
- 6 strongly-typed domain identity wrappers

All of this is a single project with no implementation code.

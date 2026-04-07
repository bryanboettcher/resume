---
title: MassTransit Contract Design — Why Contract-First Matters for Distributed Systems
tags: [masstransit, contract-design, message-driven, csharp, distributed-systems, topology, rabbitmq, service-boundaries, living-documentation]
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

# MassTransit Contract Design — Why Contract-First Matters for Distributed Systems

In a saga-orchestrated system like the Madera direct mail platform, contracts are the API between services. Bryan's contract-first design discipline — where all message interfaces are defined before any consumer or saga implementation — follows directly from how MassTransit works internally and what properties it makes possible in a distributed system.

---

## How MassTransit Uses Contract Types

MassTransit's topology engine generates RabbitMQ exchanges and queue bindings directly from message interface types. When two services share a `Madera.Contracts` reference, they can communicate without any additional configuration — the interface types are the wire format. This means:

- A consumer that implements `IConsumer<CreateVerticalCommand>` automatically gets a queue bound to the correct exchange the moment it starts
- A saga that publishes `ImportStagingCompletedEvent` doesn't need to know which consumers are listening — MassTransit routes it based on type
- Adding a new consumer to an existing event type requires no changes to any other service

This tight coupling between interface types and message topology is what makes contract-first a practical requirement, not just a design preference. If you define contracts after implementations, you're working backward against the framework's grain.

---

## Properties the Contract-First Approach Enables

### Zero-Implementation-Code in the Contracts Assembly

The `Madera.Contracts` project depends only on `MassTransit` (for `CorrelatedBy<T>`, `ExcludeFromTopology`, `MessageData<T>`) and `System.Text.Json` (for the polymorphic filter hierarchy). No references to any implementation assembly. This enforces three properties:

1. **Adding a new consumer or saga never requires changes to the contracts project** unless the message shape changes. Implementation churn doesn't propagate to the shared contract boundary.

2. **Multiple services can reference the contracts without pulling in implementation dependencies.** A front-end BFF that only needs to publish `UploadImportCommand` doesn't need to reference the saga assembly, the EF Core models, or the database layer.

3. **The contracts serve as living documentation.** Every message that flows through the system is defined in one place, with its structure, inheritance, and relationships readable without running the code. New engineers can navigate the domain by reading contracts.

### Readable Workflow Structure Without Tracing Implementation

The import workflow's eight transition events — `ImportStagingStartedEvent` through `ImportCompletedEvent` — describe the complete pipeline in a flat list. The mail file lifecycle's forward and backward transition pairs show exactly which state reversals are supported. This level of design clarity would be invisible if the workflow were defined procedurally across consumer implementations.

### Topology Safety Through Attributes

The `[ExcludeFromTopology]` and `[ExcludeFromImplementedTypes]` attributes on base interfaces are what make structural inheritance viable in a message-driven system. Without them, MassTransit would create exchange bindings for every base interface, routing messages to non-existent consumers. The contracts project's author has to actively mark which interfaces are structural and which are real message types — this discipline is only possible when contracts are designed as a first-class artifact.

---

## Contrast with Implementation-First Approaches

In implementation-first systems, message types tend to proliferate without structure — different services define their own versions of similar concepts, shared types end up in utility projects with no clear ownership, and the topology degrades over time as shortcuts accumulate. The Madera contracts project avoids this by making the contracts project the authoritative source of domain structure from the start.

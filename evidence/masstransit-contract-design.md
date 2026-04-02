---
title: MassTransit Message Contract Design
tags: [masstransit, contract-design, message-driven, csharp, interface-design, saga-orchestration, cqrs, event-driven, domain-modeling, direct-mail]
related:
  - evidence/distributed-systems-architecture.md
  - evidence/etl-pipeline-framework.md
  - projects/call-trader-madera.md
children:
  - evidence/masstransit-contract-design-project-structure.md
  - evidence/masstransit-contract-design-interface-inheritance.md
  - evidence/masstransit-contract-design-import-workflow.md
  - evidence/masstransit-contract-design-mail-file-lifecycle.md
  - evidence/masstransit-contract-design-value-types.md
  - evidence/masstransit-contract-design-why-contract-first.md
category: evidence
contact: resume@bryanboettcher.com
---

# MassTransit Message Contract Design — Index

Bryan treats message contracts as the primary design artifact in distributed systems. In the Madera direct mail platform, the `Madera.Contracts` project defines every message interface before any consumer, saga, or endpoint is implemented. The contracts project has zero references to implementation code — the workflow structure, state transitions, and domain operations are all readable from the contract definitions alone.

The full evidence is split into focused documents:

## Child Documents

- **[Project Structure and Philosophy](masstransit-contract-design-project-structure.md)** — How the contracts project is organized to mirror domain topology; why contract-first is a design discipline, not just a convention. Scale: ~130 message interfaces across 30 files.

- **[Interface Inheritance as Domain Modeling](masstransit-contract-design-interface-inheritance.md)** — The three-tier base model / command / event pattern applied to all six CRUD entity types. Covers `[ExcludeFromTopology]` / `[ExcludeFromImplementedTypes]` attributes, the `I`-prefix naming decision, and the typed failure contract hierarchy.

- **[Import Workflow Saga Contracts](masstransit-contract-design-import-workflow.md)** — How `ImportWorkflowMessages.cs` defines the complete saga lifecycle: the base model as saga state shape, eight flat transition events, endpoint-facing vs. saga-internal request/response pairs, `MessageData<Stream>` for large file payloads, and minimal restart commands.

- **[Mail File Lifecycle — Bidirectional Transitions](masstransit-contract-design-mail-file-lifecycle.md)** — The more complex workflow: forward Kneading → Shaking → Baking → Completed transitions, backward reset transitions, two-layer endpoint/saga contract structure, polymorphic `GroupingFilter` hierarchy with `[JsonDerivedType]` / `[JsonPolymorphic]`, and optimistic concurrency via `GroupingsVersion` nonce.

- **[Value Types and Domain Identity](masstransit-contract-design-value-types.md)** — Strongly-typed wrappers (`CorrelationId`, `BrokerId`, `PublisherId`, etc.) colocated in the contracts project. Implicit conversion operators, private constructors, `IEquatable<T>`, and why placement in the contracts assembly matters.

- **[Why Contract-First Matters](masstransit-contract-design-why-contract-first.md)** — How MassTransit's topology engine ties interface types to RabbitMQ exchange/queue bindings, the three properties the zero-implementation-code constraint enables, and contrast with implementation-first approaches.

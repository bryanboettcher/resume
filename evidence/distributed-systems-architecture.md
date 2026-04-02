---
title: Distributed Systems & Event-Driven Architecture
tags: [masstransit, sagas, state-machines, ddd, event-driven, rabbitmq, microservices, kubernetes, cqrs, saga-orchestration]
related:
  - projects/call-trader-madera.md
  - projects/kbstore-ecommerce.md
  - projects/homelab-infrastructure.md
  - projects/taylor-summit.md
  - evidence/open-source-contributions.md
  - evidence/dotnet-csharp-expertise.md
  - evidence/data-engineering-etl.md
  - evidence/infrastructure-devops.md
  - links/github-prs.md
category: evidence
contact: resume@bryanboettcher.com
---

# Distributed Systems & Event-Driven Architecture — Evidence Portfolio

## Philosophy

Bryan builds distributed systems around explicit state machines and message-driven workflows rather than synchronous request/response chains. His preferred pattern is saga-based orchestration where each step is independently retriable and the system state is always inspectable. He has deep familiarity with MassTransit (the leading .NET distributed application framework) at both the user and framework-internals level.

---

## Evidence: MassTransit Framework Expertise (Deep Internals)

### Attempted Contributions to MassTransit Core
**Repository:** https://github.com/MassTransit/MassTransit (7,707 stars)

#### PR #6039 — "Big Beautiful PR" (ADO.NET Saga Repositories)
**URL:** https://github.com/MassTransit/MassTransit/pull/6039 (Closed November 2025)

Bryan proposed a complete set of ADO.NET-based saga repository implementations for MySQL, PostgreSQL, and SQL Server as alternatives to Entity Framework Core. The implementation included:
- Optimistic and pessimistic concurrency strategies
- Job consumer support
- Message data repositories
- Full test coverage

The PR was closed by maintainer Chris Patterson (phatboyg) not due to quality concerns but because the maintenance burden didn't justify the projected adoption (NuGet download stats showed EF Core dominance). This demonstrates Bryan's ability to work at the framework level and understand distributed persistence patterns deeply enough to implement them from scratch.

#### PR #5956 — Dapper Integration Overhaul
**URL:** https://github.com/MassTransit/MassTransit/pull/5956 (Closed June 2025)

Identified and addressed multiple architectural issues in MassTransit's existing Dapper integration:
- Generic type misuse in saga repository
- Inappropriate semaphore usage for concurrency control
- Missing extension methods for Job saga consumers
- Missing Outbox/MessageAudit/MessageData integrations

Received detailed code review from Chris Patterson. Some fixes were extracted and applied by the maintainer separately.

#### Systematic Issue Filing (5 issues, April–May 2025)
**Issues:** #5954, #5957, #5958, #5980, #5981, #5982

Filed a systematic series of issues identifying architectural problems in MassTransit's Dapper integration. Several were marked completed. This demonstrates not just usage knowledge but the ability to audit framework internals and identify structural deficiencies.

---

## Evidence: KbStore — Event-Driven E-Commerce Platform

**Repository:** https://github.com/bryanboettcher/KbStore
**Local path:** ~/src/bryanboettcher/KbStore/

A distributed e-commerce platform built with Domain-Driven Design and event-driven architecture:

### Architecture
- **Bounded Contexts:** Catalog (product/inventory, PostgreSQL) and Storefront (customer transactions, MongoDB) — polyglot persistence by design
- **Messaging:** MassTransit over RabbitMQ for inter-domain communication
- **Orchestration:** .NET Aspire for local development and service discovery
- **State Machines:** MassTransit saga state machines for cross-domain workflow orchestration
- **API Gateway:** ASP.NET Core HTTP gateway handling cross-domain request routing

### Design Decisions
- Separate databases per bounded context (PostgreSQL for relational catalog data, MongoDB for document-oriented storefront data) — true polyglot persistence, not just using two databases
- Message-driven cross-domain communication — domains never call each other directly
- Saga-based orchestration for workflows that span domain boundaries

---

## Evidence: Madera/Call-Trader — Production Saga Orchestration

**Repository:** github.com/Call-Trader/madera-apps (private)
**Local path:** ~/src/bryanboettcher/madera-apps/

### The Transformation: Monolithic Loop → Observable Pipeline
The system Bryan replaced was a single `foreach` loop over a CSV file — if any row failed, the entire import failed with no recovery path. No visibility into progress, no ability to retry from mid-point, no way to understand what went wrong without reading logs after the fact.

Bryan decomposed this into a saga state machine where each processing stage is an independent, observable step:
```
Upload → Stage → MatchAddresses → NormalizeAddresses → MigrateData → Complete
```

The architectural payoff:
- **Mid-process visibility:** Operators can see exactly which stage an import is in, how many records have been processed, and what's pending — while it's running
- **Partial failure recovery:** If address normalization fails on row 40,000 of 50,000, the system retries from that stage with the 10,000 remaining rows — not from scratch
- **Repairability:** A failed stage can be manually re-triggered after fixing the underlying issue (bad data, API timeout, schema mismatch) without re-uploading or re-processing earlier stages
- **Independent retriability:** Each transition is a discrete step with fault handling and compensating actions
- **State persistence:** Saga state survives process restarts — an import that was mid-normalization when the server restarted picks up where it left off
- **Auditability:** Every state transition is a message, creating an implicit audit trail of what happened, when, and in what order

This pattern was then replicated across all four data source pipelines (DirectMail, Convoso, Ringba, Dispos), and the same observability/repairability principles were applied to the MailFile generation workflow. The investment in saga decomposition meant that subsequent features like import reporting, progress dashboards, and error remediation UIs were trivial to build — the data was already there in the saga state.

### Mail File State Machine
Orchestrates mail file generation with bidirectional transitions:
```
Kneading (configure) → Shaking (populate) → Baking (generate) → Complete
```
- Supports undo/redo (bidirectional state transitions) — unusual for saga implementations
- 12 composable filter types for population stage
- Output generation produces CSV files for mail house consumption

### Infrastructure Choices
- **MassTransit over Wolverine:** Explicit architectural decision documented in project history. Bryan chose MassTransit for its mature saga support and explicit state machine modeling despite Wolverine's simpler API.
- **Custom MassTransit.DapperIntegration:** Built a local library for Dapper-based saga persistence, motivated by performance requirements that EF Core couldn't meet. This directly led to the upstream MassTransit PRs.
- **RabbitMQ transport:** Chosen for reliability and operational familiarity; later architecture documents discuss potential migration to MessagePipe for in-process pub/sub in a modular monolith configuration.

---

## Evidence: Saga Entity Deduplication & Cross-Aggregate Choreography

**Repository:** github.com/Call-Trader/madera-apps (private)
**Branch:** `do-not-run` (architectural prototype)

### Address State Machine — Hash-Based Entity Deduplication
Bryan designed an `AddressStateMachine` (173 lines) implementing a saga-based entity deduplication pattern rarely seen in MassTransit implementations:

- **Hash-based correlation:** `CorrelateBy(x => x.AddressHash)` uses CRC64 hashes instead of GUIDs for saga correlation, enabling automatic detection of duplicate address submissions across independent import batches
- **Merge pattern:** When a duplicate address arrives, the saga publishes an `AddressMergedEvent` containing the canonical entity reference, then finalizes the duplicate — rather than rejecting it or silently dropping it
- **Read-only events:** Validation queries use events that don't create saga instances (`OnMissingInstance` returns typed failure responses rather than faulting)

### Cross-Aggregate Saga Choreography
The Recipient and Address domains maintain eventual consistency through saga-to-saga messaging:

```
RecipientStateMachine → publishes EnsureAddressCommand
    → AddressStateMachine receives, creates/merges address
        → publishes AddressVerifiedEvent / AddressMergedEvent
            → RecipientStateMachine reacts, links to canonical address
```

This is genuine distributed systems choreography — two independent state machines coordinating across aggregate boundaries without synchronous coupling. Each saga owns its lifecycle, communicates only through messages, and handles the case where the other aggregate may not yet exist.

### Domain-Driven Bounded Context Restructuring
Restructured flat consumer/saga folders into proper bounded contexts: `Domains/{Addresses, Brokers, Creatives, Imports, MailFiles, MailHouses, Publishers, Recipients, Verticals}`. Each domain contains its own state machines, consumers, and persistence — enforcing domain isolation at the folder level.

### BDD Test Coverage at Scale
The `Address_Tests.cs` file spans 1,153 lines with 26 nested test classes following `When_X / When_Y` BDD patterns. This provides exhaustive behavioral coverage of a single state machine: create/update/delete/validate/ensure operations, each with exists/not-exists scenarios and merge scenarios. The MassTransitTestBase infrastructure includes `ISystemClock` mocking for deterministic time-based saga testing.

---

## Evidence: Homelab — Distributed Infrastructure

**Local path:** ~/src/bryanboettcher/homelab/

Bryan's personal infrastructure is itself a distributed system:
- **3-node Talos Kubernetes cluster** with shared-nothing architecture
- **LINSTOR/DRBD** for synchronous block-level replication across nodes
- **ArgoCD** for GitOps-driven deployment orchestration
- **MetalLB** for load balancer IP allocation
- **Traefik** for ingress routing with automatic TLS via cert-manager

The storage architecture has four tiers designed for different consistency/performance tradeoffs:
- `local-path`: Ephemeral (no replication)
- `endurance`: Write-heavy workloads (PM953 enterprise SSDs)
- `performance`: Critical HA with synchronous replication (990 PRO)
- `general-ha`: Configuration and staging (NFS-backed)

This demonstrates understanding of distributed system tradeoffs (consistency vs. availability vs. performance) applied to infrastructure design, not just application code.

---

## Evidence: Stack Overflow — Architecture Questions

### Question: "Are we queueing and serializing properly?" (Score: 13, 2,191 views)
**Site:** Software Engineering Stack Exchange
**URL:** https://softwareengineering.stackexchange.com/users/17309/ (accessible via profile)

Asked a substantive question about distributed queuing and serialization architecture, receiving community engagement that validated the architectural thinking while refining the approach.

---

## Summary

Bryan's distributed systems experience spans:
- **Framework internals:** Deep enough to contribute saga repository implementations to MassTransit core
- **Production systems:** Saga-orchestrated pipelines processing millions of records at Call-Trader
- **Architecture design:** DDD bounded contexts with polyglot persistence and event-driven integration
- **Infrastructure:** Kubernetes clusters with replicated storage and GitOps deployment
- **Operational patterns:** Fault-tolerant state machines with compensating actions, bidirectional transitions, and independent retriability

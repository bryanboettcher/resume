---
skill: Distributed Systems & Event-Driven Architecture
tags: [MassTransit, sagas, state-machines, DDD, event-driven, messaging, RabbitMQ, microservices]
relevance: Demonstrates deep expertise in message-driven distributed systems with real production implementations and open source framework contributions
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

### Import State Machine
Orchestrates the full lifecycle of a direct mail data import:
```
Upload → Stage → MatchAddresses → NormalizeAddresses → MigrateData → Complete
```
- Each transition is a discrete, independently retriable step
- Fault handling at every stage with compensating actions
- State is persisted via saga repository (surviving process restarts)
- Processes 50K–500K records per import through this pipeline

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

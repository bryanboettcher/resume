---
title: MassTransit Consumer Implementation Patterns
tags: [masstransit, consumers, csharp, message-driven, retry, fault-handling, batch-consumer, job-consumer, routing-slip, consumer-definition, dapper, dependency-injection, concurrency]
related:
  - evidence/distributed-systems-architecture.md
  - evidence/masstransit-contract-design.md
  - evidence/dapper-async-data-access.md
  - evidence/dependency-injection-composition.md
  - projects/call-trader-madera.md
children:
  - evidence/masstransit-consumer-patterns-crud-respondif.md
  - evidence/masstransit-consumer-patterns-consumer-definitions.md
  - evidence/masstransit-consumer-patterns-job-consumers.md
  - evidence/masstransit-consumer-patterns-advanced.md
category: evidence
contact: resume@bryanboettcher.com
---

# MassTransit Consumer Implementation Patterns — Index

The Madera/Call-Trader platform (madera-apps repository) contains 30+ MassTransit consumer classes spanning six domain areas: DirectMail core entity management, import pipeline processing, address normalization, Convoso call log imports, Ringba call tracking, and disposition data processing. The KbStore e-commerce platform (kb-platform repository) adds cross-bounded-context consumers that bridge Catalog and Storefront domains.

The consumers use three MassTransit consumer types and four distinct patterns:

| Pattern | Consumer Type | Count | Examples |
|---------|--------------|-------|----------|
| CRUD entity management | `IConsumer<T>` (multi-interface) | 5 classes | BrokerConsumers, VerticalConsumers, PublisherConsumers, MailHouseConsumers, CreativeConsumers |
| Pipeline stage processing | `IConsumer<T>` with ConsumerDefinition | ~10 classes | DataMigrationConsumer, AddressExportConsumer, StageAddressesConsumer, ShakeMailFileConsumer, CreateLeadConsumer |
| Long-running import jobs | `IJobConsumer<T>` | 4 classes | BeginConvosoImportConsumer, BeginDispoImportConsumer, BuildImportReportsConsumer, ConvosoFtpDownloadJobConsumer |
| Activity orchestration | Routing slip builder | 1 class | RingbaStartDownloadConsumer |

The full evidence is split into focused documents:

## Child Documents

- **[CRUD Consumers and RespondIf](masstransit-consumer-patterns-crud-respondif.md)** — The multi-interface sealed class pattern for reference data entities (Broker, Vertical, Publisher, MailHouse, Creative). Property-level change detection in Update consumers. The `RespondIf<TResponse>` extension that enables consumers to serve both request/response and fire-and-forget modes without branching.

- **[ConsumerDefinition Endpoint Tuning](masstransit-consumer-patterns-consumer-definitions.md)** — Per-consumer transport configuration via paired `ConsumerDefinition<T>` classes. Covers the deadlock-to-typed-exception bridge for SQL Server migrations, rate-limited batch consumer configuration driven from options, incremental retry for cascading deletes, and the `DBConcurrencyException` deadlock strategy for Ringba.

- **[IJobConsumer for Long-Running Work](masstransit-consumer-patterns-job-consumers.md)** — Three consumer families that use `IJobConsumer<T>` for work exceeding normal message timeouts. Import pipeline coordinators with scoped DI, open-generic pipeline resolution by buyer name, and checkpointed report generation with `SaveJobState`/`SetJobProgress`.

- **[Advanced Patterns](masstransit-consumer-patterns-advanced.md)** — Routing slip construction for the Ringba download pipeline, batch consumers for throughput (NormalizeAddress, StageAddresses), deliberate outbox bypass for high-volume publishing (AddressExport), self-scheduling FTP polling via `IMessageScheduler`, cross-bounded-context event bridging with graduated error handling (kb-platform), and fault-forward pipeline design.

---
title: Direct Mail Domain System
tags: [direct-mail, masstransit, saga-orchestration, angular, sql-server, dapper, etl, address-normalization, composable-filters, domain-modeling]
children:
  - projects/direct-mail-platform-import-pipeline.md
  - projects/direct-mail-platform-mail-files.md
related:
  - projects/call-trader-madera.md
  - evidence/masstransit-contract-design.md
  - evidence/masstransit-consumer-patterns.md
  - evidence/etl-pipeline-framework.md
  - evidence/address-normalization-pipeline.md
  - evidence/sql-server-database-engineering.md
  - evidence/sql-analytical-views.md
  - evidence/dapper-async-data-access.md
  - evidence/domain-driven-modeling.md
  - evidence/aspnet-minimal-api-patterns.md
  - evidence/angular-service-patterns.md
  - evidence/dependency-injection-composition.md
  - evidence/authentication-authorization.md
category: project
contact: resume@bryanboettcher.com
---

# Direct Mail Domain System — Index

The Direct Mail subsystem is the core business domain within the Madera platform at Call-Trader, a direct mail marketing company. It handles the full lifecycle of a physical mail campaign: importing recipient lead lists from publishers, normalizing their addresses against USPS standards, selecting which recipients to include in a mailing via composable filters, generating the output files that mail houses use to print and send physical mail, and tracking performance after the fact.

This is not a simple CRUD application. A single import of 50,000–500,000 recipient records must be staged into SQL Server, have its addresses matched against 10–15 million existing records, send any unknown addresses through external USPS normalization, migrate verified data into production tables, and then become available for mail file selection — all orchestrated through MassTransit saga state machines over RabbitMQ. The system processes approximately 20 imports per month and manages 30 million total recipients.

## Domain Model

The Direct Mail domain centers on a handful of core entities connected through a publisher/broker hierarchy: **Publishers** supply lead lists, **Brokers** act as intermediaries, **Verticals** represent market segments, **Mail Houses** are the physical printers, and **Creatives** represent the mail piece design. These appear as strongly-typed message contract base interfaces in `Madera.Contracts.Messages.DirectMail.Core`.

## Technologies

**Backend:** .NET 9, C# 13, MassTransit over RabbitMQ, Dapper (saga persistence, filter queries), SQL Server 2022 (partitioned staging, columnstore indexes, views), SqlBulkCopy, Lob API (USPS normalization), ClearScript V8 (JavaScript transforms)

**Frontend:** Angular 19, TypeScript 5.8, RxJS (interval polling, observable composition), Angular Material

**Infrastructure:** .NET Aspire orchestration, Docker multi-stage builds, GitHub Actions CI/CD

## Child Documents

- **[Import Pipeline and Address Normalization](direct-mail-platform-import-pipeline.md)** — The `ImportStateMachine` five-state orchestration: staging via `SqlBulkCopy`, address matching, Lob USPS normalization pipeline, data migration, error recovery. The `AddressStateMachine` with CRC64-based deduplication and global cache-hit behavior. SQL schema for `stage`, `data`, `ref`, and `reports` schemas. Angular real-time import status with auto-polling and restart buttons.

- **[Mail File Lifecycle and Composable Filter System](direct-mail-platform-mail-files.md)** — The `MailFileStateMachine` bread-baking metaphor (Kneading → Shaking → Baking → Complete) with bidirectional transitions, `GroupingsVersion` optimistic concurrency, and recipient count estimation before committing. The 11-filter polymorphic hierarchy with `[JsonDerivedType]` discriminators, AND-within-group / OR-across-groups execution, and unbuffered streaming for large filter results. Angular filter configuration UI.


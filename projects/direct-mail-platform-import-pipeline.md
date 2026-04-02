---
title: Direct Mail Domain System — Import Pipeline and Address Normalization
tags: [direct-mail, masstransit, saga-orchestration, sql-server, address-normalization, etl, sqlbulkcopy, partitioned-tables, dapper]
related:
  - projects/direct-mail-platform.md
  - projects/direct-mail-platform-mail-files.md
  - projects/call-trader-madera.md
  - evidence/masstransit-contract-design.md
  - evidence/masstransit-consumer-patterns.md
  - evidence/address-normalization-pipeline.md
  - evidence/etl-pipeline-framework.md
category: project
contact: resume@bryanboettcher.com
parent: projects/direct-mail-platform.md
---

# Direct Mail Domain System — Import Pipeline and Address Normalization

The import and address normalization subsystem handles the full pipeline from CSV upload to verified, production-ready recipient data. A single import of 50,000–500,000 recipient records must be staged into SQL Server, have its addresses matched against 10–15 million existing records, send any unknown addresses through external USPS normalization, migrate verified data into production tables — all orchestrated through MassTransit saga state machines over RabbitMQ.

---

## The ImportStateMachine

An import begins when a user creates a new import record through the Angular UI, specifying the publisher, broker, vertical, cost per lead, and optionally a JavaScript transformation script. The `ImportStateMachine` in `Madera.Dataflows.DirectMail/Sagas/ImportStateMachine.cs` orchestrates the entire process through five states:

**UploadingFile** — The initial state after `CreateImportCommand` is processed. The saga captures the filename (used for duplicate detection via `CorrelateBy((s, c) => s.Filename == c.Message.Filename)`) and awaits a file upload. When `UploadImportCommand` arrives with a `MessageData<Stream>` reference to the uploaded CSV, the saga dispatches a `FileImportRequest` to the ETL pipeline.

**ImportFile.Pending** — The ETL pipeline bulk-loads the CSV into a partitioned staging table (`stage.Recipients`) using `SqlBulkCopy`. The staging table uses `FILLFACTOR=60` and `PAGE` compression, partitioned by `ImportPartition` on a partition scheme (`PS_ImportLog`). Each recipient gets a `RecipientHash` and an `AddressHash` computed from their address fields. When staging completes, the saga transitions to address matching.

**MatchAddresses.Pending** — The `MatchAddressesConsumer` runs with `UseConcurrencyLimit(1)` and `UseConcurrentMessageLimit(1)` — only one import can match addresses at a time, because this phase writes to shared address lookup tables. It calls `IDirectMailImportService.MarkAddressesForExport()` to identify which staged addresses don't yet exist in the production `data.Addresses` table. Addresses that need normalization get flagged for external processing. The consumer has aggressive retry configuration: 20 incremental retries starting at 2 seconds with 4-second increments.

**NormalizingAddresses** — This is the longest phase. Unknown addresses are sent to the Lob API for USPS verification in batches of 20. The `AddressCountsUpdatedEvent` periodically updates the saga with progress (`ImportedRowCount` total vs. `AddressPendingCount` remaining). The saga publishes updated status messages showing completion percentage via `GetAddressProgress()`. When `AddressPendingCount` reaches zero, the saga automatically transitions forward — detected by the guard `IsFullyNormalized`: `context.Saga is { ImportedRowCount: > 0, AddressPendingCount: 0 }`.

**MigrateData.Pending / Complete** — Verified recipients are migrated from staging to production `data.Recipients` with their normalized address IDs. On completion, the saga publishes `ImportCompletedEvent` and enters the terminal state.

**Error Recovery** — The saga supports two restart operations from the `Errored` state: `AddressNormalizationRestartCommand` re-exports addresses for normalization, and `RecipientMigrationRestartCommand` retries the migration step.

The `DirectMailImport` saga entity stores detailed timing: `CreatedOn`, `ImportInitiatedOn`, `ImportCompletedOn`, `AddressNormalizationInitiatedOn`, `AddressNormalizationCompletedOn`, `CompletedOn`, and `LastUpdatedOn`. The saga entity is persisted to SQL Server via a custom Dapper-based repository (`ImportsDatabaseContext`) implementing MassTransit's `DatabaseContext<DirectMailImport>`, with `RepeatableRead` isolation to prevent concurrent saga mutations.

---

## The AddressStateMachine

Addresses are managed by their own saga state machine with two states: `Unverified` and `Verified`.

The key design decision is CRC64-based deduplication using saga correlation. The `OnEnsure` event correlates by `AddressHash` rather than `CorrelationId`: `e.CorrelateBy((s, c) => s.AddressHash == c.Message.AddressHash)`. When a new address arrives that matches an existing hash, the saga doesn't create a duplicate — it either publishes a merge event (if the incoming message has a different `CorrelationId`) or confirms the existing verified address.

This means address normalization is a global operation — once any import normalizes an address, every subsequent import containing that address gets an instant cache hit through the state machine correlation, with no external API call needed.

---

## Data Layer

The SQL schema reflects the domain's staging pipeline architecture:

**stage** — Partitioned staging tables (`stage.Recipients`) with `FILLFACTOR=60` and `PAGE` compression on a partition scheme (`PS_ImportLog`). Indexes on `RecipientId`, `ImportId+AddressHash`, and `RecipientHash` support the different lookup patterns needed during address matching and deduplication.

**data** — Production tables holding verified recipients, addresses, and import logs. The `data.DirectMailImportSagas` table stores the import saga state with indexes on `CurrentState` and `ImportLogId`.

**ref** — Reference data: publishers, brokers, verticals, the `BrokerPublisherLink` many-to-many, mail houses, creatives.

**reports** — Denormalized reporting tables like `reports.Imports` that pre-join saga state with publisher/vertical/broker details, removing the need for expensive report-time joins.

---

## Angular Import Status UI

The Angular frontend polls the saga status endpoint every 2 seconds via `interval(2000).subscribe(() => this.fetchData())`. It displays all timestamp milestones, automatically stops polling when `completedOn` is set, and provides restart buttons for address normalization and data migration when errors occur.

---

## Key Files

- `madera-apps:Madera/Madera.Dataflows.DirectMail/Sagas/ImportStateMachine.cs` — 5-state import orchestration saga
- `madera-apps:Madera/Madera.Dataflows.DirectMail/Domains/Addresses/AddressStateMachine.cs` — 2-state address lifecycle with deduplication
- `madera-apps:Madera/Madera.Dataflows.DirectMail/Domains/Imports/DirectMailImport.cs` — Saga entity with 8 timestamp milestones
- `madera-apps:Madera/madera.ui.client/src/app/dm-import-status.component.ts` — Angular real-time import monitoring

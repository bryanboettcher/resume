---
title: Madera Direct Mail Platform
tags: [dotnet, masstransit, angular, etl, address-normalization, direct-mail, sql-server, rabbitmq, dapper, aspire, ci-cd, performance, saga-orchestration]
related:
  - evidence/distributed-systems-architecture.md
  - evidence/data-engineering-etl.md
  - evidence/etl-pipeline-framework.md
  - evidence/performance-optimization.md
  - evidence/frontend-web-development.md
  - evidence/leadership-mentoring.md
  - evidence/dotnet-csharp-expertise.md
  - evidence/infrastructure-devops.md
  - evidence/cloud-azure-experience.md
  - evidence/ai-driven-development.md
  - projects/fastaddress-research.md
  - projects/career-history.md
category: project
contact: resume@bryanboettcher.com
---

# Madera Direct Mail Platform — Project Narrative

## Context

Call-Trader is a direct mail marketing company. Bryan joined to lead a ground-up rewrite of their data processing platform from a Node.js/Express system ("Madera Digital") to a modern .NET 9 architecture. The platform manages recipient data for direct mail campaigns — importing leads from multiple sources, normalizing and deduplicating addresses, managing campaign assignment, and generating physical mail files for mail houses.

## Scale

- **30 million total recipients** in the database
- **10–15 million unique addresses** requiring deduplication
- **Average import:** 50,000 recipients; **peak:** 500,000 recipients per import
- **Import frequency:** ~20 imports per month
- **Codebase:** 625+ source files, 14 .NET projects, 203 SQL migrations

## What Bryan Built

### Multi-Source ETL Pipeline Framework
Designed a generic `IPipelineSource → IPipelineProcessor → IPipelineSink` architecture used across 4 data sources:
- **DirectMail:** CSV imports with V8-powered field transformations
- **Convoso:** Automated FTP downloads of dialer lead data
- **Ringba:** Multi-step REST API integration for call tracking data
- **Dispos/Tranzact:** Disposition data for lead outcome tracking

### Address Normalization Pipeline
- Lob API integration (USPS-certified, bulk endpoint, 20 addresses/request)
- CRC64 hash-based deduplication across 10-15M unique addresses
- 17-flag bitwise enum for address metadata (deliverability, vacancy, residential/commercial)
- In-memory cache: 200MB for 2M addresses, <100ns lookup time
- Multi-stage dedup: internal (within batch) → external (across all data)

### Saga State Machine Orchestration
- **ImportStateMachine:** Upload → Stage → MatchAddresses → NormalizeAddresses → MigrateData → Complete
- **MailFileStateMachine:** Kneading → Shaking → Baking → Complete (with bidirectional transitions)
- Fault handling at every stage with compensating actions
- MassTransit over RabbitMQ with custom Dapper-based saga persistence

### Real-Time Target Optimizer
- Automatic call routing optimization based on Revenue Per Call
- Runs every 17 minutes during business hours
- Snub detection (10+ consecutive non-conversions)
- Tiered priority assignment

### Prediction Reporting
- Response rate forecasting using volume-weighted historical data
- 5-tier confidence hierarchy

### 12-Filter Composable Recipient Selection
ImportBatch, IncludedState, IncludedVerticals, OriginalPublishers, MultiPublishers, TimesMailed, DaysSinceMailing, DateOfBirth, UnscrubbedLeads, ExternalDuplicates, ZipList, LastMailDate

### Embedded JavaScript Transformation Engine
V8 (Microsoft.ClearScript) for user-configurable field transformations during import, allowing non-developers to write simple mapping scripts.

## Performance Achieved

| Metric | Target | Achieved |
|--------|--------|----------|
| Import throughput | 5,000 rows/sec | <10 sec for 50K rows |
| Address lookup | <100ns | O(1) dictionary cache |
| Address normalization | — | 15M in <2 hours |
| API reads | <200ms p95 | Met |
| API writes | <500ms p95 | Met |
| Memory (web app) | <2 GB container | 650MB typical / 900MB peak |

All performance targets were met on modest hardware without infrastructure expansion — the optimization work was explicitly chosen over expensive hardware scaling.

## Technology Stack

**Backend:** .NET 9, C# 13, ASP.NET Core Minimal APIs, MassTransit, Dapper, EF Core, SQL Server 2022, RabbitMQ, SqlBulkCopy, Sep, Sylvan, ClosedXML, FluentFTP, FluentValidation, OpenTelemetry, Lamar IoC, Hangfire, .NET Aspire

**Frontend:** Angular 19, TypeScript 5.8, Angular Material, CoreUI, PapaParse, Levenshtein matching, RxJS

**Infrastructure:** Docker (multi-stage), GitHub Actions (8 workflows), GitHub Container Registry, .NET Aspire orchestration

**Testing:** NUnit3 (BDD nested classes), NSubstitute, Shouldly, Testcontainers, SQLite in-memory

## Team & Leadership

3-person team. Bryan: architecture + infrastructure + mentoring (426 commits). Sophie Walker: primary feature development (1,052 commits). Lillian Fleming: junior contributions (18 commits). Bryan designed the architectural patterns and pipeline frameworks that enabled Sophie's high-velocity feature development.

## Business Impact

The Node.js system had three concrete operational problems that motivated the rewrite:

**Reliability:** Any transient error — a single API timeout, a network blip — crashed the import process and left data half-imported. Recovering required developer intervention. Until someone manually fixed the state, all downstream reports were corrupted. This happened regularly at ~20 imports/month.

**Throughput:** There was no queuing. Imports had to be run one at a time and babysitted. Large batches created enormous operations backlogs. The saga-based rewrite allows concurrent imports with automatic fault recovery and no manual state repair.

**Deduplication accuracy:** The legacy system deduplicated on `address1 + zipcode` only — a 15% false-unique rate on real data. Direct mail batches are round numbers (50,000 recipients). Dedup failures don't just waste postage on duplicates — they displace better-performing leads from those slots. At $0.20/piece postage plus mailer production costs, the math is straightforward. There's also a legal dimension: industry standard is ~3 contacts per lead over 3 months. Dedup failures inflate contact counts against the same household, creating regulatory exposure.

The CRC64 hash-based deduplication against 10–15M addresses, combined with the 17-flag bitwise address metadata enum, eliminated the false-unique problem and gave the business the foundation for accurate deliverability and contact-frequency tracking.

**Schema quality:** The legacy system had 26 nullable VARCHAR columns with no typed data. Reports used `TRY_CONVERT` everywhere. Inline HTML was generated per endpoint. This propagated SQL injection surface area and N+1 query patterns across every report. The rewrite introduced proper datatypes — enabling actual SQL math instead of `TRY_CONVERT` chains — and a single shared report structure that eliminated the per-endpoint bug surface entirely.

## Legacy System Context

The rewrite expanded the system from 45 features across 9 domains (Node.js/Express) to 100+ features across 12 domains (.NET 9), with a 60% feature overlap and 40% net-new capabilities. The operational problems above — crash-on-any-error imports, no concurrency, 15% dedup false-unique rate, untyped schema — were the direct motivation. The architectural upgrade introduced MassTransit saga orchestration, generic pipeline framework, address normalization caching, and comprehensive testing infrastructure not present in the original system.

## Advanced Patterns (from branch analysis)

### Multi-Tenant Single-Binary Deployment
The same `Madera.Workflows` binary is deployed as 4 separate instances via .NET Aspire, each differentiated by the `Madera__Dataflow` environment variable. Convoso, Dispos, DirectMail, and Ringba workflows share identical code but process only their designated data source at runtime — eliminating per-dataflow build artifacts while maintaining runtime isolation.

### SQL Server In-Memory OLTP
Import staging uses memory-optimized tables (`MEMORY_OPTIMIZED = ON, DURABILITY = SCHEMA_ONLY`) with hash indexes (`BUCKET_COUNT=4096`) for O(1) address deduplication during bulk processing. Memory-optimized TVPs pass bulk data to stored procedures without tempdb overhead.

### Saga Entity Deduplication
An `AddressStateMachine` prototype uses CRC64 hash-based saga correlation for automatic cross-batch address deduplication. When duplicates arrive, the saga publishes merge events and finalizes the duplicate rather than rejecting it — maintaining referential integrity across independent import batches.

### Incremental ORM Migration
Gradual Dapper → EF Core transition using a dual-access consumer pattern (both `IConnectionProvider` and `DirectMailDbContext` injected). Complex reports stay as stored procedures; CRUD migrates to EF Core. The `DirectMailDbContext` extends MassTransit's `SagaDbContext` to share a single context for domain entities and saga state.

### Custom .NET Aspire Resources
Extended Aspire with custom resource types: `FileStore` (shared bind-mount abstraction for MassTransit MessageData) and `GroupResource` (visual dashboard hierarchy with `ResourceNotificationService` eventing). The AppHost orchestrates 8+ services with dependency ordering via `WaitFor()` and `WaitForCompletion()`.

## What This Project Begat

The address normalization work at Call-Trader directly inspired the FastAddress research project — Bryan recognized that exact string matching missed legitimate address variations (abbreviation differences, compound word splitting, direction variations) and began designing a semantic matching system using neural embeddings to capture these patterns.

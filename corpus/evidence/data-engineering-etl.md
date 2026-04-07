---
title: Data Engineering & ETL Pipelines
tags: [etl, data-processing, csv, address-normalization, deduplication, sql-server, sqlbulkcopy, dapper, ef-core, pipeline, crc64, lob-api]
related:
  - evidence/etl-pipeline-framework.md
  - evidence/performance-optimization.md
  - projects/call-trader-madera.md
  - projects/fastaddress-research.md
  - evidence/distributed-systems-architecture.md
  - evidence/dotnet-csharp-expertise.md
  - links/stackoverflow.md
category: evidence
contact: resume@bryanboettcher.com
---

# Data Engineering & ETL Pipelines — Evidence Portfolio

## Overview

Bryan has built production ETL systems processing tens of millions of records with strict performance targets. His data engineering work combines high-throughput batch processing with real-time API integration, address normalization, and sophisticated deduplication logic. The FastAddress project extends this into research-grade semantic matching.

---

## Evidence: Madera/Call-Trader — Multi-Source ETL Framework

**Repository:** github.com/Call-Trader/madera-apps (private)
**Scale:** 30M total recipients, 10-15M unique addresses, ~20 imports/month

### Pipeline Architecture
Built a generic, reusable pipeline framework using the pattern `IPipelineSource → IPipelineProcessor → IPipelineSink`, applied consistently across four data sources:

#### 1. DirectMail Pipeline (Primary)
- **Source:** CSV file upload (50K–500K rows per import)
- **Processing:**
  - Client-side CSV parsing (PapaParse) with Levenshtein distance column mapping
  - Server-side parsing via Sep/Sylvan high-performance CSV libraries
  - Configurable JavaScript-based field transformations (V8 engine via Microsoft.ClearScript)
  - Pre-LOB internal duplicate detection (address + ZIP partitioning)
  - Lob API USPS-certified address normalization (bulk endpoint, 20/request)
  - Post-LOB external duplicate detection (cross-batch, cross-vertical)
  - Age eligibility validation per vertical (TCPA compliance)
  - CRC64 hash-based deduplication across 10-15M unique addresses
- **Sink:** SqlBulkCopy batch inserts (5,000 records/batch) into SQL Server 2022
- **Performance:** <10 seconds for 50K rows (5,000 rows/second sustained)

#### 2. Convoso Pipeline (Automated)
- **Source:** Automated FTP downloads (FluentFTP) of dialer lead data on cron schedule
- **Processing:** Data normalization and mapping
- **Sink:** Database ingestion with deduplication

#### 3. Ringba Pipeline (API Integration)
- **Source:** REST API with multi-step workflow:
  1. Request report generation (GetReportId)
  2. Poll for completion (GetDownloadUri)
  3. Download report data
  4. Unzip and parse
- **Processing:** Call tracking data normalization
- **Sink:** Database ingestion with lead matching

#### 4. Dispos/Tranzact Pipeline
- **Source:** Disposition data files from Tranzact
- **Processing:** Lead outcome tracking and status updates
- **Sink:** Database updates linking dispositions to existing leads

### Orchestration
All pipelines orchestrated via MassTransit saga state machines:
- **ImportStateMachine:** Upload → Stage → MatchAddresses → NormalizeAddresses → MigrateData → Complete
- Each step is independently retriable with fault handling
- State persisted via saga repository (survives process restarts)

### Address Normalization Details
- **Lob API integration:** USPS-certified address verification via bulk endpoint
- **Bitwise metadata flags (17 flags):** ZIP code type, residential/commercial classification, deliverability status, vacancy detection, phantom address detection
- **In-memory address cache:** 200 MB for 2M addresses at 100 bytes per address
- **Cache lookup speed:** <100ns per lookup (O(1) dictionary)
- **Deduplication:** CRC64 hashing across the full 10-15M address corpus
- **Original field preservation:** Both raw input and normalized output stored for audit trail

### Data Quality Pipeline
Multi-stage quality validation:
1. Format validation (field types, required fields)
2. Internal duplicate detection (within the import batch)
3. Address normalization and verification (Lob API)
4. External duplicate detection (across all existing data)
5. Age eligibility verification (TCPA compliance per vertical)
6. Address deliverability classification
7. Vacancy and phantom address detection

---

## Evidence: FastAddress — Semantic Address Matching Research

**Repository:** https://github.com/bryanboettcher/FastAddress
**Local path:** ~/src/bryanboettcher/FastAddress/

### Purpose
Research project developing high-performance semantic address matching that evolved directly from the Call-Trader import pipeline work. The insight: exact string matching misses legitimate address variations that a human would recognize as identical.

### Training Data Strategy (designed for ML pipeline)
**Positive pairs (should match):**
- Abbreviation variations: "STREET"/"ST", "AVENUE"/"AVE"
- Compound word splitting: "ELMWOOD"/"ELM WOOD"
- Direction variations: "SW MAIN"/"S WEST MAIN"
- Punctuation: "P.O. BOX"/"PO BOX"

**Hard negative pairs (must NOT match):**
- Different streets: "SOMMERSET DR" vs "SUNSET BLVD"
- Opposite directions: "N MAIN" vs "S MAIN"
- Different house numbers: "100 MAIN" vs "1000 MAIN"

### Data Processing Design
- **USPS normalization rules:** 220+ hardcoded abbreviation mappings
- **Token pipeline:** Tokenization → lexical classification → domain normalization → domain classification → EntityDigest construction
- **Zero-allocation processing:** Entire pipeline designed to avoid GC pressure

---

## Evidence: Address Normalization Service

**Repository:** https://github.com/bryanboettcher/address-normalizing

Microservice wrapper for third-party address normalization services with caching and throttling for distributed consumers. Demonstrates the pattern of wrapping expensive external APIs with local caching and rate limiting.

---

## Evidence: Stack Overflow — Data Processing Questions

### "Storing large, rarely-changing data" (Score: 15, 29K views)
**Site:** Software Engineering SE

Asked about efficient C# data structures for large, infrequently-updated datasets — directly relevant to the address cache design pattern used in production.

### "What's an appropriate search/retrieval method for a VERY long list of strings?" (Score: 62 answer)
**Site:** Stack Overflow

The benchmark answer comparing HashSet vs. sorted array for large-scale lookups is directly applicable to the deduplication problem space.

---

## Evidence: SQL & Database Expertise

### Madera Database
- **203 SQL migration files** — extensive schema evolution
- **SQL Server 2022** with optimized indexing for address lookups
- **SqlBulkCopy** for high-throughput batch inserts
- **Dapper** for performance-critical queries (avoiding EF Core overhead on hot paths)
- **Entity Framework Core** for CRUD operations where ORM convenience outweighs performance needs

### Incremental ORM Migration Strategy (Dapper → EF Core)
**Branch:** `efcore3-toyko-drift`

Rather than a risky big-bang ORM migration, Bryan designed a gradual transition strategy:
- **Dual-access consumer pattern:** Consumers inject *both* `IConnectionProvider` (Dapper) and `DirectMailDbContext` (EF Core). Complex report queries remain as stored procedures via Dapper while CRUD operations for reference entities (Publishers, Brokers, Creatives, Verticals, ZipCodeLists) migrate to EF Core with `ExecuteDeleteAsync` and snapshot isolation transactions.
- **SagaDbContext extension:** `DirectMailDbContext` extends MassTransit's `SagaDbContext`, so saga state and domain entities share a single context with fluent model configuration. This cohabitation pattern avoids the overhead of multiple DbContexts while preserving MassTransit saga persistence.
- **Generic paginated service base:** `EfCorePaginatedServiceBase<TModel, TQuery>` with individual services (e.g., `EfCoreBrokerService`) overriding `BuildQuery()` to return `IQueryable<T>` projections. Clean decomposition from the prior monolithic `SqlQueryServices.cs` (189 lines deleted, replaced by 6 focused service files).
- **Obsolete markers for migration discipline:** Internal concrete implementations of contract interfaces (e.g., `InlinePublisherModel : PublisherModel`) marked `[Obsolete]` to prevent user-code usage during the transition period.

### Career History
- Multiple roles involving MSSQL (listed as primary skill on resume)
- Kansys (2020-2023): Telecom billing data — inherently high-volume data processing
- Earlier roles: MySQL, NoSQL, ElasticSearch exposure

---

## Summary

Bryan's data engineering expertise includes:
- **High-volume ETL:** Production pipelines processing 30M+ records with sub-second performance targets
- **Multi-source integration:** CSV, FTP, REST API, and file-based data sources unified through a generic pipeline framework
- **Address normalization:** USPS-certified verification at scale with intelligent caching and deduplication
- **Data quality:** Multi-stage validation pipelines with compliance checks (TCPA)
- **Semantic matching:** Research-grade address comparison combining exact matching with neural embeddings
- **Database optimization:** SqlBulkCopy, Dapper for hot paths, proper indexing, CRC64 hash-based lookups

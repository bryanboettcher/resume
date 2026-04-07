---
title: ETL Pipeline Framework Design
tags: [etl, pipeline-architecture, iasyncenumerable, streaming, sqlbulkcopy, composable-transformers, data-import, generics, dependency-injection, csharp]
related:
  - evidence/data-engineering-etl.md
  - evidence/performance-optimization.md
  - evidence/dotnet-csharp-expertise.md
  - evidence/distributed-systems-architecture.md
  - projects/call-trader-madera.md
children:
  - evidence/etl-pipeline-framework-architecture.md
  - evidence/etl-pipeline-framework-rawrow.md
  - evidence/etl-pipeline-framework-transformers.md
  - evidence/etl-pipeline-framework-pipeline-variations.md
  - evidence/etl-pipeline-framework-sink-observability.md
category: evidence
contact: resume@bryanboettcher.com
---

# ETL Pipeline Framework Design — Index

Bryan builds data processing pipelines as composable, streaming abstractions rather than monolithic import routines. The Madera platform's `Madera.Common/Importing/` framework separates concerns into four discrete stages — source, process, observe, sink — connected via `IAsyncEnumerable<T>` with no intermediate buffering. Four data source domains (DirectMail, Convoso, Ringba, Dispos) each implement the same interfaces with genuinely different processing strategies. The DirectMail pipeline processes 50,000-row imports in under 10 seconds at 5,000 rows/second sustained.

## Child Documents

- **[Architecture and Coordinator Pattern](etl-pipeline-framework-architecture.md)** — The four pipeline interface contracts (`IPipelineSource`, `IPipelineProcessor`, `IPipelineProgress`, `IPipelineSink`), the `ImportPipelineCoordinator<TModel>` that chains them into a single `IAsyncEnumerable<T>` stream, and how generic type parameters allow DI to resolve the correct implementation per domain.

- **[RawRow Schema-Agnostic Row Representation](etl-pipeline-framework-rawrow.md)** — Array-backed property bag with case-insensitive keys, static type conversion registry (reflection-at-startup, cached `MethodInfo`), `UpdateIf` combinator, `Rename` operation, and multiple factory methods. Bridges schema differences across four external data providers without per-provider intermediate models.

- **[Composable Row Transformers](etl-pipeline-framework-transformers.md)** — `IRowTransformer<TModel>` with `ushort Order` for DI-collected, sorted execution. Eight DirectMail transformers at order points 10,000/20,000/25,000/30,000 covering sanitization → hashing → age calculation → scrubbing → partition assignment. Scrubbing sets bitflags rather than filtering rows, preserving data lineage.

- **[Concrete Pipeline Variations](etl-pipeline-framework-pipeline-variations.md)** — How DirectMail (ordered transformers), Convoso (inline LINQ with `ICachingLookup<T>`), Ringba (`.Rename()` + MD5 dedup), and Dispos (minimal — phone sanitization + filter) each implement the same interfaces with fundamentally different strategies. Plus six source adapters (CSV/TSV/PSV in file and stream variants) using Sylvan `CsvDataReader`.

- **[Sink, Observability, and Testing](etl-pipeline-framework-sink-observability.md)** — `BulkCopyPipelineSinkBase` bridges `IAsyncEnumerable<T>` → FastMember `ObjectReader` → SqlBulkCopy with streaming enabled. `ConsoleLoggingPipelineProgress` emits records/second every 10,000 rows with `Lazy<Stopwatch>`. Processor-level tests with BDD `When_X` nested-class pattern and mocked lookup dependencies.

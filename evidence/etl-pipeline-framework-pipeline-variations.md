---
title: ETL Pipeline Framework — Concrete Pipeline Variations
tags: [etl, pipeline-architecture, iasyncenumerable, csharp, data-import, generics, caching, md5-hashing]
related:
  - evidence/etl-pipeline-framework.md
  - evidence/etl-pipeline-framework-architecture.md
  - evidence/etl-pipeline-framework-transformers.md
  - evidence/etl-pipeline-framework-rawrow.md
  - evidence/etl-pipeline-framework-sink-observability.md
  - evidence/data-engineering-etl.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/etl-pipeline-framework.md
---

# ETL Pipeline Framework — Concrete Pipeline Variations

The four Madera pipeline implementations demonstrate that the framework accommodates genuinely different processing patterns rather than forcing everything into one mold. DirectMail uses the ordered transformer chain for its complex multi-step transformation. Convoso uses inline LINQ with `ICachingLookup<T>` for lookup-heavy processing. Ringba uses `.Rename()` for column remapping and MD5 for call deduplication. Dispos is the simplest — phone sanitization and a sale/no-sale filter — using the same framework without wasted abstraction.

---

## Evidence: Concrete Pipeline Variations

Each data source implements the pipeline differently, demonstrating that the framework accommodates genuinely different processing patterns rather than forcing everything into one mold:

**DirectMail** uses the `TransformingPipelineProcessor` base class with eight ordered `IRowTransformer` implementations. This is the most complex pipeline — it needs address normalization, hash computation, age calculation, and multiple scrubbing rules applied in a specific sequence.

**Convoso** implements `IPipelineProcessor<ConvosoData>` directly with a fluent LINQ chain. Instead of ordered transformers, it uses inline `.Apply()` calls for field-level lookups via `ICachingLookup<T>` — resolving string values like campaign names and publisher names to their database IDs through an in-memory cache. The Convoso pipeline also has an `IsDataRow` filter that skips non-data rows (the Convoso export format intermixes data rows with summary rows identifiable by whether the `Id` field parses as a `long`).

**Ringba** also implements `IPipelineProcessor<RingbaData>` directly with a fluent chain. It uses `.Rename()` to remap Ringba-specific column names (e.g., `"tag:InboundNumber:State"` to `"State"`), MD5 hashing for call deduplication, and phone number stripping that normalizes varied phone formats (parentheses, dashes, country codes) to bare digit strings. Its `IsValid` filter excludes live calls (`"Is Live"` flag).

**Dispos (Tranzact)** is the simplest — phone sanitization, a sale/no-sale filter, and direct DTO conversion. No transformers, no lookup caches, no complex validation. The same framework handles it without any wasted abstraction.

### Source Adapters

The framework includes six source adapters for delimited file formats, built from a `DelimitedPipelineSource` base class parameterized on separator character:

- `CsvFilePipelineSource<T>` / `CsvStreamPipelineSource<T>` (comma)
- `TsvFilePipelineSource<T>` / `TsvStreamPipelineSource<T>` (tab)
- `PsvFilePipelineSource<T>` / `PsvStreamPipelineSource<T>` (pipe)

Each pair has a file-based variant (takes a `FilenameSelector<T>` delegate) and a stream-based variant (takes a `FileDataSelector<T>` delegate). The delegates are generic on the model type so the DI container can resolve the correct source for each pipeline without ambiguity. The base class uses Sylvan's `CsvDataReader` for high-performance parsing and yields `RawRow` instances via `IAsyncEnumerable`.

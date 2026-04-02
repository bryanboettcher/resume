---
title: ETL Pipeline Framework — Architecture and Coordinator Pattern
tags: [etl, pipeline-architecture, iasyncenumerable, streaming, composable-transformers, generics, dependency-injection, csharp]
related:
  - evidence/etl-pipeline-framework.md
  - evidence/etl-pipeline-framework-rawrow.md
  - evidence/etl-pipeline-framework-transformers.md
  - evidence/etl-pipeline-framework-pipeline-variations.md
  - evidence/etl-pipeline-framework-sink-observability.md
  - evidence/data-engineering-etl.md
  - evidence/performance-optimization.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/etl-pipeline-framework.md
---

# ETL Pipeline Framework — Architecture and Coordinator Pattern

Bryan builds data processing pipelines as composable, streaming abstractions rather than monolithic import routines. The Madera platform (`Madera.Common/Importing/`) separates concerns into four discrete stages — source, process, observe, sink — connected via `IAsyncEnumerable<T>` so data flows through the pipeline without buffering entire datasets in memory. The `ImportPipelineCoordinator<TModel>` wires the four interfaces into a single chain; generic type parameters on each interface let the DI container resolve the correct implementation per domain without service locator patterns.

---

## Evidence: Madera Import Pipeline Framework Architecture

**Repository:** github.com/Call-Trader/madera-apps (private)
**Framework location:** `Madera/Madera.Common/Importing/`
**Implementations:** DirectMail, Convoso, Ringba, Dispos (four distinct data source domains)

### The Problem

The Madera direct mail platform ingests data from four fundamentally different external sources:
- **DirectMail:** CSV files from data brokers containing recipient records (50,000+ rows per import)
- **Convoso:** CSV exports from a dialer platform with 100+ columns of call log data
- **Ringba:** TSV/CSV call tracking data with publisher/buyer/campaign resolution
- **Dispos:** Disposition files from Tranzact with sale/no-sale classification

Each source has different column layouts, different validation rules, different normalization requirements, and different destination schemas. The naive approach is four separate import routines, each duplicating the parsing, batching, progress tracking, and bulk insert logic.

### Framework Architecture

The pipeline is composed from four interface contracts, each parameterized on the output model type:

```csharp
public interface IPipelineSource<TModel>
{
    IAsyncEnumerable<RawRow> ProduceData(CancellationToken token = default);
}

public interface IPipelineProcessor<out TOutput>
{
    IAsyncEnumerable<TOutput> PrepareData(IAsyncEnumerable<RawRow> input, CancellationToken token);
}

public interface IPipelineProgress<TModel>
{
    IAsyncEnumerable<TModel> Capture(IAsyncEnumerable<TModel> input, CancellationToken token);
}

public interface IPipelineSink<in TObject>
{
    Task ConsumeData(IAsyncEnumerable<TObject> source, CancellationToken token);
}
```

The coordinator wires these into a single streaming pipeline:

```csharp
public sealed class ImportPipelineCoordinator<TModel> : ImportPipelineCoordinator
{
    public override async Task RunAsync(CancellationToken token)
    {
        var source = _source.ProduceData(token);
        var transformed = _processor.PrepareData(source, token);
        var monitored = _progress.Capture(transformed, token);
        await _sink.ConsumeData(monitored, token);
    }
}
```

This is a single chain of `IAsyncEnumerable<T>` operations. Data flows from source through processing and observation into the sink without any intermediate collections or buffering. The entire pipeline processes one row at a time through the chain (with batching handled only at the sink level by SqlBulkCopy).

Generic type parameters carry domain context: `IPipelineSource<DirectMailData>` vs. `IPipelineSource<ConvosoData>` means the DI container resolves the correct source, processor, progress, and sink for each domain without service locator patterns or string-based keys.

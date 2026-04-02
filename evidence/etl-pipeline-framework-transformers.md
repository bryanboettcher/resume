---
title: ETL Pipeline Framework — Composable Row Transformers
tags: [etl, pipeline-architecture, composable-transformers, dependency-injection, csharp, data-import, scrubbing, bitflags]
related:
  - evidence/etl-pipeline-framework.md
  - evidence/etl-pipeline-framework-architecture.md
  - evidence/etl-pipeline-framework-rawrow.md
  - evidence/etl-pipeline-framework-pipeline-variations.md
  - evidence/sql-server-database-engineering-bitflag-architecture.md
  - evidence/data-engineering-etl.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/etl-pipeline-framework.md
---

# ETL Pipeline Framework — Composable Row Transformers

The DirectMail pipeline uses an ordered chain of `IRowTransformer<TModel>` implementations sorted by a `ushort Order` property. Transformers use widely-spaced order values (10,000, 20,000, 25,000) to allow insertion between existing ones without renumbering. The DI container collects all registered implementations automatically. Scrubbing transformers don't filter rows — they set bitflag values on `ScrubbedReason`, preserving data lineage for downstream analysis while allowing multiple scrub reasons to accumulate on a single row.

---

## Evidence: Composable Row Transformers

The DirectMail pipeline uses an ordered chain of `IRowTransformer<TModel>` implementations that execute in sequence on each row:

```csharp
public interface IRowTransformer<TModel>
{
    ushort Order { get; }
    ValueTask TransformInput(RawRow input, CancellationToken token);
}
```

The `Order` property uses a `ushort` with 65,535 buckets, and transformers use widely-spaced order values (10,000, 20,000, 25,000) to allow insertion of new transformers between existing ones without renumbering. The `TransformingPipelineProcessor<TModel>` base class collects all registered transformers via DI, sorts them by order, and runs them sequentially:

```csharp
public abstract class TransformingPipelineProcessor<TModel>
{
    public IAsyncEnumerable<TModel> PrepareData(IAsyncEnumerable<RawRow> input, CancellationToken token)
    {
        return input
              .Apply(r => RunTransformers(r, token))
              .Where(IsValid)
              .Select(ConvertDto);
    }
}
```

The DirectMail pipeline has eight transformers registered at specific order points:

| Order | Transformer | Purpose |
|-------|------------|---------|
| 10,000 | `SanitizingRowTransformer` | Truncate fields to DB column widths, normalize state abbreviations, parse ZIP+4 codes |
| 20,000 | `AddressHashRowTransformer` | Compute CRC64 hash from address components for deduplication |
| 20,000 | `RecipientHashRowTransformer` | Compute CRC32 hash from name for recipient matching |
| 25,000 | `CalculatedAgeRowTransformer` | Derive age from date of birth |
| 25,000 | `DobRangeScrubbingRowTransformer` | Flag records with out-of-range ages based on configurable min/max |
| 25,000 | `InvalidDobScrubbingRowTransformer` | Flag records with unparseable dates of birth |
| 25,000 | `MissingNameScrubbingRowTransformer` | Flag records without first/last names |
| 25,000 | `UnderageScrubbingRowTransformer` | Flag records below legal age threshold |
| 30,000 | `ImportPartitionRowTransformer` | Assign partition key for parallel downstream processing |

The ordering matters: sanitization (10,000) runs before hashing (20,000) so hashes are computed on clean data, and scrubbing (25,000) runs after age calculation (25,000) so age-based rules have the computed age available. New transformers can be added at any position without modifying existing ones — the DI container collects all `IRowTransformer<DirectMailData>` registrations automatically.

Scrubbing transformers don't remove rows. They set bitflag values on a `ScrubbedReason` field via an extension method (`AddScrubbedReasonFlag`), allowing multiple scrub reasons to accumulate on a single row. The row still flows through the pipeline and into the database — the scrubbed reason is used downstream for reporting and filtering, not for rejection at import time. This preserves data lineage and enables analysis of why records were flagged.

---
title: ETL Pipeline Framework — Sink, Observability, and Testing
tags: [etl, pipeline-architecture, sqlbulkcopy, streaming, iasyncenumerable, throughput, testing, bdd, csharp]
related:
  - evidence/etl-pipeline-framework.md
  - evidence/etl-pipeline-framework-architecture.md
  - evidence/etl-pipeline-framework-transformers.md
  - evidence/etl-pipeline-framework-pipeline-variations.md
  - evidence/data-engineering-etl.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/etl-pipeline-framework.md
---

# ETL Pipeline Framework — Sink, Observability, and Testing

The `BulkCopyPipelineSinkBase<TModel, TOptions>` uses FastMember's `ObjectReader` to bridge `IAsyncEnumerable<TModel>` into an `IDataReader` that SqlBulkCopy consumes directly — avoiding dataset materialization at 5,000 rows/second throughput. Domain sinks declare only column mappings via an abstract `Members` property. The `ConsoleLoggingPipelineProgress` wrapper emits throughput metrics every 10,000 rows using a `Lazy<Stopwatch>`. Pipeline processors are tested in isolation with BDD-style `When_X` nested test classes and mocked `ICachingLookup<T>` dependencies.

---

## Evidence: Sink — Streaming SqlBulkCopy

The `BulkCopyPipelineSinkBase<TModel, TOptions>` uses FastMember's `ObjectReader` to bridge `IAsyncEnumerable<TModel>` into an `IDataReader` that SqlBulkCopy can consume directly. This avoids materializing the entire dataset — SqlBulkCopy pulls rows through the reader on demand while the upstream pipeline continues processing:

```csharp
public abstract class BulkCopyPipelineSinkBase<TModel, TOptions>
{
    public async Task ConsumeData(IAsyncEnumerable<TModel> source, CancellationToken token)
    {
        await using var reader = ObjectReader.Create(
            source.ToEnumerable(), 
            Members.Select(m => m.Item1).ToArray()
        );

        using var bcp = new SqlBulkCopy(connection, SqlBulkCopyOptions.KeepNulls, null);
        bcp.EnableStreaming = true;
        bcp.BatchSize = _batchSize;
        await bcp.WriteToServerAsync(reader, token);
    }

    protected abstract (string, string)[] Members { get; }
}
```

Each domain-specific sink only needs to declare its column mappings via the abstract `Members` property — a tuple array of `(sourceProperty, destinationColumn)`. The batch size and destination table come from strongly-typed options. Deadlock detection uses specific `SqlException.Number == 1205` rather than catching all SQL exceptions.

The framework also includes `NullPipelineSink<T>` (drains the pipeline to completion without writing, useful for dry-run testing) and `ConsoleLoggingPipelineSink<T>` (dumps rows to stdout for debugging).

---

## Evidence: Observability — Throughput Monitoring

The `ConsoleLoggingPipelineProgress<TModel>` wraps the pipeline stream and emits throughput metrics every 10,000 rows:

```csharp
public async IAsyncEnumerable<TModel> Capture(IAsyncEnumerable<TModel> input, ...)
{
    var itemCount = 0;
    await foreach (var item in input.WithCancellation(token))
    {
        if (itemCount++ % 10000 == 0)
            UpdateProgress(itemCount);
        yield return item;
    }
    UpdateProgress(itemCount, LogLevel.Information);
}
```

It uses a `Lazy<Stopwatch>` to defer timing until the first row flows through, and computes records/second for throughput reporting. The `NullPipelineProgress<T>` variant passes through without any overhead. Both are swappable via the `PipelineProgressProviders` configuration enum, resolved at DI registration time.

---

## Evidence: Testing Pattern

Pipeline processors are tested in isolation by feeding `RawRow` instances through `PrepareData` and asserting on the output DTOs. The `ConvosoPipelineProcessor_Tests` demonstrate this pattern:

```csharp
public class When_importing_good_data : ConvosoPipelineProcessor_Tests
{
    public When_importing_good_data()
    {
        Input = RawRow.FromDynamic(new Dictionary<string, object> { ... });
        Output = Subject.PrepareData(
            new[] { Input }.ToAsyncEnumerable(),
            CancellationToken.None
        ).FirstOrDefaultAsync().GetAwaiter().GetResult();
    }

    [Fact]
    public void It_should_set_status_id() => Output.StatusId.ShouldBe((byte)30);
}
```

Dependencies like `ICachingLookup<T>` are mocked to return known values, and the test verifies that the pipeline correctly transforms raw input to typed output. The BDD nested-class pattern (`When_importing_good_data` inheriting from the base test class) keeps test setup readable and scoped.

### Scale

This framework processes data imports across four data source domains in a production direct mail platform handling 30 million recipients across 10-15 million unique addresses. The DirectMail pipeline alone processes 50,000-row imports in under 10 seconds (5,000 rows/second sustained) including all transformation, hashing, and bulk insert operations.

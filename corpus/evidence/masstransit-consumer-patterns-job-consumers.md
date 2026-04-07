---
title: MassTransit Consumer Patterns — IJobConsumer for Long-Running Work
tags: [masstransit, consumers, csharp, message-driven, job-consumer, long-running, open-generic, checkpointing, di-scope, concurrency]
related:
  - evidence/masstransit-consumer-patterns.md
  - evidence/masstransit-consumer-patterns-crud-respondif.md
  - evidence/masstransit-consumer-patterns-consumer-definitions.md
  - evidence/masstransit-consumer-patterns-advanced.md
  - evidence/masstransit-contract-design.md
  - evidence/dependency-injection-composition.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/masstransit-consumer-patterns.md
---

# MassTransit Consumer Patterns — IJobConsumer for Long-Running Work

Three consumer families in the Madera platform use MassTransit's `IJobConsumer<T>` interface (rather than `IConsumer<T>`) for operations that exceed normal message timeout windows. This document covers those patterns including scoped DI injection, open-generic pipeline resolution, and checkpointed progress.

---

## Evidence: Import Pipeline Coordinators

The Convoso and Dispo import consumers use `IJobConsumer` to wrap their respective `ImportPipelineCoordinator<T>` executions. Each creates an `AsyncScope` from `IServiceProvider` to get a fresh DI scope with runtime-configurable pipeline data:

```csharp
// Madera/Madera.Dataflows.Convoso/Consumers/BeginConvosoImportConsumer.cs
public async Task Run(JobContext<ConvosoDataReceivedMessage> context)
{
    await HandleImport(filePath);
    await context.Publish<ConvosoImportCompleteMessage>(context.Job, context.CancellationToken);
}

private async Task HandleImport(string filePath)
{
    await using var scope = _serviceProvider.CreateAsyncScope();
    var settings = scope.ServiceProvider.GetRequiredService<ConvosoRuntimeData>();
    settings.ImportSource = new FileStream(filePath, FileMode.Open);
    var pipeline = scope.ServiceProvider.GetRequiredService<ImportPipelineCoordinator<ConvosoData>>();
    await pipeline.RunAsync(CancellationToken.None);
}
```

The consumer definitions configure `JobOptions` with `ConcurrentJobLimit = 1` and `JobTimeout = TimeSpan.FromMinutes(30)`, serializing imports to prevent resource contention.

---

## Evidence: Open-Generic Pipeline Resolution (BeginDispoImportConsumer)

The Dispo import consumer adds a layer of complexity: it resolves the correct `ImportPipelineCoordinator<T>` at runtime using open generic type resolution based on the buyer name from the message:

```csharp
// Madera/Madera.Dataflows.Dispos/Consumers/BeginDispoImportConsumer.cs
var dispo = _baseDispos.First(
    d => string.Equals(d.BuyerName, buyerName, StringComparison.OrdinalIgnoreCase)
);
var coordinatorType = typeof(ImportPipelineCoordinator<>).MakeGenericType(dispo.GetType());
var coordinator = scope.ServiceProvider.GetRequiredService(coordinatorType);
await ((ImportPipelineCoordinator) coordinator).RunAsync(CancellationToken.None);
```

The consumer receives `IEnumerable<BaseDispoData>` via DI — each registered `BaseDispoData` subclass maps to a distinct buyer's data format. The consumer finds the matching subclass by buyer name, constructs the closed generic type, and resolves it from the container. This allows new buyer formats to be added by registering a new `BaseDispoData` subclass and its corresponding pipeline stages without modifying the consumer.

---

## Evidence: Checkpointed Report Generation (BuildImportReportsConsumer)

The `BuildImportReportsConsumer` uses `IJobConsumer<ImportCompletedEvent>` with MassTransit's job state persistence to make report generation resumable:

```csharp
// Madera/Madera.Dataflows.DirectMail/Consumers/Reporting/BuildImportReportsConsumer.cs
public async Task Run(JobContext<ImportCompletedEvent> context)
{
    var availableReports = await _service.GetAvailableReports(token);
    var completedProcedures = GetJobState(context, availableReports);

    foreach (var report in availableReports)
    {
        await context.SetJobProgress(completedProcedures.Count, availableReports.Count);
        if (completedProcedures.Contains(report.ProcedureName))
            continue;

        await RunReport(context.Job, report, token);
        completedProcedures.Add(report.ProcedureName);
        await context.SaveJobState(completedProcedures);
    }
}
```

Each completed stored procedure name is saved to the job state via `SaveJobState`. If the consumer crashes or is restarted, `TryGetJobState` recovers the `HashSet<string>` of completed procedures and skips them. Combined with `SetJobProgress`, this provides both crash recovery and progress visibility through MassTransit's job monitoring infrastructure.

---

## Key Files

- `madera-apps:Madera/Madera.Dataflows.Convoso/Consumers/BeginConvosoImportConsumer.cs` — job consumer with scoped DI (64 lines)
- `madera-apps:Madera/Madera.Dataflows.Dispos/Consumers/BeginDispoImportConsumer.cs` — open-generic pipeline resolution (83 lines)
- `madera-apps:Madera/Madera.Dataflows.DirectMail/Consumers/Reporting/BuildImportReportsConsumer.cs` — checkpointed job consumer (113 lines)

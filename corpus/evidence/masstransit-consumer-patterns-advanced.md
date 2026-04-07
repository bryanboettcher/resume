---
title: MassTransit Consumer Patterns — Advanced Patterns (Routing Slip, Batch, Outbox Bypass, Self-Scheduling, Cross-Context)
tags: [masstransit, consumers, csharp, message-driven, routing-slip, batch-consumer, outbox, self-scheduling, cross-bounded-context, fault-forward, throughput]
related:
  - evidence/masstransit-consumer-patterns.md
  - evidence/masstransit-consumer-patterns-crud-respondif.md
  - evidence/masstransit-consumer-patterns-consumer-definitions.md
  - evidence/masstransit-consumer-patterns-job-consumers.md
  - evidence/masstransit-contract-design.md
  - evidence/distributed-systems-architecture.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/masstransit-consumer-patterns.md
---

# MassTransit Consumer Patterns — Advanced Patterns

This document covers the more specialized consumer patterns in Madera and kb-platform: routing slip construction, batch consumers for throughput, outbox bypass for high-volume publishing, self-scheduling pollers, cross-bounded-context event bridges, and fault-forward pipeline design.

---

## Evidence: Routing Slip Consumer (RingbaStartDownloadConsumer)

The Ringba data download pipeline uses MassTransit's routing slip pattern — a consumer builds a sequential activity chain and dispatches it:

```csharp
// Madera/Madera.Dataflows.Ringba/Consumers/RingbaStartDownloadConsumer.cs
public Task Consume(ConsumeContext<RingbaStartDownloadMessage> context)
{
    var builder = new RoutingSlipBuilder(NewId.NextGuid());
    var message = context.Message;

    if (message.StartTime is not null)
    {
        builder.AddVariable(nameof(GetReportIdArguments.StartTime), message.StartTime);
        builder.AddVariable(nameof(GetReportIdArguments.EndTime), message.EndTime);
    }
    else
    {
        builder.AddActivity("GetDownloadRangeActivity",
            new Uri($"queue:{_formatter.ExecuteActivity<DownloadRangeActivity, DownloadRangeArguments>()}"));
    }

    builder.AddActivity("GetReportIdActivity", ...);
    builder.AddActivity("GetDownloadUriActivity", ...);
    builder.AddActivity("DownloadApiDataActivity", ...);
    builder.AddActivity("UnzipApiDataActivity", ...);

    return context.Execute(builder.Build());
}
```

The consumer conditionally includes a `DownloadRangeActivity` step — if the caller provides explicit start/end times, those are set as routing slip variables and the range calculation step is skipped. Using `IEndpointNameFormatter` to resolve activity queue URIs means the routing slip adapts to whatever endpoint naming convention the bus is configured with.

---

## Evidence: Batch Consumers for Throughput

Two consumers use MassTransit's `Batch<T>` message wrapper to aggregate individual messages into batches before processing.

The `NormalizeAddressConsumer` receives `Batch<NormalizeAddressCommand>`, extracts all addresses from the batch, calls the address normalization API in a single round trip, then publishes a `StageAddressesCommand` with the results.

The `StageAddressesConsumer` receives `Batch<StageAddressesCommand>`, deduplicates addresses across all messages in the batch by `RecipientId`, then runs the bulk import and downstream publish operations concurrently:

```csharp
// Madera/Madera.Dataflows.DirectMail/Consumers/Importing/StageAddressesConsumer.cs
var addresses = message
    .SelectMany(m => m.Message.Addresses)
    .DistinctBy(m => m.RecipientId)
    .ToList();

var importTask = _importService.BulkImportAddresses(addresses, CancellationToken.None);
var publishTasks = addresses.Chunk(20).Select(b => Publish(context, b, token));
return Task.WhenAll(publishTasks.Concat([ importTask ]));
```

The import and downstream publish happen in parallel via `Task.WhenAll`, with publish further chunked into groups of 20 to avoid overwhelming the message broker.

---

## Evidence: Outbox Bypass for High-Volume Publishing

The `AddressExportConsumer` demonstrates deliberate outbox bypass for bulk message publishing. When exporting thousands of addresses for normalization, it calls `InternalOutboxExtensions.SkipOutbox(context)` to publish directly to the transport rather than through MassTransit's transactional outbox:

```csharp
// Madera/Madera.Dataflows.DirectMail/Domains/Imports/Consumers/AddressExportConsumer.cs
var directContext = InternalOutboxExtensions.SkipOutbox(context);
foreach (var address in addresses)
{
    await directContext.PublishBatch(address, context.CancellationToken);
}
```

The addresses are chunked into batches of 200 via LINQ's `.Chunk(200)` before publishing. The outbox would normally buffer all published messages until the consumer completes, then dispatch them transactionally. For a consumer that publishes tens of thousands of individual messages, this would accumulate an unmanageable outbox payload. Bypassing it trades exactly-once delivery guarantees for practical throughput — acceptable because the downstream `NormalizeAddressConsumer` is idempotent (re-normalizing the same address produces the same result).

---

## Evidence: Self-Scheduling Consumer (ConvosoFtpDownloadJobConsumer)

The Convoso FTP download consumer implements a polling pattern through self-scheduling: if the expected file doesn't exist on the FTP server, the consumer schedules a future re-delivery of its own message:

```csharp
// Madera/Madera.Dataflows.Convoso/Downloading/ConvosoFtpDownloadJobConsumer.cs
if (tempPath is not null)
{
    await context.Publish<ConvosoDataReceivedMessage>(...);
}
else
{
    var future = DateTime.UtcNow.AddMinutes(15);
    var checkCount = (context.Job.CheckCount ?? 0) + 1;
    await _scheduler.SchedulePublish<DownloadConvosoFtpMessage>(future, new
    {
        TargetDate = targetDate,
        CheckCount = checkCount
    });
}
```

The `CheckCount` field on the message tracks how many attempts have been made. The consumer uses `IMessageScheduler` (MassTransit's scheduling abstraction) to publish a future copy of its own message type, creating a retry loop without holding a thread or connection open.

---

## Evidence: Cross-Bounded-Context Consumer (ProductNameUpdatedConsumer)

In the kb-platform repository, the `ProductNameUpdatedConsumer` bridges the Catalog and Storefront bounded contexts. When a product name changes in Catalog, this consumer translates the event into a Storefront command:

```csharp
// kb-platform: src/services/KbStore.ApiService/Consumers/Catalog/ProductNameUpdatedConsumer.cs
public async Task Consume(ConsumeContext<ProductNameUpdated> context)
{
    var response = await _requestClient.GetResponse<UpdateSellableItemResponse>(
        new
        {
            SellableItemId = msg.ProductId,
            Name = msg.Name ?? msg.Sku,
            Timestamp = DateTimeOffset.UtcNow
        },
        context.CancellationToken);
}
```

The fault handling uses pattern matching on `RequestFaultException` message content to distinguish between "not found" (a timing issue — the sellable item may not be created yet, so the consumer logs a warning and completes without throwing) and "discontinued" (a domain rule — discontinued items cannot be renamed, also a warning), versus unexpected faults (rethrown to trigger MassTransit's retry/fault pipeline). This graduated error handling avoids treating expected domain conditions as infrastructure failures.

---

## Evidence: FileImportConsumer — Fault-Forward Design

The `FileImportConsumer` processes file imports by delegating to a generic `ImportPipelineCoordinator<DirectMailData>`. A deliberate design comment explains the fault handling approach:

```csharp
// Madera/Madera.Dataflows.DirectMail/Domains/Imports/Consumers/FileImportConsumer.cs

// we don't try/catch anymore so the process can throw-and-fault directly. this is now
// captured as `Fault<FileImportRequest>` in the state machine, which will include a lot
// more exception information than previously returned
await coordinator.RunAsync(CancellationToken.None);
```

Rather than catching exceptions and returning error response objects, the consumer lets pipeline failures propagate as unhandled exceptions. MassTransit captures these as `Fault<FileImportRequest>` messages which the import state machine handles — providing richer exception details (full stack traces, inner exceptions) than a hand-rolled error response would. This is a conscious choice to use MassTransit's fault pipeline as the error channel rather than building a parallel error reporting mechanism.

---

## Key Files

- `madera-apps:Madera/Madera.Dataflows.DirectMail/Domains/Imports/Consumers/FileImportConsumer.cs` — fault-forward pipeline consumer (73 lines)
- `madera-apps:Madera/Madera.Dataflows.DirectMail/Domains/Imports/Consumers/AddressExportConsumer.cs` — outbox bypass (89 lines)
- `madera-apps:Madera/Madera.Dataflows.DirectMail/Consumers/Importing/StageAddressesConsumer.cs` — deduplicating batch consumer (85 lines)
- `madera-apps:Madera/Madera.Dataflows.Convoso/Downloading/ConvosoFtpDownloadJobConsumer.cs` — self-scheduling FTP poller (133 lines)
- `madera-apps:Madera/Madera.Dataflows.Ringba/Consumers/RingbaStartDownloadConsumer.cs` — routing slip builder
- `madera-apps:Madera/Madera.Dataflows.DirectMail/Consumers/MailFiles/ShakeMailFileConsumer.cs` — saga-driven mail file population (54 lines)
- `kb-platform:src/services/KbStore.ApiService/Consumers/Catalog/ProductNameUpdatedConsumer.cs` — cross-context event bridge (83 lines)

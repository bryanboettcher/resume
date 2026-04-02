---
title: MassTransit Contract Design — Import Workflow Saga Contracts
tags: [masstransit, contract-design, message-driven, csharp, saga, saga-orchestration, state-machine, large-message, import-workflow, restart-commands]
related:
  - evidence/masstransit-contract-design.md
  - evidence/masstransit-contract-design-project-structure.md
  - evidence/masstransit-contract-design-interface-inheritance.md
  - evidence/masstransit-contract-design-mail-file-lifecycle.md
  - evidence/distributed-systems-architecture.md
  - evidence/etl-pipeline-framework.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/masstransit-contract-design.md
---

# MassTransit Contract Design — Import Workflow Saga Contracts

In the Madera direct mail platform, Bryan designed the import workflow contracts so the saga lifecycle is fully readable from the message interfaces alone — no implementation code required. The `ImportWorkflowMessages.cs` file defines the complete state machine structure through interface composition: what state the saga tracks, what events drive transitions, how external triggers differ from saga-internal messages, and how operators can restart mid-workflow without re-uploading data.

---

## Evidence: Import Workflow Contracts

**File:** `Madera/Madera.Contracts/Messages/DirectMail/Importing/ImportWorkflowMessages.cs` (144 lines)

The file is organized into seven regions, each corresponding to a phase of the import process.

### Base Model Defines Saga State Shape

```csharp
public interface ImportModelBase : CorrelatedBy<Guid>
{
    int CurrentState { get; }
    PublisherModelBase Publisher { get; }
    VerticalModelBase Vertical { get; }
    BrokerModelBase Broker { get; }
    decimal? CostPerLead { get; }
    float? MaxScrubPercentage { get; }
    byte PaymentStatus { get; }
    byte SourceFileType { get; }
    int ImportedRowCount { get; }
    int AddressPendingCount { get; }
    DateTimeOffset CreatedOn { get; }
    DateTimeOffset? ImportInitiatedOn { get; }
    DateTimeOffset? ImportCompletedOn { get; }
    DateTimeOffset? AddressNormalizationInitiatedOn { get; }
    DateTimeOffset? AddressNormalizationCompletedOn { get; }
    DateTimeOffset? CompletedOn { get; }
    DateTimeOffset LastUpdatedOn { get; }
    string? Filename { get; }
    string? LastStatus { get; }
    string? ExceptionMessage { get; }
    string? TransformScript { get; }
    float? MinimumAge { get; }
    float? MaximumAge { get; }
}
```

Every nullable `DateTimeOffset` represents a workflow stage boundary. The saga state class (`DirectMailImport : SagaStateMachineInstance` in `Madera/Madera.Dataflows.DirectMail/Sagas/DirectMailImport.cs`) mirrors this interface exactly, with XML documentation explaining what each timestamp means in the workflow.

### Workflow Events as a Flat Transition Map

The transition events are defined as a flat set of interfaces all extending `ImportUpdatedEvent`:

```csharp
public interface ImportStagingStartedEvent : ImportUpdatedEvent;
public interface ImportStagingCompletedEvent : ImportUpdatedEvent;
public interface ImportNormalizationReadyEvent : ImportUpdatedEvent;
public interface ImportNormalizationStartedEvent : ImportUpdatedEvent;
public interface ImportNormalizationCompletedEvent : ImportUpdatedEvent;
public interface ImportMigrationStartedEvent : ImportUpdatedEvent;
public interface ImportMigrationCompletedEvent : ImportUpdatedEvent;
public interface ImportCompletedEvent : ImportUpdatedEvent;
```

Reading these eight interfaces sequentially describes the complete workflow: staging starts, staging completes, normalization becomes ready, normalization starts, normalization completes, migration starts, migration completes, import completes. Each event inherits the full `ImportModelBase` state through `ImportUpdatedEvent : ImportModelBase`, meaning every event carries the complete saga state at the time of the transition. Consumers receiving any of these events have access to all timestamps and counters without querying back to the saga.

### Request/Response Pairs Separate Endpoint-Facing from Saga-Internal

```csharp
// External trigger — arrives from upload endpoint
public interface UploadImportCommand : CorrelatedBy<Guid>
{
    MessageData<Stream> FileContents { get; }
}
public interface UploadImportResponse : ImportModelBase;
public interface UploadImportFailure : CorrelatedBy<Guid>, FailureEventBase;

// Saga-internal — the state machine dispatches these to consumers
public interface FileImportRequest : CorrelatedBy<Guid>
{
    MessageData<Stream> FileContents { get; }
    SourceFileType SourceFileType { get; }
    string TransformScript { get; }
    float? MinimumAge { get; }
    float? MaximumAge { get; }
}
public interface FileImportResponse : CorrelatedBy<Guid>;
```

`UploadImportCommand` is what the HTTP endpoint publishes. `FileImportRequest` is what the saga publishes after enriching the command with its own state — adding `SourceFileType`, `TransformScript`, and age range filters stored in the saga. The API layer only needs to know about the upload contract. The saga handles the enrichment before dispatching to the actual file import consumer.

`MessageData<Stream>` is MassTransit's large message payload support — file contents are stored out-of-band in a message data repository rather than serialized inline with the message. This is essential when imports can be tens of megabytes.

### Restart Commands for Mid-Workflow Recovery

```csharp
public interface AddressNormalizationRestartCommand : CorrelatedBy<Guid>;
public interface RecipientMigrationRestartCommand : CorrelatedBy<Guid>;
public interface RebuildImportReportCommand : CorrelatedBy<Guid>;
```

These are deliberately minimal — just a correlation ID. They exist so operators can re-trigger a specific workflow stage without re-uploading data or re-running earlier stages. Restart commands are part of the contract, not an afterthought bolted onto the implementation. The fact that they're defined in the same contracts file as the workflow events means they're a first-class part of the workflow design.

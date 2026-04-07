---
title: Direct Mail Domain System — Mail File Lifecycle and Composable Filter System
tags: [direct-mail, masstransit, saga-orchestration, composable-filters, polymorphic-serialization, angular, domain-modeling, json-derived-type]
related:
  - projects/direct-mail-platform.md
  - projects/direct-mail-platform-import-pipeline.md
  - projects/call-trader-madera.md
  - evidence/masstransit-contract-design.md
  - evidence/domain-driven-modeling.md
  - evidence/aspnet-minimal-api-patterns.md
category: project
contact: resume@bryanboettcher.com
parent: projects/direct-mail-platform.md
---

# Direct Mail Domain System — Mail File Lifecycle and Composable Filter System

After recipients are imported and addresses normalized, users assemble physical mail files through a composable filter system managed by the `MailFileStateMachine`.

---

## The MailFileStateMachine

The `MailFileStateMachine` manages the mail file lifecycle through a bread-baking metaphor with five states:

**Kneading** — The initial configuration phase. Users set the mail date, publisher, broker, vertical, mail house, creative, and configure recipient selection filters through groupings. The saga creates a default grouping on initialization. The Kneading state supports an `EstimateRecipientCount` request — the saga dispatches a `CountFilteredRecipientsRequest` to the grouping service, which executes the filter chain against the database without actually populating the mail file. This lets users preview how many recipients their filter configuration would select before committing.

**Shaking** (PopulateMailFile.Pending) — When the user triggers population, the `ShakeMailFileConsumer` removes any existing recipients for the file, then runs the grouping filter chain via `IDirectMailGroupingService.GetRecipients()` and writes the results via `IDirectMailFileService.PopulateMailFile()`. The response carries the total recipient count back to the saga.

**Baking** (PopulateMailOutput.Pending) — After reviewing the populated recipients, the user "bakes" the file. This generates the final output — the formatted file that gets sent to the mail house for printing.

**Complete** — The mail file is finalized and delivered.

The state machine supports bidirectional transitions: Shaking can return to Kneading (via `ResetRecipientRequest` / `ClearMailFileRequest`), and Baking can return to Shaking (via `ResetMailOutputRequest` / `ClearMailOutputRequest`). Users can adjust filters, re-populate, re-bake at any point before final completion.

**Concurrency control**: The `GroupingsVersion` field is a random GUID nonce regenerated every time groupings are updated. The `UpdateSagaGrouping` method compares the message's version against the saga's stored version and throws `InvalidOperationException` on mismatch, implementing optimistic concurrency for the filter configuration without database-level locking.

**Duplicate detection**: `e.CorrelateBy((s, c) => s.Filename == c.Message.Filename)`. Attempting to create a mail file with an existing filename returns a `CreateMailFileFailure` with a conflict message, handled by the `DuringAny` block.

---

## Composable Filter System

The mail file's recipient selection is built on a polymorphic filter hierarchy. `GroupingFilter` is an abstract base class using `System.Text.Json` polymorphic serialization with `[JsonDerivedType]` discriminators for 11 concrete filter types:

- **ImportBatchFilter** — restrict to recipients from specific import batches
- **IncludedStateFilter** — filter by U.S. state
- **IncludedVerticalsFilter** — filter by market vertical
- **OriginalPublishersFilter** — filter by the publisher that originally supplied the lead
- **MultiPublishersFilter** — filter by publisher across multiple verticals
- **TimesMailedFilter** — restrict by how many times a recipient has been mailed
- **DaysSinceMailingFilter** — exclude recipients mailed within N days
- **DateOfBirthFilter** — age range filtering
- **UnscrubbedLeadsFilter** — include only leads that haven't been scrubbed
- **ExternalDuplicatesFilter** — exclude duplicates found in external databases
- **ZipListFilter** — restrict to specific ZIP codes

Filters are grouped into `MailGrouping` objects — a mail file can have multiple groupings, each with its own set of filters. This enables complex selection logic: "Group A: Medicare vertical, ages 65+, never mailed" alongside "Group B: Auto insurance, mailed more than 30 days ago, in these ZIP codes."

Each filter type has a corresponding server-side processor extending `BaseFilterProcessor<TFilter>`. For example, `DaysSinceMailingFilterProcessor` computes a cutoff date from the current time, then executes `QueryUnbufferedAsync<int>` against SQL Server to stream matching recipient IDs. The unbuffered streaming is deliberate — some filter queries can return millions of qualifying IDs.

The `IDirectMailGroupingService` orchestrates filter execution: it collects recipient IDs from each filter processor within each grouping, intersects them (all filters within a grouping are AND logic), and unions across groupings (groupings are OR logic).

---

## Angular Mail File UI

The Angular frontend (`dm-files-detail.component.ts`) manages the grouping/filter configuration for a mail file. It supports adding/removing groupings, adding/removing/updating filters within groupings, estimating recipient counts, and triggering population. It uses immutable update patterns (`this.data = { ...this.data, groupings: newGroupings }`) for Angular change detection. The `filterDataMap` provides metadata about available filter types for dynamic form rendering.

The frontend communicates with the backend through a service layer that wraps HTTP calls to approximately 50 distinct endpoints under `/api/direct-mail/`.

---

## Key Files

- `madera-apps:Madera/Madera.Dataflows.DirectMail/Sagas/MailFileStateMachine.cs` — 5-state mail file lifecycle with bidirectional transitions
- `madera-apps:Madera/Madera.Contracts/Messages/DirectMail/MailFiles/Groupings/GroupingFilter.cs` — Polymorphic filter base with 11 subtypes and JSON discriminators
- `madera-apps:Madera/Madera.Contracts/Messages/DirectMail/MailFiles/Groupings/MailGrouping.cs` — Self-validating aggregate with AND-within-group, OR-across-groups logic
- `madera-apps:Madera/Madera.Dataflows.DirectMail/Services/Groupings/` — FilterProcessor implementations per filter type
- `madera-apps:Madera/madera.ui.client/src/app/dm-files-detail.component.ts` — Angular filter configuration UI

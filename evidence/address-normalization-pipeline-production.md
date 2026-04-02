---
title: Address Normalization Pipeline — Production (madera-apps)
tags: [address-normalization, usps, lob-api, crc64, deduplication, bitflags, direct-mail, pipeline-architecture, sql-server, masstransit, saga, csharp]
related:
  - evidence/address-normalization-pipeline.md
  - evidence/address-normalization-pipeline-fastaddress.md
  - evidence/data-engineering-etl.md
  - evidence/sql-server-database-engineering.md
  - evidence/masstransit-consumer-patterns.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/address-normalization-pipeline.md
---

# Address Normalization Pipeline — Production (madera-apps)

Address data is the core entity in the Call-Trader/Madera direct mail platform. Every import of 50K-500K recipient records must normalize raw user-submitted addresses through USPS verification, deduplicate against a corpus of 10-15M existing addresses, classify deliverability, and store results for downstream mailing workflows.

---

## Evidence: Lob API Integration with Bulk Verification

The `LobAddressNormalizer` class in `Madera/Madera.AddressNormalization/Components/LobAddressNormalizer.cs` wraps the Lob USPS verification API. It accepts arrays of `AddressMetadata` and sends them to Lob's bulk verification endpoint (`/v1/bulk/us_verifications`) in batches. The implementation uses `IHttpClientFactory` for connection management, `System.Text.Json` with `SnakeCaseLower` naming policy for Lob's API contract, and streams the request payload through a `MemoryStream` buffer.

Error handling maps Lob HTTP status codes to typed exceptions (`UnauthorizedException`, `ForbiddenException`, `TooManyRequestsException`, etc.) via a switch expression, giving upstream consumers specific exception types to handle rather than generic HTTP failures.

---

## Evidence: Bitflag Address Metadata (17 Flags)

The `AddressFlags` enum in `Madera/Madera.Common/Locations/AddressMetadata.cs` encodes 17 distinct address characteristics into a single `int` field using bit shifts:

```
Bits 0-3:  ZIP code type (standard, PO box, unique, military)
Bits 4-5:  Address type (residential, commercial)
Bits 6-7:  Validity (valid, deliverable)
Bits 8-12: Deliverability detail (deliverable, remove secondary, incorrect secondary,
           missing secondary, USPS undeliverable)
Bits 13-16: Special conditions (vacant, informed delivery, phantom, general delivery)
```

This design stores what would otherwise be 17 boolean columns in a single 4-byte integer. The `BuildAddressFlags` method in `LobAddressNormalizer` maps Lob's string responses (e.g., `"deliverable_unnecessary_unit"`, `"residential"`) to the appropriate flag bits.

The `AddressMetadataExtensions` class provides `IsValid()` which checks both structural validity and flag validity. The `GetHash()` method produces a CRC64 over the pipe-delimited concatenation of all address fields, uppercased, for deduplication lookups.

---

## Evidence: CRC64 Address Hashing and Deduplication

Address deduplication across the 10-15M corpus uses CRC64 hashing rather than string comparison. The `GetHash()` extension method concatenates address fields with pipe delimiters, uppercases the result, and calls `.AsCrc64()` to produce a `long`. This hash is stored as `AddressHash` on every address record and indexed for O(1) lookups.

The SQL table-valued parameter type `dbo.NormalizedAddressType` includes an `AddressHash BIGINT` column with a nonclustered index (`INDEX IX_AddressHash NONCLUSTERED (AddressHash)`), enabling bulk hash-based lookups during import batches.

The stored procedure `dbo.usp_GetAddressId` implements an upsert pattern: it first checks whether an address with the given hash exists, returning the existing ID if found. Only when no match exists does it insert a new row and return `SCOPE_IDENTITY()`. The return also includes a `Created` flag so the caller knows whether the address was new or pre-existing.

---

## Evidence: MassTransit Address Saga State Machine

Address lifecycle is managed by `AddressStateMachine` in `Madera/Madera.Dataflows.DirectMail/Domains/Addresses/AddressStateMachine.cs`, a MassTransit `MassTransitStateMachine<DirectMailAddress>` with two states: `Unverified` and `Verified`.

The state machine handles several scenarios:

- **New address via import (`OnEnsure`)**: Correlates by `AddressHash` (not by GUID), sets `InsertOnInitial = true`, transitions to `Unverified`, and publishes `NormalizeAddressCommand` to trigger Lob verification.
- **Normalization complete (`OnNormalized`)**: Updates flags and GPS, transitions to `Verified`, publishes `AddressVerifiedEvent`.
- **Duplicate detection**: When `OnEnsure` fires for an address already in `Unverified` state with a different `CorrelationId`, the machine publishes an `AddressMergedEvent` to consolidate the duplicate.
- **Already verified**: When `OnEnsure` fires for an address in `Verified` state, it immediately publishes `AddressVerifiedEvent` without re-verifying — serving as a cache hit.

---

## Evidence: Batch Staging with MassTransit Batching

The `StageAddressesConsumer` consumes `Batch<StageAddressesCommand>`, accumulating up to 50 messages with a 6-second time limit before processing. It deduplicates by `RecipientId` across the batch, then fans out two concurrent operations:

1. `BulkImportAddresses` — writes addresses to the database via the import service
2. Chunks addresses into groups of 20 and publishes `UpdateRecipientAddressesCommand` for each chunk

The consumer is configured with `ConcurrencyLimit = 1` and `MessageRetry` of 8 attempts at 1-minute intervals, ensuring ordered processing with resilience.

---

## Evidence: SQL Table-Valued Parameter for Bulk Operations

The C# `NormalizedAddressType` class bridges the gap between the C# domain model and SQL Server's table-valued parameters. It maps `AddressMetadata` to a `SqlDataRecord` with explicit column definitions and handles nullable fields via a local `AsDbNull` helper. The `FromAddressMetadata` factory method hardcodes invalid addresses to `Guid.Empty` while leaving valid addresses with `null` AddressId — signaling to the stored procedure that the address needs to be looked up.

---

## Key Files

- `madera-apps:Madera/Madera.AddressNormalization/Components/LobAddressNormalizer.cs` — Lob bulk verification client with typed error handling
- `madera-apps:Madera/Madera.Common/Locations/AddressMetadata.cs` — AddressFlags 17-bit enum, CRC64 hashing, validity checks
- `madera-apps:Madera/Madera.Dataflows.DirectMail/Domains/Addresses/AddressStateMachine.cs` — MassTransit saga: Unverified → Verified with merge handling
- `madera-apps:Madera/Madera.Dataflows.DirectMail/Consumers/Importing/StageAddressesConsumer.cs` — Batched address staging with MassTransit Batch consumer
- `madera-apps:Madera/Madera.Dataflows.DirectMail/Services/CustomTypes/NormalizedAddressType.cs` — SqlDataRecord TVP bridge for bulk SQL operations
- `madera-apps:Madera/Madera.DbMigrations/Migrations/DirectMail/20241227/05_tvp_NormalizedAddressType.sql` — SQL TVP with indexed AddressHash
- `madera-apps:Madera/Madera.Workflows/Migrations/DirectMail/20241227/06_usp_GetAddressId.sql` — Hash-based upsert stored procedure

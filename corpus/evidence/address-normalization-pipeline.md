---
title: Address Normalization & Matching Pipeline
tags: [address-normalization, usps, lob-api, crc64, deduplication, bitflags, direct-mail, cosine-similarity, pipeline-architecture, sql-server, masstransit, saga, csharp]
related:
  - evidence/data-engineering-etl.md
  - evidence/etl-pipeline-framework.md
  - evidence/sql-server-database-engineering.md
  - evidence/distributed-systems-architecture.md
  - projects/call-trader-madera.md
  - projects/fastaddress-research.md
children:
  - evidence/address-normalization-pipeline-production.md
  - evidence/address-normalization-pipeline-fastaddress.md
category: evidence
contact: resume@bryanboettcher.com
---

# Address Normalization & Matching Pipeline — Index

Address data is the core entity in the Call-Trader/Madera direct mail platform. Every import of 50K-500K recipient records must normalize raw user-submitted addresses through USPS verification, deduplicate against a corpus of 10-15M existing addresses, classify deliverability, and store results for downstream mailing workflows. Bryan built the production address pipeline in madera-apps and then extracted the matching problem into FastAddress, a standalone research project exploring semantic address comparison beyond exact-match hashing.

The two codebases represent different phases of the same problem: madera-apps handles the production reality of normalizing millions of addresses via external API with caching and batch processing, while FastAddress explores how to determine whether two differently-formatted addresses refer to the same physical location without calling an external service.

The full evidence is split into focused documents:

## Child Documents

- **[Production Pipeline (madera-apps)](address-normalization-pipeline-production.md)** — Lob API bulk verification with typed error handling, 17-flag bitfield for address metadata, CRC64 hashing for deduplication across 10-15M records, the `AddressStateMachine` MassTransit saga (Unverified → Verified with merge events and cache-hit behavior), batched staging via `Batch<T>` consumers, and SQL table-valued parameters for bulk upsert.

- **[FastAddress Research](address-normalization-pipeline-fastaddress.md)** — Local matching without external API calls. Four-stage pipeline (tokenize, normalize, encode, compare). 170+ USPS standard abbreviations with bidirectional equivalence. Two-layer matching: exact numeric token gate (house numbers must match exactly) + cosine similarity over semantic embeddings. `ReadOnlySpan<char>` inputs, source-generated regex, statistical validation against 27M production records.

---
title: Address Normalization Pipeline — FastAddress Research (Local Matching)
tags: [address-normalization, cosine-similarity, usps, deduplication, pipeline-architecture, csharp, readonly-struct, span, source-generated-regex, semantic-similarity]
related:
  - evidence/address-normalization-pipeline.md
  - evidence/address-normalization-pipeline-production.md
  - projects/fastaddress-research.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/address-normalization-pipeline.md
---

# Address Normalization Pipeline — FastAddress Research

FastAddress is a standalone research project exploring address matching without calling an external service. The production pipeline (madera-apps) determines address identity via Lob API + CRC64 hashing. FastAddress asks the next question: can two differently-formatted addresses be matched locally, without a round trip?

---

## Evidence: Pipeline Architecture

FastAddress defines a four-stage pipeline through interfaces in `FastAddress.Components/Interfaces/`:

- `IAddressTokenizer` — splits raw address text into structured tokens
- `IAddressNormalizer` — normalizes tokens to standard forms
- `IAddressEncoder` — converts normalized tokens to a compact encoding
- `IAddressDecoder` — reconstructs address text from encoding (for round-trip testing)
- `IAddressComparer` — compares two encodings for match confidence
- `IAddressEncodingPipeline` — orchestrates the full encode/compare/decode flow

The `NaiveAddressPipeline` composes `NaiveAddressProcessor` and `NaiveAddressComparer` into a single pipeline: tokenize, normalize, encode, compare. The pipeline accepts `ReadOnlySpan<char>` for both address lines, avoiding string allocations at the API boundary.

---

## Evidence: USPS Normalization Dictionary (170+ Mappings)

`UspsNormalizer` contains a dictionary of 170+ USPS standard abbreviations covering street suffixes (STREET/ST, AVENUE/AVE, BOULEVARD/BLVD), directionals (NORTH/N, SOUTHWEST/SW), and plural forms (HEIGHTS/HT, ISLANDS/IS). The normalizer handles bidirectional equivalence: "STREET", "STREETS", and "ST" all normalize to "ST".

Key methods:
- `Normalize(string token)` — returns the USPS abbreviation, or the uppercased token unchanged if no mapping exists
- `AreEquivalent(string token1, string token2)` — normalizes both and compares
- `NormalizeTokens(string[])` — batch normalization without LINQ overhead (manual for loop)
- `NormalizeText(string)` — tokenizes on whitespace/punctuation and normalizes each token

---

## Evidence: Two-Layer Matching — Exact Tokens + Semantic Similarity

The `NaiveAddressComparer` implements a two-layer matching strategy:

1. **Hard constraint — exact numeric tokens**: All numbers extracted from the address (house numbers, apartment numbers) must match exactly in count and value. If "319" appears in one address but "329" in another, the match fails immediately with score 0.0 regardless of how similar the street names are. This prevents the most dangerous class of false positive: matching different house numbers on the same street.

2. **Soft constraint — semantic similarity**: If exact tokens pass, cosine similarity is computed over the semantic embedding vectors. The naive implementation uses token-length ratios as embeddings (each token's character count divided by total address length). A score >= 0.7 counts as a semantic match.

The `AddressEncoding` struct separates these concerns explicitly: `ExactTokens` (a `short[]` of numeric values in order found) and `SemanticEmbedding` (a `float[]` of concept vectors). The struct comment notes the design intention: "Future: neural embeddings for 'south west', 'tree names', positional concepts."

---

## Evidence: Naive Processor with USPS-Aware Tokenization

The `NaiveAddressProcessor` implements four interfaces (`IAddressTokenizer`, `IAddressNormalizer`, `IAddressEncoder`, `IAddressDecoder`). Tokenization uses `ReadOnlySpan<char>` input, converts to uppercase, splits on whitespace and punctuation via source-generated regex (`[GeneratedRegex]`), and applies `UspsNormalizer.NormalizeTokens` to every token before returning.

The numeric extraction uses a compiled regex (`\d+`) and stores results as `short[]`, capturing house numbers, apartment numbers, and ZIP fragments. The semantic embedding is deliberately crude: each token's length divided by total address length, producing a vector that captures rough token prominence.

---

## Evidence: Validation Against Real Data (27M Records)

`AddressAnalysisDemo` validates the pipeline against a production SQL Server database containing 27,248,382 address records. The `SqlAddressLoader` streams addresses via `IAsyncEnumerable<AddressEntity>` with ordinal-based reading for performance.

The demo runs three analyses:
1. **Format analysis** — samples random addresses, counts token distributions, identifies common patterns
2. **Round-trip testing** — encodes real addresses, decodes them, re-encodes, and measures whether exact tokens survive the round trip and how much embedding drift occurs (Euclidean distance)
3. **Statistical validation** — runs 1,000 round-trip tests, reports success rate, mean/median/max embedding differences, and lists failures

The `AddressEntity` class mirrors the SQL schema exactly, including the bitflag property accessors (`GetFlag(int bit)`) that decode the same `AddressFlags` enum used in production.

---

## Key Files

- `FastAddress.Components/Implementations/UspsNormalizer.cs` — 170+ USPS abbreviation mappings with bidirectional equivalence
- `FastAddress.Components/Implementations/NaiveAddressProcessor.cs` — Tokenizer/normalizer/encoder/decoder with ReadOnlySpan input and source-generated regex
- `FastAddress.Components/Implementations/NaiveAddressComparer.cs` — Two-layer matching: exact numeric gate + cosine similarity
- `FastAddress.Components/Pipeline/NaiveAddressPipeline.cs` — Composed pipeline orchestrating tokenize → normalize → encode → compare
- `FastAddress.Components/Models/AddressEncoding.cs` — Encoding struct: ExactTokens (short[]) + SemanticEmbedding (float[])
- `FastAddress.Demo/AddressAnalysisDemo.cs` — Statistical validation against production data
- `FastAddress.Components.Tests/NaiveBaselineTests.cs` — 16 tests: USPS normalization, directional abbreviations, round-trip fidelity, embedding math

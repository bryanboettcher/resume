---
project: FastAddress Semantic Address Matching
company: Personal Research
dates: 2025 – present
role: Sole Researcher / Developer
tags: [dotnet, performance, simd, ml, address-matching, zero-allocation, neural-embeddings]
---

# FastAddress — Project Narrative

## Context

FastAddress is a research project developing a high-performance semantic address matching library. It evolved directly from Bryan's address normalization work at Call-Trader, where he recognized that exact string matching missed legitimate address variations that a human would recognize as identical (e.g., "STREET" vs "ST", "ELMWOOD" vs "ELM WOOD", "SW MAIN" vs "S WEST MAIN").

## Technical Approach

### Hybrid Matching Architecture
Combines explicit token matching (fast, exact) with neural embeddings (slower, semantic):

1. **Stage 0 — Hard constraints:** ZIP code and house number must match exactly. Eliminates ~95% of candidates immediately.
2. **Stage 1 — Exact hash match:** CRC64 hash comparison, ~1 CPU cycle. Catches identical normalized addresses.
3. **Stage 2 — Token-based matching:** Individual token comparison with USPS normalization, ~50 CPU cycles.
4. **Stage 3 — Neural vector similarity:** 128-dimensional character-level trigram embeddings with cosine similarity, ~200 CPU cycles. Catches semantic equivalences that token matching misses.

### Performance Engineering
- **Target:** 1–2 million address comparisons per second
- **Latency:** <500 nanoseconds per comparison
- **Memory:** ~600 bytes per address (hybrid structure)
- **Zero-allocation:** `stackalloc`, `Span<T>`, value semantics — no GC pressure on hot path
- **SIMD:** AVX2/AVX-512 for vectorized cosine similarity
- **Cache alignment:** 64-byte aligned structures for CPU cache optimization

### Token Pipeline
Five-stage processing: tokenization → lexical classification → domain normalization → domain classification → EntityDigest construction

### USPS Normalization
220+ hardcoded abbreviation mappings (STREET→ST, AVENUE→AVE, etc.) following USPS Publication 28 standards.

## Current Status

- **Phase 1 complete:** Core token architecture with 13 passing tests
- **Naive baseline working:** Exact token matching with USPS normalization
- **Phase 2 designed:** Neural embedding pipeline with contrastive loss training strategy
- **Phases 3-4 planned:** Hybrid integration and self-learning from external API feedback

## Significance for Resume

- Demonstrates performance engineering at the CPU level (SIMD, cache lines, cycle counting)
- Shows ML/AI awareness (contrastive learning, embedding spaces, training data design)
- Connects directly to production experience (Call-Trader address normalization → FastAddress research)
- Domain expertise: USPS addressing standards, deliverability classification
- Research methodology: phased approach with measurable targets at each stage

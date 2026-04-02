---
title: FastAddress Semantic Address Matching
tags: [dotnet, performance, simd, ml, address-matching, zero-allocation, neural-embeddings, avx2, benchmarkdotnet, genetic-algorithm, ml-net, csharp]
related:
  - evidence/performance-optimization.md
  - evidence/data-engineering-etl.md
  - evidence/dotnet-csharp-expertise.md
  - evidence/ai-driven-development.md
  - projects/call-trader-madera.md
  - links/github-repos.md
category: project
contact: resume@bryanboettcher.com
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

## ML/Training Infrastructure (neural-embeddings branch)

Despite the branch name, the implemented work covers the foundational training infrastructure that feeds upstream of the token pipeline — normalizing messy input into clean USPS-like format before it enters tokenization.

### Genetic Algorithm Pattern Evolution
GeneticSharp-based evolutionary search for regex pattern discovery:
- **Grammar-aware chromosomes:** Variable-length sequences of 19 typed regex tokens across 5 categories, maintaining syntactic validity through grammar-aware mutations (not raw string mutation)
- **7 mutation operators:** Insert, delete, replace, swap, quantifier modification, negation toggle, complex pattern insertion
- **Fitness function:** Weighted F-beta score with complexity penalty, parallelized via `Partitioner.Create()` across CPU cores
- **Population management:** 1K–100K population, 3% elite preservation, tournament selection, `O(n log k)` top-K selection via binary search
- **Zero-allocation fitness scoring:** `ReadOnlyMemory<WeightedTrainingPair>` slices, `ref struct` score context, non-allocating `Regex.EnumerateMatches()`
- **Object pooling:** `ListPool` for chromosome token lists to reduce GC pressure during evolution

### ML.NET Training Infrastructure
- **Dual-mode training:** AutoML (1-hour sweep across LightGBM, FastTree, SdcaMaximumEntropy) vs. manual LightGBM configuration
- **Text featurization:** Word 3-grams + character 5-grams, case normalization, punctuation stripping
- **Weighted training:** Inverse frequency weighting (`count / (numClasses * classFrequency)`)
- **Binary AND multiclass classification** with full metrics (AUC-ROC, F1, confusion matrix)
- **GPU acceleration:** Optional CUDA device assignment with CPU fallback

### Training Orchestration Pipeline
- **Automatic strategy planning:** `DefaultTrainingStrategyPlanner` analyzes property statistics to auto-select classifier vs. extractor based on cardinality threshold (24 distinct values)
- **TPL Dataflow pipeline:** `TransformBlock<TrainingPlan, ITrainingResult>` for concurrent training execution
- **Pluggable trainer adapters:** Strategy pattern routing training plans to genetic regex or ML.NET handlers

### Boxing-Free Generic Diagnostics
- **Zero-boxing metrics capture:** `AsyncMetricsScope<T> where T : struct` uses `ref T Instance` for direct struct mutation without boxing
- **Pub/sub metrics distribution:** MessagePipe `IAsyncPublisher<T>` for decoupled metrics routing
- **Enrichment pipeline:** `IMetricsEnricher<T>` for automatic duration/memory/CPU tracking

### Empirical Research (1.5M training examples)
- **Zipf's law analysis:** Street name vocabulary sizing at coverage percentiles (100/500/1K/2.5K/5K/10K/20K words)
- **Phonetic encoding research:** Self-contained Soundex and Metaphone implementations for similarity analysis
- **Hash collision analysis:** Empirical CRC64 collision rate validation at scale
- **Dual processing pathway:** Backend ingestion (trusted USPS data, 2–4M addresses/sec) vs. frontend comparison (untrusted input, ML normalization at 2–10μs, then 100K–500K addresses/sec)

## Current Status

- **Phase 1 complete:** Core token architecture with 13 passing tests
- **Phase 2 substantially implemented:** ML/genetic training infrastructure, empirical research complete
- **Phase 3 planned:** Hybrid integration and self-learning from external API feedback

## Significance for Resume

- Demonstrates performance engineering at the CPU level (SIMD, cache lines, cycle counting)
- Shows ML/AI awareness (contrastive learning, embedding spaces, training data design)
- Connects directly to production experience (Call-Trader address normalization → FastAddress research)
- Domain expertise: USPS addressing standards, deliverability classification
- Research methodology: phased approach with measurable targets at each stage

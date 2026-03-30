---
skill: Performance Optimization
tags: [benchmarking, SIMD, zero-allocation, data-structures, profiling, throughput]
relevance: Demonstrates a benchmark-driven, measurement-first approach to performance work across multiple projects and contexts
---

# Performance Optimization — Evidence Portfolio

## Philosophy

Bryan's approach to performance is consistently empirical: measure first, optimize second, measure again. This is visible across Stack Overflow answers, open source contributions, and personal projects. He does not make performance claims without benchmarks, and frequently builds custom benchmark harnesses to validate assumptions before committing to an approach.

---

## Evidence: FastAddress Semantic Matching System

**Project:** FastAddress (personal research project, 2025–present)
**Repository:** https://github.com/bryanboettcher/FastAddress
**Local path:** ~/src/bryanboettcher/FastAddress/

FastAddress is a high-performance semantic address matching library targeting industrial-scale throughput with sub-microsecond latency. The project demonstrates deep understanding of CPU-level optimization techniques.

### Performance Targets (documented and designed for)
- **Throughput:** 1–2 million address comparisons per second
- **Latency:** <500 nanoseconds per comparison
- **Memory:** ~600 bytes per address (hybrid structure with neural embeddings)
- **Hardware requirements:** SIMD (SSE2 minimum, AVX2 optimal, AVX-512 supported)

### Optimization Techniques Applied
- **Zero-allocation design:** Value semantics throughout, `stackalloc`, `Span<T>`, avoiding GC pressure entirely on the hot path
- **SIMD vectorization:** AVX2/AVX-512 for cosine similarity calculations on 128-dimensional embedding vectors
- **Cache-line alignment:** 64-byte aligned structures for optimal CPU cache utilization
- **Multi-stage early exit:** Four-stage comparison pipeline where cheap checks (hash comparison, ~1 CPU cycle) gate expensive checks (neural similarity, ~200 CPU cycles):
  - Stage 0: Hard constraint filtering (ZIP code, house number) — eliminates ~95% of candidates
  - Stage 1: Exact hash match (~1 CPU cycle)
  - Stage 2: Token-based matching (~50 CPU cycles)
  - Stage 3: Neural vector similarity (~200 CPU cycles)
- **Bit-packing:** Compact representation of address metadata using bitwise flags
- **CRC64 hashing:** For O(1) deduplication across millions of addresses

### Current Status
Phase 1 complete with 13 passing tests. Naive baseline implemented with USPS normalization. Neural embedding pipeline (Phase 2) designed but not yet implemented. BenchmarkDotNet integration planned for formal regression tracking.

---

## Evidence: Madera/Call-Trader Import Pipeline

**Project:** Madera Direct Mail Platform (Call-Trader, June 2024 – October 2025)
**Repository:** github.com/Call-Trader/madera-apps (private)
**Local path:** ~/src/bryanboettcher/madera-apps/

### Achieved Performance Metrics
- **Import throughput:** <10 seconds for 50,000 row CSV imports (5,000 rows/second sustained)
- **Address lookup:** <100 nanoseconds per lookup using in-memory dictionary cache
- **Bulk insert:** SqlBulkCopy with 5,000-record batch sizes for high-throughput database ingestion
- **Address normalization:** 15 million addresses processed in under 2 hours via Lob API (USPS-certified, bulk endpoint processing 20 addresses per request)
- **API response time:** <200ms p95 for read operations, <500ms p95 for write operations
- **Memory budget:** 650 MB typical / 900 MB peak for web application within 2 GB container limit
- **Address cache footprint:** 200 MB for 2 million addresses at 100 bytes per address

### Optimization Techniques
- **High-performance CSV parsing:** Used Sep and Sylvan CSV libraries (both known for zero-allocation parsing) rather than standard .NET CSV readers
- **CRC64 hash-based deduplication:** O(1) lookups across 10–15 million unique addresses using System.IO.Hashing
- **In-memory address caching:** Pre-loaded address cache for sub-100ns lookups during import processing, avoiding database round-trips on the hot path
- **Batched API calls:** Lob address normalization batched at 20 addresses per request to minimize HTTP overhead while staying within rate limits
- **Pre-filtering pipeline:** Multi-stage deduplication (pre-LOB internal dedup → post-LOB external dedup) to avoid wasting expensive API calls on duplicate addresses

---

## Evidence: Stack Overflow — Benchmark-Driven Answers

### Answer: HashSet vs. Sorted Array Binary Search for 256-bit Hashes
**URL:** https://stackoverflow.com/a/11227902 (Score: 62, Accepted)
**Question:** "What's an appropriate search/retrieval method for a VERY long list of strings?" (Score: 66, 4,879 views)

Bryan wrote a complete benchmark implementation comparing two data structure approaches for looking up 256-bit hashes. He created a custom `Data256Bit` struct implementing `IEquatable<T>` and `IComparable<T>`, built bucketed data structures for both approaches, and ran controlled performance comparisons. The answer doesn't just recommend — it proves with measurements. This is his highest-scored Stack Overflow answer and directly demonstrates his performance engineering methodology.

### Answer: AutoMapper Performance Benchmarking
**URL:** https://stackoverflow.com/questions/tagged/automapper (Score: 14, 13,232 views)
**Question:** "Automapper running extremely slow on mapping 1400 records"

Rather than theorizing about AutoMapper's overhead, Bryan benchmarked it at 85,000 maps/second on a 2.0GHz Xeon and demonstrated it was 60x slower than manual property copying. The answer quantifies the overhead precisely, allowing the questioner to make an informed architectural decision about whether the convenience tradeoff is acceptable for their use case.

---

## Evidence: Service Management Group (2016–2018)

Bryan's resume states he achieved "80% performance improvements in some applications" at SMG. While specific details of the optimizations are not publicly documented, this claim combined with his demonstrated benchmark-driven methodology across other projects suggests systematic profiling and optimization work rather than accidental improvements.

---

## Evidence: Open Source — Lamar IoC Container Fix
**URL:** https://github.com/JasperFx/lamar/pull/362 (Merged December 2022)

Fixed a `StackOverflowException` in the Lamar IoC container (607 stars, maintained by Jeremy Miller) by converting recursive method calls to iterative processing. This is a classic performance/reliability optimization — recursive approaches fail on deep dependency graphs, and the iterative conversion maintained identical behavior while eliminating the stack depth constraint. The PR touched topological sort, expression writing, and resolver building — demonstrating understanding of the container's internal compilation pipeline.

---

## Summary Pattern

Bryan's performance work follows a consistent methodology:
1. **Measure the baseline** — always benchmark before optimizing
2. **Identify the bottleneck** — profiling, not guessing
3. **Apply appropriate technique** — SIMD, caching, batching, algorithmic improvement, data structure selection
4. **Verify the improvement** — benchmark again, document the delta
5. **Design for the constraint** — memory budgets, latency targets, throughput goals are set upfront and designed toward

This is not "premature optimization" — it's engineering to known constraints with empirical validation.

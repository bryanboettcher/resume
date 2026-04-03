# Restart Prompt — Resume Project

## Current State (2026-04-03)

**Branch:** `feature/retrieval-enhancement` off `main`
**Working tree:** dirty — spike code in progress (see below)
**Docker image:** `ghcr.io/bryanboettcher/resume-chat:latest` was rebuilt and pushed (2026-04-02) with all main branch changes (OTel, Anthropic SDK, completion base class)
**Frontend:** synced to angryhosting.com/devel/ (2026-04-02)
**K8s:** Bryan is handling manifest updates in a separate session

### What's on main (unchanged from prior session)

1. Chatbot branch squash — Full RAG pipeline, FluentValidation, threat classifier, canary injection detection, corpus analysis CLI, 135 evidence/project docs
2. OTel observability — Aspire ServiceDefaults, RagDiagnostics, spans+metrics on all pipeline classes
3. Anthropic SDK completion — CompletionProviderBase, ClaudeCompletionProvider, provider selection
4. Patchup — Removed duplicated OTel instrumentation

### What's on the feature branch (dirty, uncommitted)

**Spike code already written (needs rework per design discussion below):**
- `IVectorStore.SearchAsync` — added `int? dimensions` parameter
- `QdrantVectorStore.SearchAsync` — implemented Matryoshka truncation: when `dimensions` is set, fetches 4x candidates with `with_vectors: true`, truncates both query and stored vectors, L2-normalizes, recomputes cosine similarity client-side, re-ranks, returns top K. Added `TruncateAndNormalize()` and `CosineSimilarity()` private helpers. Added `Vector` field to `QdrantSearchResult` record.
- `IRetrievalProvider.RetrieveAsync` — added `int? dimensions` parameter
- `VectorRetrievalProvider.RetrieveAsync` — passes dimensions through
- `ChatEndpoints.cs` — updated call to use `cancellationToken:` named arg
- `EmptyRetrievalProvider` (test double) — updated signature
- `DebugRetrievalEndpoints.cs` — NEW FILE, `GET /api/debug/retrieval?query=...&topK=5&dimensions=256`, calls retrieval provider, returns JSON with scores/sources/sections/tags/preview
- `WebApplicationExtensions.cs` — wired up DebugRetrievalEndpoints

**Build status:** Api, Rag, and both test projects compile clean. `ResumeChat.Corpus.Cli` has pre-existing errors (missing `OllamaOptions` type) unrelated to our work.

**Tests:** Not yet run after changes (user interrupted before test execution).

### Design Discussion (IN PROGRESS — this is where to resume)

We were iterating on the retrieval pipeline architecture. Bryan wants to be slightly on the "overengineered" side for pluggability/abstractions — his preference is extensible design that scales to complex projects, not minimal-change task completion.

**Agreed design direction — RetrievalRequest + IQueryPreprocessor:**

The current positional parameters (`topK`, `dimensions`, `minScore`) should become a request object. A preprocessor pipeline transforms raw user input into the prepared request.

```csharp
public sealed record RetrievalRequest
{
    public required string OriginalQuery { get; init; }
    public required string ProcessedQuery { get; init; }  // after synonym expansion, normalization
    public int TopK { get; init; } = 5;
    public int? Dimensions { get; init; }
    public float? MinScore { get; init; }
}

public interface IQueryPreprocessor
{
    Task<RetrievalRequest> PrepareAsync(string query, CancellationToken ct = default);
}
```

**Agreed pipeline ordering:**
```
ChatEndpoints
  → IQueryPreprocessor.PrepareAsync("big data experience")
  → RetrievalRequest { OriginalQuery, ProcessedQuery, TopK, Dimensions, MinScore }
  → IThreatClassifier.ClassifyAsync(request.OriginalQuery)  // safety on raw input
  → IRetrievalProvider.RetrieveAsync(RetrievalRequest)
      // VectorRetrievalProvider internally:
      //   embed ProcessedQuery → build VectorSearchRequest → IVectorStore.SearchAsync
      // TagRetrievalProvider (hypothetical):
      //   parse tags → Qdrant payload filter → no embedding at all
  → IReadOnlyList<ScoredChunk>
  → ICompletionProvider.CompleteAsync(...)
```

**Key design decisions made:**
- `VectorSearchRequest` is an **internal** concern of `VectorRetrievalProvider`, not part of the public contract. Different retrieval implementations build completely different internal requests from the same `RetrievalRequest`.
- Threat classifier sees `OriginalQuery` (what user typed), not the expanded/processed form
- Completion provider gets both original and processed query
- `IQueryPreprocessor` is pure logic — no HTTP, no embeddings. Synonym expansion, query decomposition, dimension policy all live here and are unit-testable in isolation.
- The preprocessor pipeline is the future home for `IEnumerable<IQueryVisitor>` or similar composable transforms

**Open question (where we left off):**
Where does the dimension policy live? Two options:
1. **Preprocessor** — "for this corpus size, use 256 dims" is a query-preparation concern. The preprocessor sets `Dimensions` on the request.
2. **Config-driven on retrieval side** — dimensions as an options value that the retrieval provider reads, not a per-query decision.

The lean was toward preprocessor, but Bryan hadn't answered yet.

### Matryoshka Truncation — Why We're Doing This

The retrieval pipeline has a quality problem. Vague/conversational queries ("bryan's big data experience") miss relevant docs because `nomic-embed-text` at 768 dimensions over-separates conceptually related but lexically different terms.

**Hypothesis:** nomic-embed-text supports Matryoshka Representation Learning (trained to be truncated: 768→512→256→128→64). At lower dimensions, the model is forced to collapse related concepts — "big data", "ETL", "data pipeline" cluster together instead of occupying distinct regions.

**Why it fits this corpus:** 562 chunks is tiny. You don't need 768 dimensions of discrimination. The extra capacity is actively hurting by over-separating related concepts. False positives at low dims are acceptable — this is a conversation starter, not archival retrieval.

**Implementation approach (already spiked in QdrantVectorStore):** Store full 768-dim vectors. At search time, fetch candidates with `with_vectors: true`, truncate both query and stored vectors to target dimension, L2-normalize, recompute cosine similarity, re-rank. No re-ingestion needed.

### Remaining Retrieval Enhancement Work

1. **Refactor to RetrievalRequest/IQueryPreprocessor** — rework the spike code per the design discussion
2. **Relevance threshold** — `MinScore` on RetrievalRequest, applied after vector search
3. **Query preprocessing** — static synonym expansion map in the preprocessor (Bryan suggested a sonnet subagent could generate the C# dictionary by analyzing the corpus vocabulary)
4. **Multi-query** — embed original + normalized variant, merge/deduplicate results
5. **Debug endpoint** — keep `/api/debug/retrieval` for experimentation, returns JSON with scores
6. **Test harness** — golden query files, fake corpus fixtures, score threshold assertions. The preprocessor is pure-logic and independently testable.
7. **Experiment** — run problem queries at 768 vs 256 vs 128 to validate the Matryoshka hypothesis before committing to the architecture

### Conventions

- Options pattern with `ValidateOnStart()` for any new config
- Extension methods for DI registration
- `ConfigureAwait(false)` on all awaits
- Integration tests via `WebApplicationFactory`, unit tests for isolated logic
- No LLM calls in the query preprocessing path — static/fast techniques only
- Bryan prefers extensible/pluggable abstractions over minimal implementations

### Docker Compose (`docker-compose.yml` at repo root)

- **Aspire dashboard:** `localhost:18888` (UI), `localhost:18889` (OTLP gRPC)
- **Qdrant:** `localhost:6333`, data in `qdrant-data` named volume
- **API:** `localhost:5000`, corpus bind-mounted from evidence/projects/links
- **Caddy frontend:** `localhost:8080`
- **Ollama:** host network, `host.docker.internal:11434`
- **Ingestion:** `curl -X POST http://localhost:5000/api/admin/ingest -H "X-Api-Key: abc123"`

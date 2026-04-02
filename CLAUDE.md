# Resume Project

## Purpose
This project contains structured evidence documents, project narratives, and link registries that serve as source material for Bryan Boettcher's resume. The eventual goal is a static PDF resume (via Typst) AND a dynamic web resume with an embedded RAG chatbot that answers "how would Bryan solve this?" using real examples from his work.

## Current Phase
**Phase 2: Embeddings & RAG Pipeline** — see `RESTART_PHASE2.md` for full work items.
- Phase 1 complete: static SPA at `bryanboettcher.com/devel/`, C# API at `resume-chat.mallcop.dev`, PHP proxy with rate limiting, SSE streaming chat widget
- Phase 2: corpus ingestion, retrieval pipeline, real completion providers (Ollama dev / Claude prod), wire RAG end-to-end

## Directory Structure

```
evidence/          — One file per skill claim, self-contained with examples/links/metrics (RAG corpus)
projects/          — One file per major project/role with full narratives (RAG corpus)
links/             — URL registries with descriptions (fragile to reconstruct, preserve carefully)

backend/
  src/
    ResumeChat.Api/          — ASP.NET Core 10 minimal API
      Program.cs             — Bootstraps app: AddApplicationServices(), rate limiter, API key middleware, endpoints
      Endpoints/
        ChatEndpoints.cs     — POST /api/chat (SSE streaming via StreamSse), GET /api/chat/health
      Extensions/
        WebApplicationBuilderExtensions.cs  — AddApplicationServices(): options, rate limiting, DI
        ServiceCollectionExtensions.cs      — AddResumeChatRateLimiting(): fixed window policy
        WebApplicationExtensions.cs         — MapApplicationEndpoints()
      Middleware/
        ApiKeyMiddleware.cs  — X-Api-Key header check, exempts /api/chat/health
      Options/
        ApiKeyOptions.cs     — "ApiKey" section, required Key property, ValidateOnStart()
        RateLimitOptions.cs  — "RateLimit" section, PermitLimit=10, WindowSeconds=60
        CorpusOptions.cs     — "Corpus" section: Directory path to corpus files
      Validation/
        ChatRequestValidator.cs  — FluentValidation: not empty, ≤2048 chars, 18 regex injection patterns
        ValidationFilter.cs      — Generic IEndpointFilter for FluentValidation on minimal API endpoints
    ResumeChat.Rag/          — RAG library (all abstractions and implementations)
      ICompletionProvider.cs — IAsyncEnumerable<string> CompleteAsync(CompletionRequest, ct)
      HardcodedCompletionProvider.cs — Demo impl, echoes user message words
      ChatResponses.cs       — Shared response constants (Unrelated rejection message)
      Models/
        DocumentMetadata.cs  — SourceFile, Title, Tags
        DocumentChunk.cs     — Text, SectionHeading, ChunkIndex, Metadata
        EmbeddedChunk.cs     — Chunk + ReadOnlyMemory<float> Embedding
        ScoredChunk.cs       — Chunk + Score
        CompletionRequest.cs — UserMessage + IReadOnlyList<ScoredChunk> Context
      Chunking/
        IChunkingStrategy.cs — IReadOnlyList<DocumentChunk> Chunk(content, metadata)
        MarkdownSectionChunkingStrategy.cs — Split on ##, cap ~400 tokens, split at paragraphs
      Classification/
        IThreatClassifier.cs            — Task<ThreatResult> ClassifyAsync(message, ct)
        ThreatResult.cs                 — IsThreat + ThreatScore, factory methods Safe()/Threat()
        OllamaThreatClassifier.cs       — qwen3:4b SAFE/UNSAFE classification, 10s timeout, fail-closed
        OllamaThreatClassifierOptions.cs — "Ollama:Guard" section: BaseUrl, Model, TimeoutSeconds
        PassthroughThreatClassifier.cs  — Always returns Safe(), used in tests
      Embedding/
        IEmbeddingProvider.cs           — Task<ReadOnlyMemory<float>> EmbedAsync(text, ct)
        OllamaEmbeddingProvider.cs      — Ollama /api/embed HTTP client
        OllamaEmbeddingOptions.cs       — "Ollama:Embedding" section: BaseUrl, Model
      VectorStore/
        IVectorStore.cs      — Upsert, Search, EnsureCollection
        QdrantVectorStore.cs — Qdrant REST API client, SHA256 deterministic point IDs
        QdrantOptions.cs     — "Qdrant" section: BaseUrl, CollectionName
      Retrieval/
        IRetrievalProvider.cs        — Task<IReadOnlyList<ScoredChunk>> RetrieveAsync(query, topK, ct)
        VectorRetrievalProvider.cs   — Embed query → Qdrant search → ranked chunks
      Ingestion/
        IIngestionPipeline.cs        — IAsyncEnumerable<EmbeddedChunk> IngestAsync(directory, ct)
        CorpusIngestionPipeline.cs   — Read markdown → chunk → embed → yield
        IngestionService.cs          — Orchestrator: probe dimensions → ensure collection → ingest → upsert
      Completion/
        CompletionSecurityOptions.cs  — "Security" section: Canary (sentinel token for injection detection)
        SystemPromptBuilder.cs       — Hardened system prompt: evidence-only, no generic advice, canary injection
        OllamaCompletionProvider.cs  — Ollama /api/chat streaming
        OllamaCompletionOptions.cs   — "Ollama:Completion" section: BaseUrl, Model
        ClaudeCompletionProvider.cs  — Anthropic Messages API streaming
        ClaudeCompletionOptions.cs   — "Claude" section: ApiKey, Model, MaxTokens
  tests/
    ResumeChat.Api.Tests/    — Integration tests with WebApplicationFactory
      ApiFactory.cs          — Test factory, overrides completion + retrieval + threat classifier with test doubles
      ChatApiTests.cs        — 5 tests: health, auth, startup validation, SSE streaming
    ResumeChat.Cli/          — Console app for corpus ingestion
      Program.cs             — Resolves IngestionService, streams progress to stdout
    ResumeChat.Rag.Tests/    — Unit tests for RAG components
      HardcodedCompletionProviderTests.cs       — 5 tests: streaming, edge cases, cancellation
      MarkdownSectionChunkingStrategyTests.cs   — 7 tests: sections, frontmatter, large splits, indexing
      ThreatClassifierTestCases.cs             — 75 attack strings + 8 safe queries for live Ollama testing

frontend/
  index.html                 — Static SPA: 60/40 split layout (resume left, chat right)
  api/
    chat.php                 — PHP proxy: rate limiting, SSE passthrough, canary detection
    config.example.php       — Config template for API key/URL/canary

Dockerfile                   — Multi-stage: SDK build + test → ASP.NET runtime, port 5000. Corpus mounted at /app/corpus via k8s volume.
```

## Backend Architecture

- **Target:** net10.0, nullable enabled, implicit usings
- **Image:** `ghcr.io/bryanboettcher/resume-chat:latest`
- **Dev ports:** HTTP 5014, HTTPS 7036
- **Streaming:** SSE format (`data: {chunk}\n\n`, terminated by `data: [DONE]\n\n`)
- **Auth:** API key via `X-Api-Key` header, middleware-level
- **Rate limiting:** .NET built-in RateLimiter, "chat" policy, configurable via options
- **Tests run in Docker build** — image fails if tests fail

### RAG Pipeline (built)
- `IChunkingStrategy` → `MarkdownSectionChunkingStrategy` (split on ##, ~400 token cap)
- `IEmbeddingProvider` → `OllamaEmbeddingProvider` (Ollama /api/embed)
- `IVectorStore` → `QdrantVectorStore` (Qdrant REST API)
- `IIngestionPipeline` → `CorpusIngestionPipeline` (read → chunk → embed → yield)
- `IRetrievalProvider` → `VectorRetrievalProvider` (embed query → Qdrant search)
- `ICompletionProvider` → `HardcodedCompletionProvider` | `OllamaCompletionProvider` | `ClaudeCompletionProvider`
- `SystemPromptBuilder` — hardened prompt: evidence-grounded answers only, refuses off-topic/injection, injects canary sentinel
- Provider selection via `Completion:Provider` config key ("Hardcoded" | "Ollama" | "Claude")

### Prompt Injection Defense (layered)
Defense in depth with three layers:
1. **FluentValidation endpoint filter** — `ChatRequestValidator` with 18 source-generated regex patterns catches obvious attacks (system prompt extraction, role hijacking, delimiter injection, data exfiltration, encoding tricks). Rejected at 400 before handler runs.
2. **Threat classification** — `IThreatClassifier` → `OllamaThreatClassifier` (qwen3:4b SAFE/UNSAFE classification). 10s timeout, fail-closed on timeout/error/garbage output. Checks UNSAFE first (superset of SAFE), then requires explicit SAFE. Provider selection via `Guard:Provider` config key ("Ollama" or omit for passthrough).
3. **Canary sentinel** — Backend injects canary token (from `Security:Canary` config) into system prompt. PHP proxy scans SSE output with sliding window buffer (handles cross-chunk boundaries). If detected: aborts response, burns session rate limit, spikes threat score +50.
4. **Session threat scoring** — `X-Threat-Score` header flows: backend → PHP proxy (accumulates in session) → backend (on subsequent requests). Canary trips add +50.
- Both sides must share the same canary value — backend via `Security:Canary` in appsettings, PHP via `canary` in config.php
- `CompletionSecurityOptions` has `[Required]` + `[MinLength(16)]` — app refuses to start without it
- `ChatResponses.Unrelated` — single constant for all rejection messages across validator, classifier, and endpoint

### Ingestion
- **Endpoint:** `POST /api/admin/ingest` — streams SSE progress, requires API key
- **CLI:** `dotnet cli/ResumeChat.Cli.dll <corpus-dir>` — console output
- **Status:** `GET /api/admin/ingest/status` — placeholder, returns ready
- Corpus mounted as k8s volume at `/app/corpus`, not baked into image

### Not Yet Done
- Set matching `Security:Canary` value in backend appsettings AND PHP config.php
- Switch Ollama completion model to `qwen2.5:14b` (llama3.2:3b fails all injection tests)
- Production config/secrets for Claude API key
- K8s volume mount for corpus directory
- Docker image rebuild + deploy to cluster
- End-to-end test with real Ollama + Qdrant

## Frontend Architecture

- **Layout:** 60/40 CSS grid split — resume scrolls on left, chat is full-height on right
- **Mobile:** stacks vertically, sticky "Ask about Bryan's experience" bar jumps to chat
- **Chat UI:** bubble-style (user right, bot left), typing indicator, auto-grow textarea
- **Markdown:** `marked.js` + `DOMPurify` renders bot responses; styled for paragraphs, lists, code, blockquotes
- **SSE parser:** buffers by `\n\n` delimiters, reassembles multi-line `data:` fields per SSE spec
- **No jQuery** — vanilla JS throughout
- **Deployed to:** `bryanboettcher.com/devel/` via rsync to `angryhosting.com:/home/insta/bryanboettcher.com/www/html/devel/`

## Infrastructure

- **Cluster:** 96x Ryzen 9 cores, 288GB RAM, NVMe — no GPU
- **Qdrant:** To be deployed to k8s cluster (separate session)
- **Ollama:** Already running on cluster at `https://llm.mallcop.dev` — has `qwen2.5:14b` (recommended) and `llama3.2:latest` (too weak for injection resistance)

## Key Facts

- **Name:** Bryan Boettcher
- **Email:** resume@bryanboettcher.com
- **Location:** Kansas City, KS area
- **Experience:** 25+ years in software engineering
- **Current status:** Between roles (since October 2025)
- **Target role:** AI-first workflow with RAG experience, performance focus
- **Primary tech:** C#/.NET 9, ASP.NET Core, MassTransit, Angular 19, TypeScript, Docker, Kubernetes, Rust

## Career Timeline (Corrected)

1. Call-Trader — Senior/Lead Engineer (Jun 2024 – Oct 2025) — **MISSING FROM CURRENT RESUME**
2. Taylor Summit Consulting — Software Architect/Lead (2023 – Oct 2025) — concurrent with Call-Trader
3. Kansys, Inc. — Software Architect/Lead (2020 – 2023)
4. Henry Wurst / Mittera Creative Services — Sr. Developer (2018 – 2020)
5. Service Management Group — Sr. Developer (2016 – 2018)
6. Earlier roles (2001–2016): iModules, VI Marketing, Ticket Solutions, Softek Solutions, Cities Unlimited

## Resume Tooling

- **Format:** Typst (`.typ` files)
- **Compile:** `typst compile resume.typ` → produces `resume.pdf`
- **Watch mode:** `typst watch resume.typ` for live reload during development
- **View rendered output:** Read the PDF via the Read tool to check layout

## Conventions

### Content
- The evidence docs are the RAG corpus — keep them verbose, self-contained, and accurate
- The link registries are fragile to reconstruct — verify URLs still work before citing them
- The resume PDF should be concise (2 pages max) but the evidence docs backing it are intentionally detailed
- Do NOT be sycophantic — accuracy over flattery. His livelihood depends on this document.
- Most resumes are now LLM-ingested — structure content for both human and machine readability

### Backend Code
- Options pattern with `ValidateOnStart()` for all config sections
- Extension methods for DI registration (`AddXxxServices()` pattern)
- `ConfigureAwait(false)` on all awaits
- Endpoint organization in `Endpoints/` with static `MapTo(WebApplication)` pattern
- `IAsyncEnumerable<string>` for streaming responses
- Proper `CancellationToken` propagation throughout
- Tests are integration-first via `WebApplicationFactory`, unit tests for isolated logic
- Branch: `chatbot`, remote: `nas`

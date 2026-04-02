# Resume Project

## Purpose
This project contains structured evidence documents, project narratives, and link registries that serve as source material for Bryan Boettcher's resume. The eventual goal is a static PDF resume (via Typst) AND a dynamic web resume with an embedded RAG chatbot that answers "how would Bryan solve this?" using real examples from his work.

## Current Phase
**Phase 2: Embeddings & RAG Pipeline**
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
        ChatEndpoints.cs     — POST /api/chat (SSE streaming), GET /api/chat/health
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
    ResumeChat.Rag/          — RAG library (all abstractions and implementations)
      ICompletionProvider.cs — IAsyncEnumerable<string> CompleteAsync(CompletionRequest, ct)
      HardcodedCompletionProvider.cs — Demo impl, echoes user message words
      Models/
        DocumentMetadata.cs  — SourceFile, Title, Tags
        DocumentChunk.cs     — Text, SectionHeading, ChunkIndex, Metadata
        EmbeddedChunk.cs     — Chunk + ReadOnlyMemory<float> Embedding
        ScoredChunk.cs       — Chunk + Score
        CompletionRequest.cs — UserMessage + IReadOnlyList<ScoredChunk> Context
      Chunking/
        IChunkingStrategy.cs — IReadOnlyList<DocumentChunk> Chunk(content, metadata)
        MarkdownSectionChunkingStrategy.cs — Split on ##, cap ~400 tokens, split at paragraphs
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
        SystemPromptBuilder.cs       — Builds grounded system prompt from CompletionRequest context
        OllamaCompletionProvider.cs  — Ollama /api/chat streaming
        OllamaCompletionOptions.cs   — "Ollama:Completion" section: BaseUrl, Model
        ClaudeCompletionProvider.cs  — Anthropic Messages API streaming
        ClaudeCompletionOptions.cs   — "Claude" section: ApiKey, Model, MaxTokens
  tests/
    ResumeChat.Api.Tests/    — Integration tests with WebApplicationFactory
      ApiFactory.cs          — Test factory, overrides completion + retrieval with test doubles
      ChatApiTests.cs        — 5 tests: health, auth, startup validation, SSE streaming
    ResumeChat.Cli/          — Console app for corpus ingestion
      Program.cs             — Resolves IngestionService, streams progress to stdout
    ResumeChat.Corpus.Cli/   — Source-code analysis pipeline CLI
      Program.cs             — Command routing: scan, analyze, triage, full-analysis; CLI override wiring
      CorpusDatabase.cs      — EF Core + raw Npgsql: upserts, analysis queries, tag inserts
      CorpusDbContext.cs     — Schema: source_files, file_analysis, file_tags, file_relationships
      SourceTreeWalker.cs    — Recursive directory walker with extension/size/name filtering
      SourceFile.cs          — Record: Repo, Branch, FilePath, Language, ContentText, ContentHash, LineCount, SizeBytes
      OllamaAnalyzer.cs      — Ollama /api/generate client; TriageObservation + FullAnalysisResult models
      IOllamaAnalyzer.cs     — Interface: TriageAsync, TriageDetailAsync, AnalyzeAsync
      AnalysisRunner.cs      — Concurrent two-phase runner: SemaphoreSlim-bounded triage → full analysis
      IAnalysisRunner.cs     — Interface: RunTriageAsync, RunFullAnalysisAsync → AnalysisStats
      AnalysisFilter.cs      — Parsed CLI filter+overrides: --repo, --branch, --language, --limit, --ollama-url, --concurrency, --model
      CorpusOptions.cs       — "Corpus" section (ConnectionString, Sources[]); "Ollama" section (BaseUrl, Model, MaxConcurrency)
      appsettings.json       — DB connection, source paths, Ollama endpoint
    ResumeChat.Rag.Tests/    — Unit tests for RAG components
      HardcodedCompletionProviderTests.cs       — 5 tests: streaming, edge cases, cancellation
      MarkdownSectionChunkingStrategyTests.cs   — 7 tests: sections, frontmatter, large splits, indexing

frontend/
  api/
    chat.php                 — PHP proxy with session-based rate limiting
    config.example.php       — Config template for API key/URL

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
- `SystemPromptBuilder` — grounds completion in retrieved context
- Provider selection via `Completion:Provider` config key ("Hardcoded" | "Ollama" | "Claude")

### Ingestion
- **Endpoint:** `POST /api/admin/ingest` — streams SSE progress, requires API key, error handling with [ERROR]/[CANCELLED]/[DONE] events
- **CLI:** `dotnet run --project src/ResumeChat.Cli -- <corpus-dir>` — runs from workstation, NOT in Docker image
- **CLI config:** `src/ResumeChat.Cli/appsettings.json` uses external URLs (llm.mallcop.dev, qdrant.mallcop.dev)
- **Status:** `GET /api/admin/ingest/status` — placeholder, returns ready
- **Current corpus:** 156 chunks (94 evidence, 49 projects, 13 links) embedded with nomic-embed-text (768 dims)

### Corpus Analysis CLI (`ResumeChat.Corpus.Cli`)
- **Scan:** `dotnet run --project src/ResumeChat.Corpus.Cli` — walks source trees, loads files into PostgreSQL
- **Analyze:** `dotnet run --project src/ResumeChat.Corpus.Cli -- analyze` — two-phase: triage then full analysis
- **Triage only:** `dotnet run --project src/ResumeChat.Corpus.Cli -- triage`
- **Full analysis only:** `dotnet run --project src/ResumeChat.Corpus.Cli -- full-analysis`
- **Filter/override flags** (all three analysis commands): `--repo`, `--branch`, `--language`, `--limit`, `--ollama-url`, `--model`, `--concurrency`
- **Database:** PostgreSQL 17 on port 5433, `docker compose -f docker-compose.corpus.yml up -d`
- **LLM:** Ollama with qwen2.5-coder:7b on local GPU for analysis
- **Schema:** `source_files` → `file_analysis` (`analysis_type` = `triage` or `full_analysis`) → `file_tags` (resume keywords) → `file_relationships`
- **Triage approach:** Binary-observation strategy — LLM answers four boolean dimensions for each file: `has_logic`, `has_domain_rules`, `has_composition`, `has_data_modeling`. Any dimension true → medium interest (proceed to full analysis); all false → low (skip). Result stored as `TriageObservation` JSON in `file_analysis` with `analysis_type='triage'`.
- **Concurrency:** `AnalysisRunner` uses `SemaphoreSlim` bounded by `OllamaOptions.MaxConcurrency` (default 1, overridable via `--concurrency`). Both triage and full-analysis phases run concurrently within that bound.
- **Full analysis output:** `purpose`, `domain_concepts`, `patterns`, `notable_techniques`, `frameworks`, `interactions`, `complexity` (low/medium/high), `resume_keywords` — keywords also written to `file_tags`.
- **Current state:** ~2,982 files ingested (madera-apps, FastAddress, kb-platform, homelab)
- **Next:** MCP server to query analysis data for RAG pipeline

### Not Yet Done
- Claude completion provider needs API key secret for production use
- Frontend chat widget may need SSE newline handling updates
- Prompt tuning based on real usage
- Ingestion automation (re-run when corpus changes)
- Squash & merge chatbot branch to main

## Infrastructure

- **Cluster:** 3x nodes, 32 CPU / 96GB RAM each — no GPU
- **Ollama:** `ollama.ai.svc.cluster.local:11434` / external: `llm.mallcop.dev` — models: llama3.2, nomic-embed-text, mxbai-embed-large
- **Qdrant:** `qdrant.qdrant.svc.cluster.local:6333` / external: `qdrant.mallcop.dev` — collection: resume-chunks
- **API:** `resume-chat.mallcop.dev` — image: `ghcr.io/bryanboettcher/resume-chat:latest`

### Deployment Config (env vars for pod)
```
ApiKey__Key=<secret>
Ollama__Embedding__BaseUrl=http://ollama.ai.svc.cluster.local:11434
Ollama__Embedding__Model=nomic-embed-text
Ollama__Completion__BaseUrl=http://ollama.ai.svc.cluster.local:11434
Ollama__Completion__Model=llama3.2
Qdrant__BaseUrl=http://qdrant.qdrant.svc.cluster.local:6333
Qdrant__CollectionName=resume-chunks
Completion__Provider=Ollama
Corpus__Directory=/app/corpus
```

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

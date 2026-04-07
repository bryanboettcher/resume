# Resume Project

## Purpose
Static PDF resume (via Typst) and a dynamic web resume with an embedded RAG chatbot that answers questions about Bryan's experience using real examples from the evidence corpus.

## Directory Structure

```
corpus/
  evidence/        — One file per skill claim, self-contained with examples/links/metrics
  projects/        — One file per major project/role with full narratives
  links/           — URL registries with descriptions (fragile to reconstruct, preserve carefully)
  push-corpus.sh   — Bulk upload script for corpus docs to the API

backend/
  src/
    ResumeChat.Api/        — ASP.NET Core 10 minimal API (entry point)
    ResumeChat.Rag/        — RAG library: chunking, embedding, retrieval, response providers, pipeline
    ResumeChat.Storage/    — PostgreSQL persistence: EF Core, repositories, caching orchestrator
    ResumeChat.Cli/        — Console app for corpus ingestion
    ResumeChat.ServiceDefaults/ — Aspire service defaults (OTel, health checks)
    ResumeChat.AppHost/    — Aspire app host
  tests/
    ResumeChat.Api.Tests/          — Integration tests via WebApplicationFactory
    ResumeChat.Rag.Tests/          — Unit tests for RAG components
    ResumeChat.Rag.Pipeline.Tests/ — Pipeline, enricher, and transformer tests
    ResumeChat.Storage.Tests/      — Storage layer tests

frontend/
  index.html           — Static SPA: 60/40 split layout (resume left, chat right)
  api/
    bootstrap.php      — Shared helpers: config, rate limiting, SSE parsing, session management
    chat.php           — PHP streaming proxy: session history, canary detection, threat scoring
    health.php         — Health check proxy
    config.example.php — Config template

resume.typ             — Typst resume source
resume.pdf             — Compiled resume
docker-compose.yml     — Local dev: Qdrant, Postgres, Aspire dashboard, API, frontend
```

## Backend Architecture

- **Target:** net10.0, nullable enabled, implicit usings
- **Image:** `ghcr.io/bryanboettcher/resume-chat:latest`
- **Dev ports:** HTTP 5014
- **Streaming:** SSE format (`data: {chunk}\n\n`, terminated by `data: [DONE]\n\n`)
- **Auth:** API key via `X-Api-Key` header, middleware-level
- **Tests run in Docker build** — image fails if tests fail

### RAG Pipeline
- `IChunkingStrategy` → `MarkdownSectionChunkingStrategy` (split on ##, ~400 token cap)
- `IEmbeddingProvider` → `OllamaEmbeddingProvider` (Ollama /api/embed, nomic-embed-text)
- `IVectorStore` → `QdrantVectorStore` (Qdrant REST API, SHA256 deterministic point IDs)
- `IRetrievalProvider` → `VectorRetrievalProvider` (embed query → Qdrant search)
- `IQueryTransformer` → `DefaultQueryTransformer` (enrichers → retrieval, no threat classification)
- `IQueryEnricher` → `SynonymExpansionEnricher` (GeneratedRegex vocabulary alignment)
- `IResponseProvider` → `ClaudeResponseProvider` | `OllamaResponseProvider` | `ClaudeCliResponseProvider` | `CannedResponseProvider`
- `IChatOrchestrator` → `CachingChatOrchestrator` (classify → cache → transform → respond → log)
- Provider selection via `Completion:Provider` config key ("Claude" | "Ollama" | "ClaudeCli" | default=Canned)
- Retrieval parameters (TopK, Dimensions) configured via `RetrievalOptions`, not per-query enrichment

### Prompt Injection Defense (layered)
1. **FluentValidation endpoint filter** — 18 source-generated regex patterns, rejected at 400
2. **Threat classification** — `IThreatClassifier` → `OllamaThreatClassifier` (qwen3:4b), owned by orchestrator
3. **Canary sentinel** — PHP proxy scans SSE output with sliding window buffer, burns rate limit on detection
4. **Session threat scoring** — `X-Threat-Score` header flows between backend and PHP proxy

### Storage (PostgreSQL)
- `IInteractionRepository` — query logging, prompt caching (XxHash32 including conversation history)
- `ICorpusRepository` — corpus document and chunk storage
- `CorpusSyncService` — filesystem → PG sync, single-doc upload with inline embedding
- `CachingChatOrchestrator` — classify → cache check → transform → stream+buffer → log
- `MigrationHostedService` — EnsureCreatedAsync with retry loop

### Admin Endpoints
- `POST /api/admin/sync` — filesystem corpus sync (SSE progress)
- `POST /api/admin/corpus` — single doc upload with optional embed
- `GET /api/admin/corpus` — list documents
- `GET /api/admin/corpus/{id}` — document detail
- `GET /api/admin/interactions` — interaction history
- `POST /api/admin/ingest` — full re-ingestion to Qdrant

## Frontend Architecture

- **Layout:** 60/40 CSS grid split — resume scrolls on left, chat is full-height on right
- **Mobile:** stacks vertically, sticky "Ask about Bryan's experience" bar jumps to chat
- **Chat auto-discovery:** JS fetches `api/health.php` on load, shows/hides chat widget based on response
- **Markdown:** `marked.js` + `DOMPurify` renders bot responses
- **SSE parser:** buffers by `\n\n` delimiters, reassembles multi-line `data:` fields
- **No jQuery** — vanilla JS throughout
- **Deployed to:** `bryanboettcher.com/devel/` (chat enabled) and `bryanboettcher.com/` (chat auto-hides)

### PHP Proxy Session Model
- Session opens once at request start, closes once at request end
- No session manipulation inside streaming callbacks — accumulated values written at end
- `pushHistory()` and `burnRateLimit()` write to `$_SESSION` only, caller owns lifecycle
- History is a ring buffer of 6 exchanges stored server-side in the PHP session

## Infrastructure

- **Cluster:** 96x Ryzen 9 cores, 288GB RAM, NVMe — no GPU
- **Qdrant:** Running in k8s cluster
- **Ollama:** Running on cluster at `https://llm.mallcop.dev`
- **API:** `resume-chat.mallcop.dev`, namespace `cloud`
- **Frontend:** rsync to `angryhosting.com`

## Key Facts

- **Name:** Bryan Boettcher
- **Email:** resume@bryanboettcher.com
- **Location:** Kansas City, KS area
- **Experience:** 25+ years in software engineering
- **Current status:** Between roles (since October 2025)
- **Primary tech:** C#/.NET 9, ASP.NET Core, MassTransit, Angular 19, TypeScript, Docker, Kubernetes

## Career Timeline

1. Call-Trader — Senior/Lead Engineer (Jun 2024 – Oct 2025)
2. Taylor Summit Consulting — Software Architect/Lead (2023 – Oct 2025) — concurrent with Call-Trader
3. Kansys, Inc. — Software Architect/Lead (2020 – 2023)
4. Henry Wurst / Mittera Creative Services — Sr. Developer (2018 – 2020)
5. Service Management Group — Sr. Developer (2016 – 2018)
6. Earlier roles (2001–2016): iModules, VI Marketing, Ticket Solutions/VeriShip, Softek Solutions, Cities Unlimited

## Resume Tooling

- **Format:** Typst (`.typ` files)
- **Compile:** `typst compile resume.typ` → produces `resume.pdf`
- **Watch mode:** `typst watch resume.typ` for live reload

## Conventions

### Content
- The evidence docs are the RAG corpus — keep them verbose, self-contained, and accurate
- The link registries are fragile to reconstruct — verify URLs still work before citing them
- The resume PDF should be concise (2 pages max) but the evidence docs backing it are intentionally detailed
- Do NOT be sycophantic — accuracy over flattery. His livelihood depends on this document.

### Backend Code
- Options pattern with `ValidateOnStart()` for all config sections
- Extension methods for DI registration (`AddXxxServices()` pattern)
- No `ConfigureAwait(false)` — ASP.NET Core has no SynchronizationContext
- Endpoint organization in `Endpoints/` with static `MapTo(WebApplication)` pattern
- `IAsyncEnumerable<string>` for streaming responses
- `ValueTask` for synchronous interface implementations (e.g. `IQueryEnricher`)
- Proper `CancellationToken` propagation throughout
- Tests: NUnit + Shouldly + NSubstitute, nested When_* scenario classes

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
    ResumeChat.Rag/          — RAG library (embedding, retrieval, completion abstractions)
      ICompletionProvider.cs — IAsyncEnumerable<string> CompleteAsync(prompt, ct)
      HardcodedCompletionProvider.cs — Demo impl, splits prompt by words with 50ms delay
  tests/
    ResumeChat.Api.Tests/    — Integration tests with WebApplicationFactory
      ApiFactory.cs          — Test factory, overrides DI with InstantCompletionProvider
      ChatApiTests.cs        — 8 tests: health, auth, startup validation, SSE streaming
    ResumeChat.Rag.Tests/    — Unit tests for providers
      HardcodedCompletionProviderTests.cs — 5 tests: streaming, edge cases, cancellation

frontend/
  api/
    chat.php                 — PHP proxy with session-based rate limiting
    config.example.php       — Config template for API key/URL

Dockerfile                   — Multi-stage: SDK build + test → ASP.NET runtime, port 5000
```

## Backend Architecture

- **Target:** net10.0, nullable enabled, implicit usings
- **Image:** `ghcr.io/bryanboettcher/resume-chat:latest`
- **Dev ports:** HTTP 5014, HTTPS 7036
- **Streaming:** SSE format (`data: {chunk}\n\n`, terminated by `data: [DONE]\n\n`)
- **Auth:** API key via `X-Api-Key` header, middleware-level
- **Rate limiting:** .NET built-in RateLimiter, "chat" policy, configurable via options
- **Tests run in Docker build** — image fails if tests fail

### Existing Abstractions
- `ICompletionProvider` — streaming completion interface
- `HardcodedCompletionProvider` — test/demo implementation (registered as singleton)

### Not Yet Built (Phase 2)
- Embedding provider (Ollama HTTP API for `nomic-embed-text` or `mxbai-embed-large`)
- Vector store client (Qdrant)
- Corpus ingestion pipeline (markdown → chunks → embeddings → Qdrant)
- Retrieval provider (embed query → Qdrant search → ranked context)
- Real completion providers: `OllamaCompletionProvider` (dev), `ClaudeCompletionProvider` (prod)
- System prompt with grounding in retrieved context

## Infrastructure

- **Cluster:** 96x Ryzen 9 cores, 288GB RAM, NVMe — no GPU
- **Qdrant:** To be deployed to k8s cluster (separate session)
- **Ollama:** Already running on cluster for HomeAssistant embeddings

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

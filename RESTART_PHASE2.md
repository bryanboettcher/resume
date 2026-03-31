# Resume Chat — Phase 2: Embeddings & RAG Pipeline

## Current State
- Static SPA live at `bryanboettcher.com/devel/`
- C# minimal API live at `resume-chat.mallcop.dev` with `HardcodedCompletionProvider`
- PHP proxy wired up with session-based rate limiting
- Chat widget appears and echoes input back via SSE streaming
- Image at `ghcr.io/bryanboettcher/resume-chat:latest`
- All code on `chatbot` branch, pushed to `nas` remote

## Architecture (see ARCHITECTURE.md)
- Qdrant for vector storage (deployed to k8s cluster — separate session)
- Ollama for embeddings (already on cluster for HomeAssistant, `nomic-embed-text` or `mxbai-embed-large`)
- Claude API for production completion, Ollama for dev completion
- `ICompletionProvider` abstraction already exists with pluggable implementations

## Phase 2 Work Items

### 1. Corpus Ingestion Pipeline
- CLI tool or startup job in `ResumeChat.Rag`
- Reads markdown files from `evidence/`, `projects/`, `links/`
- Chunks documents appropriately (by section? fixed size with overlap?)
- Embeds chunks via Ollama HTTP API
- Loads vectors + metadata into Qdrant
- Re-runnable: build new collection, swap pointer

### 2. Retrieval Pipeline
- `IRetrievalProvider` or similar abstraction
- Embeds user query via Ollama
- Searches Qdrant for top-K similar chunks
- Returns ranked context for the completion prompt

### 3. Completion Providers
- `OllamaCompletionProvider` — for dev/testing (slow on CPU but free)
- `ClaudeCompletionProvider` — for production (Anthropic API)
- System prompt that grounds answers in retrieved context
- Streaming response via `IAsyncEnumerable<string>`

### 4. Wire It Together
- Chat endpoint receives question → embed → retrieve → augment prompt → complete → stream
- Update Docker image and push
- Separate session handles Qdrant deployment and config updates on cluster

## Key Files
- `backend/src/ResumeChat.Rag/` — RAG library (embedding, retrieval, completion)
- `backend/src/ResumeChat.Api/` — HTTP surface
- `evidence/`, `projects/`, `links/` — corpus source material
- `ARCHITECTURE.md` — full architecture decisions

## Conventions (from kb-platform patterns)
- Options pattern with `ValidateOnStart()` for all new config sections
- Extension methods for DI registration (`AddQdrantServices()`, `AddOllamaServices()`, etc.)
- `ConfigureAwait(false)` on all awaits
- Endpoint organization in `Endpoints/` with `MapTo()` pattern

## Hardware Available
- 96x Ryzen 9 cores, 288GB RAM, NVMe — embedding is fast on CPU
- Ollama already running on the cluster
- No GPU available

# Resume Website — Architecture

## Overview

A web resume with an embedded RAG chatbot that answers questions about Bryan's experience using real examples from the evidence corpus. Designed as a portfolio piece demonstrating RAG pipeline construction, pluggable architecture, and C#/.NET expertise.

## Components

### 1. Static SPA (Commercial Host)

- Scrolling single-page resume site ("stuff slides in sideways" style)
- Hosted on existing Ubuntu/PHP static host, deployed via rsync
- Contains a thin PHP proxy that forwards chat API requests to the cluster backend
  - Eliminates CORS — browser only talks to same origin
  - Returns graceful "chat unavailable" when cluster is down (progressive enhancement)
- Chat widget mounts only if the backend health check succeeds

### 2. C# Minimal API (K8s Cluster)

- ASP.NET Core minimal API — serves only the RAG chat endpoint, no SPA concerns
- Streaming REST responses (not SignalR) for token-by-token chat output
- Pluggable completion via `ICompletionProvider`:
  - `OllamaCompletionProvider` — local model for development (slow on CPU, but free)
  - `ClaudeCompletionProvider` — Anthropic API for production (quality + speed)
- Provider selection via configuration, intentionally a design showcase
- Deployed as a container image with a Helm chart for ArgoCD

### 3. Qdrant (K8s Cluster)

- Purpose-built vector database for the embedding store
- Chosen over pgvector to learn the technology and have a richer interview story
- Deployed alongside the API via the same Helm chart
- Corpus is small enough that a full re-index on embedding model change is cheap

### 4. Embeddings (Ollama on Cluster)

- Embedding model runs on Ollama, already present on the cluster for HomeAssistant
- Candidate models: `nomic-embed-text` (~270MB), `mxbai-embed-large` for higher quality
- CPU embedding is fast (single forward pass, no autoregressive generation)
  - Full corpus: seconds; single query: sub-100ms
  - 96 Ryzen 9 cores / 288GB RAM / NVMe — more than sufficient
- Same model used for both ingestion and query-time embedding

### 5. Corpus Ingestion Pipeline

- CLI tool or startup job that:
  1. Reads evidence/projects/links markdown files
  2. Chunks them appropriately
  3. Embeds chunks via Ollama
  4. Loads vectors + metadata into Qdrant
- Re-runnable: "build new collection and swap pointer" approach
- Corpus will continue to grow as more project narratives are extracted

## Architecture Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Chat transport | Streaming REST | Simpler than SignalR; chatbot is request-response with streaming, not bidirectional |
| SPA hosting | Separate static host | Progressive enhancement — resume works even if cluster is down |
| CORS strategy | PHP proxy on static host | Browser never crosses origins; proxy handles fallback gracefully |
| Vector DB | Qdrant | Lightweight, Rust-based, good C# SDK, runs well in containers, better interview story than pgvector |
| Embeddings | Ollama (local, CPU) | Free, fast for embedding workloads, already running on cluster |
| LLM completion | Pluggable (Ollama dev / Claude prod) | Develop for free, ship with quality; pluggability is intentional portfolio piece |
| Embedding lock-in | None — re-embed on model change | Corpus is small, full re-index is cheap, build-and-swap-pointer |
| Deployment artifact | Helm chart + container image | Matches existing ArgoCD/Helm cluster workflow; separate session handles deployment |

## Phased Delivery

1. **Phase 1:** Static SPA resume site on commercial host
2. **Phase 2:** C# minimal API + Qdrant + ingestion pipeline, containerized
3. **Phase 3:** Chat widget in SPA with PHP proxy, progressive enhancement
4. **Phase 4:** Helm chart for cluster deployment, ArgoCD integration (separate session)

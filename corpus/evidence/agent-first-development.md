---
title: Agent-First Development
tags: [ai, agentic, claude-code, mcp, automation, api-integration, typescript, debugging, homelab]
related:
  - evidence/ai-driven-development.md
  - evidence/infrastructure-devops.md
  - evidence/distributed-systems-architecture.md
  - projects/homelab-infrastructure.md
category: evidence
contact: resume@bryanboettcher.com
---

# Agent-First Development — Evidence Portfolio

## Philosophy

Agent-first development is not "asking AI to write code." It is directing a multi-step engineering process where the human provides intent, constraints, and judgment while AI agents handle research, implementation, and systematic debugging. Bryan's workflow treats agent sessions as engineering engagements: the agent receives a problem statement, investigates the solution space, proposes an approach, builds it, tests it against real systems, and iterates on failures — all within a single directed session.

---

## Evidence: MCP Server for Homarr Dashboard (Single-Session Build)

**Repository:** https://github.com/bryanboettcher/mcp-homarr
**Session date:** 2026-04-01
**Result:** A 65-tool MCP server for the Homarr v1.x dashboard, tested against a live Kubernetes-hosted instance

Bryan directed an agent to build a Model Context Protocol (MCP) server for Homarr — a dashboard application for homelab services. No MCP server for Homarr existed. The entire project — from initial research through tested, deployed code — was completed in a single agent session.

### What Was Built

A TypeScript MCP server providing 65 tools across 12 management domains:

| Domain | Read | Write | Tools |
|--------|------|-------|-------|
| Boards | list, get, search, permissions | create, duplicate, rename, delete, save layout, save settings, visibility | 15 |
| Apps | list, search, get | create, bulk create, update, delete | 7 |
| Users | list, get, search | create, delete | 5 |
| Integrations | list, get, search | create, update, delete | 6 |
| Groups | list, get | create, delete, add/remove members | 6 |
| Docker | list containers | start, stop, restart, remove | 5 |
| Kubernetes | cluster info, nodes, pods, services, namespaces, resource counts | — | 6 |
| Invites | list | create, delete | 3 |
| Icons | search | — | 1 |
| Cron Jobs | list | trigger | 2 |
| API Keys | list | create, delete | 3 |
| System | info, stats, settings, updates, media, search engines, city search | — | 7 |

The server supports two authentication modes: API key (preferred, works through reverse proxies) and session cookie (fallback, direct access only).

### The Engineering Process

The session progressed through distinct phases, each demonstrating a different aspect of agent-first development:

#### Phase 1: Research (API Discovery)

Bryan's initial prompt was simply: "can you build an MCP server to manage Homarr?" The agent:
- Researched the Homarr API surface via web search and GitHub source analysis
- Identified the dual tRPC/REST API architecture
- Mapped authentication flows, permission levels, and endpoint signatures
- Delivered a comprehensive API reference covering 50+ endpoints across 15 routers

**What this demonstrates:** The agent performed the research that would normally require reading documentation, cloning repos, and tracing code paths — compressed into minutes rather than hours.

#### Phase 2: Initial Implementation (Wrong Version)

The agent built a complete MCP server targeting Homarr v0.16 (`ajnart/homarr`), including a Docker test container. The implementation passed 18/18 tests against the local test instance.

**The discovery:** Bryan's production Homarr instance was actually v1.x (`homarr-labs/homarr`) — a complete rewrite with an entirely different API surface. Every REST endpoint from v0.16 was gone. The tRPC router names, procedure signatures, and authentication cookie format had all changed.

**What this demonstrates:** Agent-first development handles version mismatches the same way a human engineer would — by discovering the discrepancy, researching the correct API, and rewriting. The agent cloned the correct source repository, analyzed all 23 tRPC routers, and rewrote the entire MCP server against the v1.x API. The rewrite passed 17/18 endpoints against the live production instance (the one failure was Kubernetes integration not being enabled in the Homarr deployment — an infrastructure configuration, not a code bug).

#### Phase 3: Authentication Debugging (Three-Layer Problem)

Connecting to the production instance revealed a layered authentication problem:

1. **Reverse proxy interception:** The external URL was behind an Authelia authentication proxy that intercepted all requests before they reached Homarr — including the authentication endpoints themselves.

2. **Cookie format mismatch:** Homarr v1.x uses Auth.js v5 (`authjs.session-token`) instead of NextAuth v4 (`next-auth.session-token`). The client needed to handle both.

3. **Credential field naming:** Homarr uses `name` (not `username`) for the login credential field — discovered by tracing the actual HTTP responses.

The agent systematically diagnosed each layer: tested via port-forward to isolate the proxy issue, traced Set-Cookie headers to identify the cookie format, and decoded URL-encoded error messages to find the field name mismatch. Each fix was verified before moving to the next layer.

**Resolution:** The agent discovered that Homarr v1.x supports API key authentication via an `ApiKey` header, which bypasses both the session cookie flow and the proxy authentication issue. It created an API key through the session-authenticated port-forward, added dual-auth support to the client (API key preferred, session cookie fallback), and verified end-to-end functionality.

**What this demonstrates:** Real-world integration debugging requires understanding of reverse proxy auth chains, cookie mechanics, and API authentication patterns. The agent traced through each layer systematically rather than guessing.

#### Phase 4: Port Collision Discovery

After all code was correct and verified, the other Claude Code instance consuming the MCP server still got 404 errors on every tRPC route. The code was confirmed correct. The API key was valid (no more 401s). The routes existed and worked when tested directly.

The root cause: the v0.16 test Docker container from Phase 2 was still running on port 7575, shadowing the kubectl port-forward to the real v1.x instance. All MCP requests were hitting the wrong Homarr entirely.

**What this demonstrates:** Agent-first development doesn't prevent environmental issues — but systematic debugging (verifying each assumption independently) surfaces them quickly. The fix was a single `docker stop`.

#### Phase 5: Write Operations

After the other agent reported the MCP was "read-only," the agent analyzed Homarr's Zod validation schemas from source to determine exact field names, types, constraints, and required vs. optional fields. It then added create/update mutations for apps, boards, users, integrations, and groups — expanding from a read-only tool set to full CRUD coverage. All write operations were tested against the live instance.

### Session Characteristics

| Metric | Value |
|--------|-------|
| Total tools implemented | 65 |
| Homarr versions handled | 2 (v0.16 initial, v1.x rewrite) |
| Auth mechanisms implemented | 3 (session cookie v0.16, session cookie v1.x, API key) |
| Authentication bugs found and fixed | 4 (proxy interception, cookie name, credential field, port collision) |
| Test passes against live instance | 17/18 read + 9/9 write |
| Lines of TypeScript | ~700 (client + server) |

---

## What Agent-First Development Is Not

This session was not "generate code and hope it works." It involved:

- **Real system testing:** Every implementation was verified against a live Homarr instance, not mocked
- **Multi-layer debugging:** Authentication issues required understanding of reverse proxy chains, cookie mechanics, and API evolution across major versions
- **Iterative refinement:** The v0.16 → v1.x rewrite was a complete pivot based on production reality, not a patch
- **Schema-accurate implementation:** Write operations were built from source-level analysis of Zod validation schemas, not documentation that might be stale

The human's role was direction and judgment: deciding to build the MCP server, pointing at the correct production instance, recognizing when the test container was the problem, and requesting write operations. The agent handled research, implementation, debugging, and testing.

---

## Relationship to Other Evidence

This session builds on patterns documented elsewhere in the evidence corpus:

- **Homelab Infrastructure (infrastructure-devops.md):** The Homarr instance runs on Bryan's 3-node Talos Kubernetes cluster. The port-forward, Authelia proxy, and service mesh are all part of the same infrastructure described in that evidence.
- **AI-Driven Development (ai-driven-development.md):** This is a concrete single-session example of the multi-agent development patterns described there. The MCP server was built by one agent, consumed by another, and the interaction between them surfaced the port collision bug.
- **Distributed Systems (distributed-systems-architecture.md):** Understanding tRPC procedure routing, session cookie mechanics, and reverse proxy authentication chains draws on the same distributed systems thinking applied to MassTransit sagas and Kubernetes CSI debugging.

---

## Summary

This single session demonstrates agent-first development as a practical engineering methodology:

1. **Research compression:** Hours of API documentation review compressed into minutes of directed agent research
2. **Rapid prototyping with real testing:** A complete MCP server built, tested, and verified against a live system
3. **Systematic debugging:** Multi-layer authentication issues resolved through methodical isolation and verification
4. **Adaptive implementation:** A full rewrite when the target API version was wrong — not a patch, a correct response to new information
5. **Production-quality output:** 65 tools with dual auth support, deployed and consumed by another agent in the same homelab infrastructure

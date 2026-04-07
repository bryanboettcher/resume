---
title: DST Dedicated Server Supervisor & Web Dashboard
tags: [go, docker, kubernetes, process-management, web-ui, sse, udp, health-checks, containers, game-servers, open-source, prometheus]
related:
  - evidence/infrastructure-devops.md
  - projects/homelab-infrastructure.md
  - evidence/open-source-contributions.md
  - links/github-repos.md
category: project
contact: resume@bryanboettcher.com
---

# DST Dedicated Server — Project Narrative

## Context

Bryan maintains a fork of the Don't Starve Together dedicated server Docker image. The original project (mathielo/dst-dedicated-server, widely used in the DST community) ran the game binary directly as PID 1 with shell scripts — no process supervision, no health checking, no graceful shutdown, and no management interface. Bryan rewrote the container architecture to be production-grade for Kubernetes while preserving the newbie-friendly "clone, paste token, docker compose up" experience.

## What Was Built

### Go Process Supervisor

A single static Go binary (~5MB) that replaces the shell entrypoint chain and acts as PID 1:

- **Process lifecycle management:** Orchestrates prepare → install (steamcmd) → run phases. Manages the DST binary as a child process with a retained stdin pipe for console commands.
- **Graceful shutdown:** On SIGTERM, sends `c_save()` to DST via stdin, waits for save to complete, then sends `c_shutdown()`. Falls back to SIGKILL after a configurable timeout. Kubernetes pod termination triggers proper world saves instead of data loss.
- **Stdout/stderr isolation:** Two independent goroutines drain DST's stdout and stderr through OS pipes. The primary destination (os.Stdout for `kubectl logs`) is written first, then a secondary LogBuffer. DST's output path is never blocked by the supervisor's log processing. The LogBuffer handles partial lines across write boundaries with a 16KB cap to protect against misbehaving mods.
- **Observer pattern:** A log observer watches DST's stdout for runtime state announcements (port bindings, server readiness, player join/leave events). This drives state machine transitions — the supervisor doesn't hardcode ports or parse config files for runtime behavior, it observes what DST actually reports.
- **Player tracking:** An in-memory map of connected players keyed by Klei User ID, maintained by the observer (join/leave events) and periodic `c_listplayers()` console polls. Stale entries age out after missing multiple poll cycles.
- **State machine:** `PREPARING → INSTALLING → STARTING → RUNNING → STOPPING → STOPPED`. Readiness transitions are observer-driven (DST announces "Server registered via geo DNS"), not timer-based.

### HTTP Health & Management API

Exposed on port 8080 inside the container, designed for both Kubernetes probes and direct management:

- **Health probes:** `/healthz` (liveness), `/readyz` (readiness, gated on DST actually accepting connections), `/startupz` (DST binary launched)
- **Status:** JSON endpoint with state, player count, player list with names, game port, region, uptime, cluster/shard identity, is_master flag
- **Prometheus metrics:** Text exposition format with state gauges, player count, uptime, server info labels
- **Management API:** Save, shutdown, restart (without container restart), rollback, arbitrary console commands — all token-gated via bearer auth
- **Log streaming:** Ring buffer of last 1000 lines with SSE stream for live tailing

### Web Management Dashboard

A separate sidecar container (~5MB, distroless base) serving an embedded SPA:

- **Multi-shard awareness:** Discovers master/slave topology from supervisor status responses. Cluster-wide commands (save, shutdown, rollback, regenerate) route to the master shard. Announcements fan out to all shards.
- **Live updates:** SSE stream aggregates status from all configured supervisor backends every 5 seconds. Per-shard log viewer with SSE live tailing and shard selector.
- **Reverse proxy:** All API calls proxy through the webui to the supervisor backends. Only the webui port needs to be exposed.
- **Zero build step:** Vanilla HTML/CSS/JS with no framework, no node_modules, no build tooling. Embedded in the Go binary via `go:embed`.

## Technical Decisions

### Why Go

Single static binary with zero runtime dependencies. The supervisor compiles in the Docker build stage and gets COPY'd into the steamcmd base image, adding ~5MB to an already ~1GB image. Go's `net/http` stdlib handles all HTTP without a framework. Go's `exec.Command` with `SysProcAttr.Credential` handles privilege dropping without `su`. Go's `os/signal` handles SIGTERM cleanly.

### Why Not s6-overlay / supervisord

These are process supervisors designed for multi-process containers. DST is a single process. The overhead of s6's execution model (longrun services, notification-fd, readiness protocol) doesn't match the problem. The Go supervisor is 11 source files, zero external dependencies, and does exactly what's needed.

### Observer Over Config Parsing

The supervisor originally tried to parse `server.ini` and `cluster.ini` for port numbers to configure health probes. This was fragile — DST has multiple ports (game, auth, master server, query) and the query port convention (`game_port + 1`) turned out to be wrong. Instead, the observer watches DST's stdout for `ServerPort: 10999` and `SteamMasterServerPort: 27016` lines, using the actual reported values. This also catches the "Server registered via geo DNS" line as the definitive readiness signal, which is more reliable than any external probe.

### Zombie Reaper: Intentionally Absent

The supervisor runs as PID 1 and inherits orphaned processes. The standard approach is a `Wait4(-1)` reap loop, but this fundamentally races with Go's internal `waitid()` calls in `cmd.Wait()`. After two iterations of trying to make them coexist (PID tracking, WNOHANG polling, SIGCHLD notification), the correct answer was: DST doesn't fork, shell scripts are waited on synchronously, there are no orphans to reap. The reaper was removed entirely.

### Log Pipe Architecture

DST's stdout/stderr must never be blocked by the supervisor — `kubectl logs` is the source of truth. Instead of `io.MultiWriter` (which couples the game binary to our log processing), each stream gets its own goroutine draining an OS pipe. The goroutine writes to os.Stdout first, then to a `PrefixWriter` (which adds `[stdout]`/`[stderr]` tags and handles partial line buffering). Each PrefixWriter owns its own partial buffer — no shared mutable state between the two goroutines.

## GC Pressure Review

The hot path is `LogBuffer.Write()` — called on every chunk of DST output. After review:
- Replaced `string(p)` + `strings.Split()` with `bytes.IndexByte` loop (eliminated full-buffer copy + `[]string` allocation per write)
- A2S parser's `readString()` pre-sized to 64 bytes, `skipString()` added for discarded fields (zero allocation)
- SSE log streaming uses three `io.WriteString` calls instead of string concatenation

## Backwards Compatibility

The default `docker-compose.yml` preserves the original newbie experience: clone the repo, paste a Klei token into `cluster_config/cluster_token.txt`, run `docker compose up`. The supervisor is invisible to the user — it just makes the server work better. Health endpoints, the web UI, and management API are free additions that don't change the basic workflow.

## Significance for Resume

- **Go systems programming:** Process supervision, signal handling, UDP protocol implementation, pipe management, goroutine architecture
- **Container architecture:** Multi-stage Docker builds, OCI image annotations, Docker healthchecks, Kubernetes probe design
- **Real-time web:** SSE streaming for both status updates and log tailing, reverse proxy aggregation across multiple backends
- **Protocol implementation:** Hand-rolled A2S_INFO UDP query parser with challenge-response (standard Valve/Steam protocol)
- **Production debugging:** Zombie reaper race condition with Go runtime, DST query port discovery, steamcmd HOME directory issues — all found and fixed through live testing
- **Open source stewardship:** Maintaining a fork used by the DST community, preserving backwards compatibility while adding significant infrastructure

---
title: Infrastructure, DevOps & Kubernetes
tags: [kubernetes, docker, talos, gitops, argocd, ci-cd, linstor, drbd, traefik, metallb, github-actions, aspire, opentelemetry, cross-compilation]
related:
  - projects/homelab-infrastructure.md
  - projects/call-trader-madera.md
  - projects/wyoming-rust.md
  - projects/dst-dedicated-server.md
  - projects/cloud-orca-slicer.md
  - evidence/distributed-systems-architecture.md
  - evidence/open-source-contributions.md
  - evidence/agent-first-development.md
  - evidence/cloud-azure-experience.md
  - evidence/hardware-embedded-systems.md
category: evidence
contact: resume@bryanboettcher.com
---

# Infrastructure & DevOps — Evidence Portfolio

## Philosophy

Bryan operates infrastructure the same way he writes application code: with explicit architecture, version-controlled configuration, and automated workflows. His homelab is not a toy — it's a 3-node Kubernetes cluster with enterprise-grade storage replication, GitOps deployment, and automated management via AI agents. His professional work includes Docker containerization and CI/CD pipeline design for production deployments.

---

## Evidence: Homelab — Production Kubernetes Cluster

**Local path:** ~/src/bryanboettcher/homelab/
**Repository:** Private GitOps repository managed via ArgoCD

### Hardware
- **Nodes:** 3x Minisforum MS-A2 (AMD Ryzen 9 7945HX)
- **Total compute:** 48 cores / 96 threads, 288 GB RAM
- **Storage per node:** Samsung 990 PRO 2TB (performance) + Samsung PM953 2TB (endurance)
- **NAS:** 60 TB ZFS pool (network-attached)
- **Network:** 2x 10GbE bonded per node, nodes at 10.13.1.11–13

### Software Stack
- **OS:** Talos Linux (immutable, API-driven — no SSH, no shell, managed entirely via `talosctl`)
- **Orchestration:** Kubernetes (vanilla, not k3s)
- **Storage:** Piraeus/LINSTOR with DRBD synchronous replication
- **GitOps:** ArgoCD for declarative deployment from Git
- **Ingress:** Traefik with automatic TLS via cert-manager (Let's Encrypt)
- **Load balancing:** MetalLB for bare-metal LoadBalancer IP allocation
- **Authentication:** Authelia for SSO/2FA across services

### Storage Architecture (4 tiers)
Bryan designed a tiered storage architecture matching workload characteristics to hardware:

| Tier | Purpose | Hardware | Replication |
|------|---------|----------|-------------|
| `local-path` | Ephemeral / scratch | OS drive | None |
| `endurance` | Write-heavy (Frigate NVR, downloads) | PM953 enterprise SSD | LINSTOR |
| `performance` | Critical HA (databases, game servers) | 990 PRO consumer SSD | LINSTOR synchronous |
| `general-ha` | Configs, media staging | NFS from ZFS NAS | ZFS RAID |

This demonstrates understanding of storage I/O patterns, SSD endurance characteristics (enterprise MLC vs consumer TLC), and the tradeoffs between replication strategies.

### Services Running
- **Home Automation:** Home Assistant, Frigate NVR (real-time object detection), Wyoming voice satellites
- **Media:** Plex, Sonarr, Radarr, Lidarr, Prowlarr, SABnzbd, Overseerr
- **Infrastructure:** Traefik, cert-manager, MetalLB, ArgoCD, Authelia
- **Game Servers:** Don't Starve Together, Valheim (both Dockerized)

### Operational Model
The cluster is managed through AI agent delegation:
- Changes are proposed and reviewed in the main conversation
- Specialized agents (homelab-manager, platform-executor) handle investigation and execution
- git-workflow-manager commits changes to the GitOps repository
- ArgoCD detects changes and reconciles cluster state automatically

---

## Evidence: LINSTOR-CSI Contribution

**Issue:** https://github.com/piraeusdatastore/linstor-csi/issues/410
**PR:** https://github.com/piraeusdatastore/linstor-csi/pull/411 (Merged February 2026)

Bryan's LINSTOR-CSI contribution came directly from operating this storage infrastructure. He discovered the PVC termination deadlock because his cluster has dedicated storage-only satellite nodes (a non-trivial LINSTOR topology), and the CSI plugin didn't account for this configuration. The bug fix demonstrates:
- Understanding of Kubernetes CSI specification (VolumeAttachments, PV finalizers, external-attacher controller)
- LINSTOR concepts (satellites, diskless resources, tie-breaker resources, auxiliary properties)
- The ability to trace a distributed systems bug across multiple Kubernetes controllers and the storage plugin

---

## Evidence: Madera/Call-Trader — CI/CD & Containerization

**Repository:** github.com/Call-Trader/madera-apps (private)

### Docker
- Multi-stage Docker builds for both backend (.NET 9) and frontend (Angular 19 + nginx)
- Runtime environment injection for frontend containers (configuration without rebuild)
- Container memory budgets: 2 GB limit with 650 MB typical / 900 MB peak for web app

### CI/CD Pipelines
- **8 GitHub Actions workflow files** covering dev/latest/prod environments for both the importer service and the main server
- Automated builds pushing to GitHub Container Registry (ghcr.io)
- Environment-specific deployment configurations

### Local Development — .NET Aspire with Custom Resources
Bryan extended .NET Aspire beyond standard usage by authoring custom Aspire resource types:

#### Custom Resource: `FileStore`
A shared bind-mount abstraction for MassTransit MessageData. The `FileStore` resource exposes `AddFileStore()` and `WithBindMount()` extension methods (overloaded for both `ContainerResource` and `ProjectResource`), enabling a single declarative statement to wire up shared filesystem paths across all services that need large message payload storage.

#### Custom Resource: `GroupResource`
A visual grouping abstraction for the Aspire dashboard. Implements parent/child relationships with `ResourceNotificationService` eventing — when a parent GroupResource starts, child resources are notified and displayed hierarchically in the dashboard. This is non-trivial Aspire extension work using the `ResourceReadyEvent` subscription pattern.

#### AppHost Orchestration (8+ Services)
The Aspire AppHost orchestrates 8+ services simultaneously:
- 4 instances of `Madera.Workflows` (one per dataflow: Convoso, Dispos, DirectMail, Ringba)
- API server, Angular frontend (via `AddNpmApp` + `PublishAsDockerFile()`)
- RabbitMQ (with management plugin and persistent data volumes), MongoDB with MongoExpress
- Database migration runner with `WaitForCompletion()` ordering
- ServiceDefaults project with OpenTelemetry (traces, metrics, logs), health checks (/health, /alive), HTTP resilience handlers, and service discovery

#### Multi-Tenant Single-Binary Deployment
The same `Madera.Workflows` project is deployed as 4 separate Aspire resources, each differentiated only by the `Madera__Dataflow` environment variable (an enum value). This single-binary multi-tenant pattern eliminates per-dataflow build artifacts while maintaining runtime isolation — each instance processes only its designated data source.

### OpenTelemetry Custom Instrumentation
**Branch:** `178-otel-logging`

Bryan implemented custom OpenTelemetry instrumentation beyond auto-instrumentation defaults:
- **Custom `ActivitySource`** ("Madera.Workflows") for application-specific distributed traces
- **Custom `Meter` and `Counter<long>`** ("workflow_startup_total") using the modern `System.Diagnostics.Metrics` API
- **Structured logging:** `AddOpenTelemetry()` with `IncludeFormattedMessage` and `IncludeScopes` for correlation
- **Self-hosted OTLP collector** on Azure Container Apps for centralized trace/metric/log collection
- **MassTransit log enrichment** (via `Serilog.Enrichers.MassTransit`) correlating saga IDs and message context to structured log entries — critical for debugging distributed state machine workflows

---

## Evidence: Game Server Infrastructure

### Valheim Server Docker
**Repository:** https://github.com/bryanboettcher/valheim-server-docker (fork of lloesche/valheim-server-docker, 2,187 stars)

Contributed Docker Secrets support (PR #748, merged). Maintains a personal fork for custom configuration. The server runs on the homelab Kubernetes cluster.

### Don't Starve Together Server — Supervisor & Web Management
**Repository:** https://github.com/bryanboettcher/dst-dedicated-server (fork, significantly extended)

Rewrote the container architecture from shell scripts to a Go process supervisor:
- **Go supervisor as PID 1:** Manages DST binary lifecycle, graceful shutdown (c_save → c_shutdown → SIGKILL), stdin pipe for console commands
- **HTTP health/management API:** Kubernetes liveness/readiness/startup probes, Prometheus metrics, save/shutdown/restart/rollback endpoints, live log streaming via SSE
- **Web dashboard sidecar:** Multi-shard-aware management UI with live status, player tracking, log viewer, console. Reverse proxies to supervisor backends. Distroless container, ~5MB.
- **Observer pattern:** Watches DST stdout for runtime state (port bindings, readiness signals, player events) instead of parsing config files. Drives state machine transitions.
- **Player tracking:** In-memory map keyed by Klei User ID, maintained by join/leave event observation + periodic c_listplayers() polls with age-out
- **Log pipe isolation:** Independent goroutines per stream, os.Stdout written first (source of truth for kubectl logs), LogBuffer second. DST's output path is never blocked by supervisor log processing.
- **Zero external Go dependencies:** stdlib only for HTTP, A2S protocol, SSE, process management
- **Backwards compatible:** Same clone-paste-run experience for newbies, supervisor is invisible

---

## Evidence: Wyoming-Rust — Cross-Compilation & Embedded Deployment

**Repository:** https://github.com/bryanboettcher/wyoming-rust
**Local path:** ~/src/bryanboettcher/wyoming-rust/

The Wyoming-Rust project demonstrates cross-compilation and embedded deployment practices:
- **Target:** ARM `arm-unknown-linux-gnueabihf` (Raspberry Pi Zero W — ARMv6, 512 MB RAM)
- **Build tool:** `cross` for containerized cross-compilation
- **Docker:** Multi-arch builds (ARMv7 + ARM64) with automated publishing
- **Deployment:** Runs as a service on resource-constrained hardware connecting to the Home Assistant instance on the Kubernetes cluster

---

## Evidence: Cloud-Orca — Docker Compose Orchestration

**Local path:** ~/src/bryanboettcher/cloud-orca/

Web-based 3D printer slicer with Docker Compose orchestration:
- Backend (ASP.NET minimal API) and frontend (Angular 19) in separate containers
- CuraEngine built from source inside the container (~15–30 min initial build)
- Docker Compose for local development with service discovery

---

## Summary

Bryan's infrastructure experience spans:
- **Kubernetes:** Full cluster lifecycle — Talos provisioning, storage (LINSTOR/DRBD), networking (MetalLB, Traefik), GitOps (ArgoCD), monitoring
- **Storage architecture:** Tiered design matching hardware characteristics to workload I/O patterns
- **Docker:** Multi-stage builds, secrets management, multi-arch builds, compose orchestration
- **CI/CD:** GitHub Actions pipelines with environment promotion (dev → latest → prod)
- **Cross-compilation:** Rust targeting embedded ARM devices
- **Operational debugging:** Tracing distributed bugs across Kubernetes controllers, CSI plugins, and storage layers

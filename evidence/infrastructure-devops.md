---
skill: Infrastructure, DevOps & Kubernetes
tags: [kubernetes, docker, k8s, talos, GitOps, ArgoCD, CI/CD, storage, networking, homelab]
relevance: Demonstrates hands-on production Kubernetes operation, storage architecture, CI/CD pipeline design, and infrastructure-as-code practices
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

### Local Development
- .NET Aspire orchestration for spinning up the full service mesh locally (API server, RabbitMQ, SQL Server, Angular dev server)

---

## Evidence: Game Server Infrastructure

### Valheim Server Docker
**Repository:** https://github.com/bryanboettcher/valheim-server-docker (fork of lloesche/valheim-server-docker, 2,187 stars)

Contributed Docker Secrets support (PR #748, merged). Maintains a personal fork for custom configuration. The server runs on the homelab Kubernetes cluster.

### Don't Starve Together Server
**Repository:** https://github.com/bryanboettcher/dst-dedicated-server (fork)

Docker-containerized DST multiplayer server with:
- Multi-server cluster coordination
- Automated backup/restore
- Configuration management via YAML
- Mod installation support

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

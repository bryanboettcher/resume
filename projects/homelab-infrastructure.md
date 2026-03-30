---
project: Homelab Kubernetes Cluster
company: Personal
dates: 2024 – present
role: Architect / Operator
tags: [kubernetes, talos, linstor, argocd, gitops, docker, storage, networking]
---

# Homelab Infrastructure — Project Narrative

## Context

Bryan operates a 3-node Kubernetes cluster as personal infrastructure for home automation, media services, game servers, and development environments. The cluster is designed and operated with production-grade practices: immutable OS, GitOps deployment, replicated storage, and automated management.

## Hardware

| Component | Specification |
|-----------|--------------|
| Nodes | 3x Minisforum MS-A2 |
| CPU | AMD Ryzen 9 7945HX (16C/32T per node, 48C/96T total) |
| RAM | 96 GB per node (288 GB total) |
| Performance SSD | Samsung 990 PRO 2TB per node |
| Endurance SSD | Samsung PM953 2TB per node |
| NAS | 60 TB ZFS pool |
| Network | 2x 10GbE bonded per node |

## Software Architecture

- **Talos Linux:** Immutable, API-driven Kubernetes OS. No SSH, no shell — managed entirely via `talosctl` API. Forces infrastructure-as-code practices.
- **Piraeus/LINSTOR:** Block-level storage replication via DRBD. Synchronous replication for HA workloads.
- **ArgoCD:** GitOps — cluster state defined in Git, ArgoCD reconciles automatically.
- **Traefik:** Ingress controller with automatic TLS via cert-manager (Let's Encrypt).
- **MetalLB:** Bare-metal LoadBalancer IP allocation.
- **Authelia:** SSO/2FA authentication gateway.

## Storage Architecture

Four tiers designed for different workload characteristics:

| Tier | Use Case | Hardware | Why |
|------|----------|----------|-----|
| `local-path` | Ephemeral scratch | OS drive | No replication needed for throwaway data |
| `endurance` | Write-heavy (Frigate NVR, downloads) | PM953 enterprise MLC | Enterprise endurance rating for sustained writes |
| `performance` | Critical HA (databases, games) | 990 PRO | Fast consumer SSD with LINSTOR replication |
| `general-ha` | Configs, media staging | NFS from ZFS NAS | Bulk storage with ZFS redundancy |

## Open Source Contribution

Operating this cluster directly led to the LINSTOR-CSI bug fix (PR #411). Bryan's storage topology (dedicated storage-only satellite nodes) exposed a deadlock that simpler configurations wouldn't encounter. The bug was discovered, diagnosed, and fixed through actual production operations.

## AI-Driven Operations

The cluster is managed through specialized AI agents:
- Investigation and state analysis via homelab-manager
- Kubernetes operations via platform-executor
- GitOps commits via git-workflow-manager
- Deployment lifecycle via deployment-manager

This is documented in Bryan's CLAUDE.md: "prefer subagent delegation over direct Bash/tool use for complex operations."

## Services

Home Assistant, Frigate (NVR with object detection), Plex, Sonarr, Radarr, Lidarr, Prowlarr, SABnzbd, Overseerr, game servers (DST, Valheim), Authelia, Traefik, cert-manager, MetalLB, ArgoCD.

## Significance for Resume

- Production Kubernetes operations (not just "I deployed to k8s once")
- Storage architecture design with hardware-aware tier selection
- GitOps practices with ArgoCD
- Infrastructure debugging that led to upstream open source contributions
- AI-driven operations management

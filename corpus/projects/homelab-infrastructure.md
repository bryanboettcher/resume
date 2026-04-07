---
title: Homelab Kubernetes Cluster
tags: [kubernetes, talos, linstor, drbd, argocd, gitops, helm, docker, storage, networking, metallb, traefik, authelia, zfs, knative, prometheus, monitoring, ai-operations]
children:
  - projects/homelab-infrastructure-storage.md
  - projects/homelab-infrastructure-gitops.md
  - projects/homelab-infrastructure-nas-ai.md
related:
  - evidence/infrastructure-devops.md
  - evidence/helm-chart-engineering.md
  - evidence/distributed-systems-architecture.md
  - evidence/agent-first-development.md
  - evidence/open-source-contributions.md
  - evidence/ai-driven-development.md
  - projects/mpc-ups-hardware.md
  - projects/dst-dedicated-server.md
  - projects/wyoming-rust.md
category: project
contact: resume@bryanboettcher.com
---

# Homelab Infrastructure — Index

Bryan operates a 3-node Kubernetes cluster as personal infrastructure for home automation, media services, game servers, AI workloads, and development environments. This is not a toy cluster with default settings — it runs production-grade practices: immutable OS (Talos Linux), GitOps deployment (ArgoCD), block-level replicated storage (LINSTOR/DRBD), tiered snapshot backup, mTLS between storage components, and Prometheus alerting across every layer.

## Hardware

| Component | Specification |
|-----------|--------------|
| Nodes | 3x Minisforum MS-A2 |
| CPU | AMD Ryzen 9 7945HX (16C/32T per node, 48C/96T total) |
| RAM | 96 GB per node (288 GB total) |
| Performance SSD | Samsung 990 PRO 2TB per node |
| Endurance SSD | Samsung PM953 2TB per node (enterprise MLC) |
| NAS | 60 TB ZFS pool |
| Network | 2x 10GbE bonded per node |

## Services Deployed

Home Assistant, Frigate (NVR with Coral TPU object detection), Mosquitto MQTT, Plex, Sonarr, Radarr, Lidarr, Bazarr, Prowlarr, SABnzbd, Overseerr, Tautulli, Nextcloud, Qdrant (vector database), resume-chat, Homarr (dashboard), Valheim, Don't Starve Together, Sunshine (game streaming), Authelia (SSO/2FA), Traefik (ingress), cert-manager, MetalLB, ArgoCD, KubeVirt, WireGuard, Kubernetes Dashboard.

## Child Documents

- **[Storage Architecture](homelab-infrastructure-storage.md)** — Talos Linux immutable OS with LINSTOR/DRBD adaptations (systemd patches, LVM path redirects, pre-compiled DRBD modules). Four-tier hardware selection (performance 3-way DRBD, endurance 1-way, general-ha NFS/ZFS, local-path ephemeral). DRBD Protocol A tuning for 10GbE (10MB buffers, 16K maxBuffers/epochSize, congestionFill, TRIM resync). Dual LVM/ZFS backends with rolling migration support. Two-layer snapshot system: local LINSTOR rollback + oxidize rsync to NAS. 13 PrometheusRules for LINSTOR and DRBD health. Helm-generated mTLS PKI. Open source LINSTOR-CSI bug fix.

- **[GitOps with ArgoCD and Custom Helm Charts](homelab-infrastructure-gitops.md)** — Namespace pre-creation ApplicationSet (sync-wave -50) with Pod Security Standards by namespace. Four domain-specific ApplicationSets (apps, home, games, pvr) with Go templates, self-heal, and `ignoreDifferences` for StatefulSet mutations. Reusable `arr-app` base chart for 9 PVR applications reducing each to ~20 lines of configuration. Ollama dual-mode chart (standard Deployment or Knative scale-to-zero) with GPU type detection and `deepCopy`/`mergeOverwrite`. Data restore Jobs chart with three backup access methods and security-hardened containers.

- **[NAS Management and AI-Driven Operations](homelab-infrastructure-nas-ai.md)** — NAS docker-compose with always-running exporters and scheduler-driven backup chain (`service_completed_successfully` dependency ordering). Prometheus alerts covering disk health (SMART), Btrfs integrity, backup freshness, and drive power management. Three-agent AI operations model: homelab-manager (planning), platform-qa (validation), platform-executor (execution) with per-command timeout, risk level, and rollback commands.

---

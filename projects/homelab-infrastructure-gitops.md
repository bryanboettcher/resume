---
title: Homelab Infrastructure — GitOps with ArgoCD and Custom Helm Charts
tags: [kubernetes, argocd, gitops, helm, metallb, traefik, authelia, knative, prometheus, pod-security-standards, applicationsets]
related:
  - projects/homelab-infrastructure.md
  - projects/homelab-infrastructure-storage.md
  - projects/homelab-infrastructure-nas-ai.md
  - evidence/helm-chart-engineering.md
  - evidence/infrastructure-devops.md
category: project
contact: resume@bryanboettcher.com
parent: projects/homelab-infrastructure.md
---

# Homelab Infrastructure — GitOps with ArgoCD and Custom Helm Charts

This document covers the GitOps deployment architecture, namespace pre-creation, domain-specific ApplicationSets, and the custom Helm charts including the reusable arr-app base and Ollama dual-mode deployment.

---

## GitOps with ArgoCD ApplicationSets

### Namespace Pre-Creation

The `namespaces.yaml` ApplicationSet (sync-wave `-50`) pre-creates all namespaces before any workloads deploy. Each namespace element specifies its Pod Security Standard level:

- `privileged`: Storage (piraeus-system, local-path-storage), networking (metallb-system, traefik), monitoring (node-exporter needs hostNetwork/hostPID), gaming (Sunshine needs /dev/dri), home (Frigate needs Coral TPU at /dev/apex_0), virtualization (kubevirt, cdi)
- `baseline` or unset: Application namespaces (pvr, cloud, ai, dashboards, authelia)

Some namespaces override the three-level PSS independently — `pvr` enforces `privileged` (Plex GPU hostPath) but audits and warns at `baseline`.

### Domain-Specific ApplicationSets

Four ApplicationSets at sync-wave `30` deploy workloads, each with Go templates (`goTemplate: true`, `missingkey=error`):

- **apps**: Nextcloud, Homarr dashboard, Qdrant vector DB, resume-chat
- **home**: Mosquitto MQTT, Home Assistant, Frigate NVR
- **games**: Valheim, Don't Starve Together, Sunshine (game streaming)
- **pvr**: arr-stack (separate ApplicationSet following the same pattern)

All use automated sync with self-heal, prune, and exponential backoff retry (3 attempts, 5s base, 2x factor, 3m max). The `home` ApplicationSet adds `ignoreDifferences` for StatefulSet `volumeClaimTemplates` — Kubernetes mutates these after creation, causing perpetual drift without the ignore rule. Source points to a self-hosted Git repo (`insta@10.13.1.30:/main/documents/git/homelab.git`), not GitHub.

---

## Custom Helm Charts

### Arr-App Base Chart

The `charts/apps/arr-app/` chart is a reusable base for LinuxServer.io media applications. It provides a standard deployment template with:

- Configurable init containers for first-run setup (SABnzbd host whitelist injection, auth disablement for reverse-proxy setups)
- Dynamic media volume mounts generated from `media.types` list (e.g., `[tv, downloads]` for Sonarr, `[movies, downloads]` for Radarr)
- LinuxServer.io environment variables (PUID, PGID, TZ)
- Optional extra volume for app-specific needs
- Health probes, ingress with optional Authelia integration, and PVC templates

Nine PVR applications consume this as a Helm dependency (`repository: "file://../../arr-app"`): Sonarr, Radarr, Lidarr, Bazarr, Prowlarr, SABnzbd, Overseerr, Tautulli, and Plex. Each wrapper chart is just a `Chart.yaml` declaring the dependency and a `values.yaml` setting app-specific values:

```yaml
# charts/apps/pvr/sonarr/values.yaml
arr-app:
  app:
    name: sonarr
    image:
      repository: lscr.io/linuxserver/sonarr
      tag: latest
    port: 8989
  media:
    types: [tv, downloads]
  configStorageClass: general-ha
  snapshotTier: standard
```

This reduces each new PVR app to ~20 lines of configuration.

### Ollama Dual-Mode Deployment

The Ollama chart supports two deployment modes toggled by `knative.enabled`:

- **Standard Deployment:** Always-running pod with GPU resources.
- **Knative Service:** Scale-to-zero with configurable concurrency, timeouts, and idle timeout. The template handles GPU resource injection by detecting GPU type (NVIDIA vs AMD) and merging the appropriate resource limit using Helm's `deepCopy`/`mergeOverwrite`. A `postStart` lifecycle hook polls `ollama ps` until the server is ready, then pulls/creates/runs configured models.

### Data Restore Operations Chart

The `charts/ops/data-restore/` chart generates Kubernetes Jobs for restoring PVC data from backups. The job template iterates over a `restores` list, generating one Job per restore operation with:

- Three backup access methods: NFS direct mount, hostPath, or existing PVC
- Two copy methods: rsync (with optional `--delete`) or cp
- Security-hardened containers (drop ALL capabilities, add only CHOWN/DAC_OVERRIDE/SETFCAP)
- Optional PVC owner references for garbage collection

---

## Key Files

- `kubernetes/argocd/applicationsets/namespaces.yaml` — Namespace pre-creation with Pod Security Standards
- `kubernetes/argocd/applicationsets/apps.yaml` — Domain-specific ApplicationSet with Go templates
- `charts/apps/arr-app/templates/deployment.yaml` — Reusable base chart for 9 PVR apps
- `charts/apps/pvr/sonarr/Chart.yaml` — Example arr-app consumer (20-line wrapper)
- `charts/ai/ollama/templates/knative/service.yaml` — Dual Deployment/Knative mode with GPU DRA
- `charts/ops/data-restore/templates/job.yaml` — Parameterized restore Jobs

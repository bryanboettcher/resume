---
title: Homelab Infrastructure — Storage Architecture (LINSTOR/DRBD, Talos, Snapshots)
tags: [kubernetes, talos, linstor, drbd, storage, zfs, lvm, snapshots, backup, drbd-tuning, 10gbe, mTLS, prometheus]
related:
  - projects/homelab-infrastructure.md
  - projects/homelab-infrastructure-gitops.md
  - projects/homelab-infrastructure-nas-ai.md
  - evidence/infrastructure-devops.md
category: project
contact: resume@bryanboettcher.com
parent: projects/homelab-infrastructure.md
---

# Homelab Infrastructure — Storage Architecture

This document covers the storage design decisions: Talos Linux-specific LINSTOR/DRBD adaptations, four-tier hardware selection, DRBD protocol tuning for 10GbE, dual LVM/ZFS backends, and the two-layer snapshot backup system.

---

## Operating System: Talos Linux

Talos is an immutable, API-driven Kubernetes OS. No SSH, no shell, no package manager — managed entirely via `talosctl`. This forces infrastructure-as-code: every change must be expressed as configuration, applied through the API, and tracked in Git.

The LINSTOR satellite configuration demonstrates the Talos-specific adaptations required. The `linstorSatelliteConfigurations` section uses kustomize `$patch: delete` directives to remove systemd-dependent init containers (`drbd-shutdown-guard`, `drbd-module-loader`) and systemd-related volumes that don't exist on Talos. DRBD kernel modules are pre-compiled as a Talos system extension rather than compiled at runtime. LVM paths are redirected from `/etc/lvm` (read-only on Talos) to `/var/etc/lvm/backup` and `/var/etc/lvm/archive` via hostPath volume overrides.

---

## Four-Tier Hardware Selection

| Tier | Hardware | Replication | Filesystem | Reclaim | Use Case |
|------|----------|-------------|------------|---------|----------|
| `performance` | Samsung 990 PRO | 3-way DRBD | ext4 | Retain | Databases, game worlds |
| `endurance` | Samsung PM953 | 1-way | xfs (reflink) | Delete | Frigate NVR, downloads |
| `general-ha` | NFS from ZFS NAS | 1-way DRBD | ext4 | Retain | App configs, media staging |
| `local-path` | OS drive | None | — | Delete | Ephemeral scratch |

The `performance` tier uses 3-way replication because it protects irreplaceable state (game save data, databases). The `endurance` tier uses 1-way because write-heavy NVR recording would amplify writes 3x across the cluster for data that can be re-recorded.

---

## DRBD Protocol Tuning for 10GbE

All tiers use DRBD Protocol A (asynchronous replication) — writes complete when the local disk confirms, without waiting for network acknowledgment. This trades a small durability window for dramatically better write IOPS, which is acceptable for workloads with journaling (PostgreSQL, ext4).

Network buffers are sized for 10GbE bandwidth:

- `sndbufSize` / `rcvbufSize`: 10MB each (optimal TCP window for 10GbE)
- `maxBuffers`: 16,000 for NVMe tiers (64MB buffer pool at 4KB pages), 20,000 for NAS tier
- `maxEpochSize`: 16,000 (matched to maxBuffers for balanced throughput)
- `alExtents`: 6,433 (effective maximum for default ring-buffer activity log)
- `congestionFill`: 20MB with `pull-ahead` congestion policy (LINSTOR maximum)
- `rsDiscardGranularity`: 1MB (enables TRIM during resync for SSD health)

A comment in the configuration notes that LINSTOR's resync controller validation limits are much stricter than native DRBD (max 4MB/s for `c-max-rate`), making those parameters too low to be useful on 10GbE. The configuration relies on Protocol A + pull-ahead instead.

---

## Dual Storage Backend: LVM and ZFS

Nodes are labeled with `storage.homelab/backend: lvm` or `storage.homelab/backend: zfs`, and LINSTOR satellite configurations use `nodeSelector` to apply the appropriate storage pool definitions. This allows rolling migration from LVM to ZFS one node at a time — the 682-line `agent-workflows/zfs-migration-workflow.yaml` documents the phase-based migration process with validation checks and rollback procedures at each stage.

---

## Snapshot Tiering and Oxidize Backup

**Rollback snapshots** (local LINSTOR thin snapshots, instant copy-on-write, no network traffic):
- `critical` tier: Every 4 hours, 42 retained (7 days). For game worlds, Home Assistant, databases.
- `plex` tier: Daily, 7 retained.
- `standard` tier: Daily, 14 retained. For arr-stack configs and general app data.
- `local-only` tier: Every 4 hours, 42 retained.

**Oxidize snapshots** (rsync to NAS spinning rust):
- Snapshots labeled with `sync.mallcop.dev/oxidize: "true"` trigger a workflow that rsyncs snapshot contents to the NAS.
- Staggered 1 hour apart to avoid overwhelming the NAS with concurrent transfers.
- Companion `-oxidize` storage classes use `placementCount: 1` and `reclaimPolicy: Delete` — single replica because the source is already replicated.

---

## LINSTOR Monitoring and mTLS PKI

The LINSTOR cluster templates deploy 13 PrometheusRules across two rule groups:

**LINSTOR rules:** Controller offline, satellite/controller error rates (15-minute window), satellite not online, storage pool errors, storage pool capacity <20%.

**DRBD rules:** Reactor offline, connection not connected, device not UpToDate, unintentional diskless (indicates backing device I/O errors), device without quorum, resource suspended, resync without progress (5-minute delta check), resource with no UpToDate replicas.

The `api-tls.yaml` implements two PKI approaches: cert-manager (using a self-signed CA with 10-year duration) or Helm-generated certificates (using `genCA` and `genSignedCert` functions to create a CA plus signed certificates for controller, client, CSI controller, and CSI node components with proper SANs).

## Open Source Contribution

Operating this storage topology led directly to a LINSTOR-CSI bug fix. Bryan's cluster uses dedicated storage-only satellite nodes, which exposed a `published_node_ids` bug in the CSI driver. The fix lives in a forked image (`ghcr.io/bryanboettcher/linstor-csi:v1.10.5-fix-published-node-ids`) referenced in the Piraeus values, with comments pointing to the fork repository and ADR-010.

---

## Key Files

- `charts/storage/piraeus/values.yaml` — LINSTOR/DRBD configuration with Talos patches and DRBD tuning
- `charts/storage/piraeus/linstor-cluster/templates/monitoring.yaml` — 13 PrometheusRules for DRBD and LINSTOR
- `charts/storage/piraeus/linstor-cluster/templates/api-tls.yaml` — Helm-generated PKI for mTLS
- `charts/storage/snapscheduler/values.yaml` — Four-tier snapshot schedule with oxidize backup

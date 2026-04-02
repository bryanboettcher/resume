---
title: Helm Chart Engineering — Data Restore Operations Chart
tags: [helm, kubernetes, operations, disaster-recovery, jobs, rsync, templating, infrastructure-as-code, gitops]
related:
  - evidence/helm-chart-engineering.md
  - evidence/helm-chart-engineering-arr-app-base-chart.md
  - evidence/helm-chart-engineering-argocd-applicationsets.md
  - evidence/infrastructure-devops.md
  - projects/homelab-infrastructure.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/helm-chart-engineering.md
---

# Helm Chart Engineering — Data Restore Operations Chart

The homelab `charts/ops/data-restore/` chart treats bulk PVC data restoration from backups as a declarative, version-controlled operation. Each restore is a values entry that can be enabled/disabled independently; the chart generates one Kubernetes Job per entry. Jobs support NFS/hostPath/PVC backup sources, rsync or cp copy methods, and optional `ownerReferences` linking Jobs to target PVCs for automatic garbage collection.

---

## Evidence: Data Restore Operations Chart

The `charts/ops/data-restore/` chart is a custom operational tool for bulk PVC data restoration from backups. The `templates/job.yaml` (200 lines) iterates `{{- range .Values.restores }}` to generate one Kubernetes Job per restore operation, each conditionally gated by `{{- if .enabled }}`.

Each Job supports three backup access methods selected by `global.backupAccessMethod`:
- **NFS**: Mounts backup storage via `nfs.server` and `nfs.path`
- **hostPath**: Direct node filesystem access (requires node scheduling)
- **PVC**: Mounts an existing PersistentVolumeClaim

The copy logic itself is templated with two methods: `rsync` (with conditional `--delete` and `--chown=0:0` flags) and `cp` (with conditional `-a` for permission preservation). Both methods include error handling — verifying source directory existence, listing contents before and after copy, and using `set -e` for fail-fast behavior.

An interesting detail: the Job template supports optional `ownerReferences` linking the Job to the target PVC. When `setPVCOwner` is true, deleting the PVC will garbage-collect the restore Job automatically — preventing orphaned completed Jobs from accumulating.

The `values.yaml` (288 lines) defines restore operations for the full PVR stack (Sonarr, Radarr, Lidarr, Prowlarr, SABnzbd, Overseerr, Plex, Bazarr) plus home automation (Home Assistant, Frigate, Mosquitto) and other services. Each entry specifies source path, target PVC, namespace, storage class, copy method, and resource limits — a declarative disaster recovery runbook.

## Key Files

- `homelab:charts/ops/data-restore/templates/job.yaml` — templated restore Jobs with rsync/cp methods, NFS/hostPath/PVC sources
- `homelab:charts/ops/data-restore/values.yaml` — declarative restore manifest for ~15 services

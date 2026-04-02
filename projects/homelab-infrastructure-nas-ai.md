---
title: Homelab Infrastructure — NAS Management and AI-Driven Operations
tags: [kubernetes, docker, zfs, prometheus, alerting, ai-operations, agent-workflows, backup, monitoring, nas]
related:
  - projects/homelab-infrastructure.md
  - projects/homelab-infrastructure-storage.md
  - projects/homelab-infrastructure-gitops.md
  - evidence/infrastructure-devops.md
  - evidence/agent-first-development.md
  - evidence/ai-driven-development.md
category: project
contact: resume@bryanboettcher.com
parent: projects/homelab-infrastructure.md
---

# Homelab Infrastructure — NAS Management and AI-Driven Operations

This document covers the NAS docker-compose services with scheduled backup chains, disk health monitoring, and the three-agent AI-driven operations model for cluster deployments.

---

## NAS Management

The NAS runs docker-compose at `nas/docker-compose.yml` with two categories of services:

**Always running:** node-exporter (Prometheus host metrics), zfs-exporter (ZFS pool metrics, custom build), smartctl-exporter (SMART disk health), hdparm-exporter (drive power state, custom build), and Samba.

**Scheduled services** (triggered by a scheduler container, not auto-started — using Docker Compose profiles `["scheduled"]`):
- **Backup chain:** Three containers with dependency ordering — `backup-mount` mounts the backup drive, `backup-rsync` copies data (depends on mount completing successfully), `backup-unmount` unmounts (depends on rsync completing). Each uses `condition: service_completed_successfully` to enforce the chain.
- **Sanoid:** ZFS snapshot management with a bind-mounted configuration.
- **Btrfs scrub:** Periodic integrity checks for the backup drive.

A shared `textfile_data` volume connects the exporters to node-exporter's textfile collector, allowing custom metrics (hdparm power states, backup timestamps) to be scraped alongside standard host metrics.

---

## NAS-Specific Prometheus Alerts

NAS-specific alerts in `nas/alerts/prometheus-alerts.yml` cover:

- **Disk health:** reallocated sectors, pending sectors, uncorrectable sectors, temperature >50C, SMART unhealthy
- **Btrfs integrity:** scrub errors, scrub overdue >45 days
- **Backup freshness:** no backup in 48+ hours, backup failure
- **Power management:** array drives awake >3 hours

---

## AI-Driven Operations

The cluster is managed through specialized AI agents documented in `agent-workflows/platform-deploy-workflow.md`. A three-agent pattern with validation gates orchestrates infrastructure deployments:

1. **homelab-manager** (Sonnet): Strategic planning and task decomposition. Read-only access.
2. **platform-qa** (Sonnet): Validates workflow YAML, checks command syntax, runs dry-run tests. Cannot fix issues.
3. **platform-executor** (Haiku): Executes commands exactly as specified. Cannot improvise.

The separation of concerns prevents scope creep — the planner cannot execute, the validator cannot fix, and the executor cannot improvise. Failed deployments iterate up to 3 times, with each iteration receiving the previous failure diagnostics as context. The workflow YAML format includes per-command timeout, risk level, exit code validation, and optional rollback commands.

---

## Key Files

- `nas/docker-compose.yml` — NAS services with scheduler-driven backup chain
- `nas/alerts/prometheus-alerts.yml` — Disk health, backup freshness, power management alerts
- `agent-workflows/platform-deploy-workflow.md` — Three-agent deploy orchestration
- `agent-workflows/zfs-migration-workflow.yaml` — 682-line phase-based ZFS migration with rollback procedures

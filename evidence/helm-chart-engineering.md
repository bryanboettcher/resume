---
title: Helm Chart Engineering
tags: [helm, kubernetes, gitops, argocd, templating, infrastructure-as-code]
related:
  - evidence/infrastructure-devops.md
  - projects/homelab-infrastructure.md
children:
  - evidence/helm-chart-engineering-arr-app-base-chart.md
  - evidence/helm-chart-engineering-ollama-dual-deployment.md
  - evidence/helm-chart-engineering-linstor-monitoring.md
  - evidence/helm-chart-engineering-data-restore-ops.md
  - evidence/helm-chart-engineering-argocd-applicationsets.md
  - evidence/helm-chart-engineering-static-pv-storage.md
category: evidence
contact: resume@bryanboettcher.com
---

# Helm Chart Engineering — Index

Bryan's homelab Kubernetes cluster is deployed entirely through custom Helm charts managed via ArgoCD GitOps. The `homelab` repository contains 40+ charts organized into a taxonomy of concerns: `charts/ai/`, `charts/apps/`, `charts/infra/`, `charts/storage/`, `charts/monitoring/`, `charts/ops/`, and `charts/core/`. These are not thin wrappers — they contain substantial templating logic, local chart composition, conditional resource type selection, Kubernetes version detection, and Helm-generated PKI.

## Child Documents

- **[Reusable Base Chart Pattern (arr-app)](helm-chart-engineering-arr-app-base-chart.md)** — Single base chart deploying 9 PVR applications via local filesystem dependency composition. Dynamic media volume mounts from `range` loops, optional init containers for first-run configuration (SABnzbd host whitelist, Tautulli auth disable), tiered storage class mapping.

- **[Ollama Chart — Dual Deployment Mode with GPU Resource Claims](helm-chart-engineering-ollama-dual-deployment.md)** — Single values schema that renders either a Kubernetes `Deployment` or a Knative `Service`. Three GPU provisioning paths: DRA with `semverCompare` K8s version detection, NVIDIA device plugin with MIG slicing, AMD GPU. Declarative model lifecycle via `postStart` hooks.

- **[LINSTOR Monitoring Stack and Piraeus API TLS](helm-chart-engineering-linstor-monitoring.md)** — Complete Prometheus stack (dashboard, ServiceMonitor, PodMonitor, 13 alert rules for LINSTOR and DRBD) generated from one template gate. Dual TLS strategy: cert-manager Certificates or inline Helm PKI using `genCA`/`genSignedCert` with `without (uniq ...)` deduplication.

- **[Data Restore Operations Chart](helm-chart-engineering-data-restore-ops.md)** — Declarative disaster recovery runbook as Helm chart: one Job per `restores[]` entry, three backup source types (NFS/hostPath/PVC), rsync/cp copy methods with error handling, optional PVC ownerReferences for Job garbage collection.

- **[ArgoCD ApplicationSets for Fleet Deployment](helm-chart-engineering-argocd-applicationsets.md)** — Go template generators with `missingkey=error` strict mode, self-healing sync, exponential backoff retries, and sync waves for deployment ordering. Adding a new application requires one list entry.

- **[Static PV Generation and Storage Taxonomy](helm-chart-engineering-static-pv-storage.md)** — Templated static PV generation with `storage.homelab/` custom labels for inventory filtering. The `config-pvs` and `media-pvs` charts implement the static PV layer; together with `piraeus`, `nfs-csi`, `snapscheduler`, and `snapshot-controller` they form the four-tier storage architecture.

---
title: Helm Chart Engineering — Ollama Chart with Dual Deployment Mode and GPU Resource Claims
tags: [helm, kubernetes, ollama, gpu, knative, dra, templating, infrastructure-as-code, ai-infrastructure]
related:
  - evidence/helm-chart-engineering.md
  - evidence/helm-chart-engineering-arr-app-base-chart.md
  - evidence/helm-chart-engineering-linstor-monitoring.md
  - evidence/helm-chart-engineering-argocd-applicationsets.md
  - evidence/infrastructure-devops.md
  - projects/homelab-infrastructure.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/helm-chart-engineering.md
---

# Helm Chart Engineering — Ollama Chart with Dual Deployment Mode and GPU Resource Claims

The homelab Ollama chart supports two mutually exclusive deployment strategies from a single values schema: a standard Kubernetes `Deployment` or a Knative `Service` for scale-to-zero inference, toggled by `knative.enabled`. The GPU resource handling covers three distinct provisioning paths (DRA with Kubernetes version detection, NVIDIA device plugin with MIG slicing, AMD GPU), and includes a declarative model lifecycle system via `postStart` hooks.

---

## Evidence: Ollama Chart — Dual Deployment Mode with GPU Resource Claims

The `charts/ai/ollama/` chart (forked and extended) supports two mutually exclusive deployment strategies controlled by a single `knative.enabled` toggle. When `knative.enabled` is false, the chart renders a standard Kubernetes `Deployment` (`templates/deployment.yaml`, 294 lines). When true, it renders a Knative `Service` (`templates/knative/service.yaml`, 201 lines) for scale-to-zero inference. The two templates are gated by `{{- if not .Values.knative.enabled }}` and `{{- if .Values.knative.enabled }}` respectively — they share the same values schema but produce fundamentally different resource types.

The GPU resource handling contains notable conditional logic. The deployment template supports three GPU provisioning paths:

1. **DRA (Dynamic Resource Allocation)**: For Kubernetes 1.34+, uses `ResourceClaimTemplate` with version-aware API selection (`resource.k8s.io/v1` vs `resource.k8s.io/v1beta1` via `semverCompare`). The deployment references the claim via `resourceClaims` in the pod spec.
2. **Traditional device plugin**: For NVIDIA GPUs without DRA, merges GPU limits (`nvidia.com/gpu`) into the resource limits dict using Helm's `merge` function. Supports MIG (Multi-Instance GPU) slicing with a range loop over `gpu.mig.devices` to generate per-slice resource requests.
3. **AMD GPU**: Merges `amd.com/gpu` limits via the same merge pattern.

Bryan's production configuration runs CPU-only (`gpu.enabled: false`) with 3 replicas, pod anti-affinity spreading across nodes, shared NFS model storage (`ReadWriteMany` to a static PV at `/main/assets/models/ollama`), and tuned environment variables (`OLLAMA_NUM_PARALLEL=2`, `OLLAMA_MAX_LOADED_MODELS=2`). The values file documents the rationale for each setting with section headers and inline comments.

The chart also includes a model lifecycle management system: `postStart` lifecycle hooks wait for the Ollama process to become ready, then pull, create, and run models declaratively. The deployment template even supports model cleanup — when `ollama.models.clean` is true, it compares the running model list against the declared set and removes undeclared models.

## Key Files

- `homelab:charts/ai/ollama/templates/deployment.yaml` — standard Deployment with GPU conditional logic
- `homelab:charts/ai/ollama/templates/knative/service.yaml` — Knative Service alternative
- `homelab:charts/ai/ollama/templates/resourceclaimtemplate.yaml` — DRA GPU provisioning with K8s version detection
- `homelab:charts/ai/ollama/values.yaml` — production config: 3 replicas, CPU-only, NFS model storage

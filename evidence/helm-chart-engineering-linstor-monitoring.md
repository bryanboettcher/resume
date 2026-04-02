---
title: Helm Chart Engineering — LINSTOR Monitoring Stack and Piraeus API TLS
tags: [helm, kubernetes, prometheus, linstor, drbd, monitoring, alerting, mtls, cert-manager, pki, templating]
related:
  - evidence/helm-chart-engineering.md
  - evidence/helm-chart-engineering-ollama-dual-deployment.md
  - evidence/helm-chart-engineering-argocd-applicationsets.md
  - evidence/infrastructure-devops.md
  - projects/homelab-infrastructure.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/helm-chart-engineering.md
---

# Helm Chart Engineering — LINSTOR Monitoring Stack and Piraeus API TLS

The homelab LINSTOR storage chart generates a complete Prometheus monitoring stack (dashboard ConfigMap, ServiceMonitor, PodMonitor, 13 PrometheusRule alerts) from a single `{{- if .Values.monitoring.enabled }}` gate. The same chart handles mTLS PKI via two strategies: cert-manager Certificate resources or Helm-generated certificates using `genCA`/`genSignedCert` with list deduplication for client cert generation.

---

## Evidence: LINSTOR Monitoring Stack

The `charts/storage/piraeus/linstor-cluster/templates/monitoring.yaml` (193 lines) generates a complete Prometheus monitoring stack for the LINSTOR/DRBD storage layer. The template conditionally creates four distinct resource types behind `{{- if .Values.monitoring.enabled }}`:

1. **Grafana dashboard ConfigMap**: Loads a bundled `dashboard.json` via `.Files.Get` and labels it with `grafana_dashboard: "1"` for Grafana's sidecar auto-discovery.
2. **ServiceMonitor for linstor-controller**: Scrapes the controller's `/metrics` endpoint. The template switches between HTTPS (with mTLS client certificates from named secrets) and plain HTTP based on whether API TLS is configured — using `{{- if or .Values.createApiTLS (dig "apiTLS" "" .Values.linstorCluster) }}`.
3. **PodMonitor for linstor-satellite**: Scrapes satellite pods with a relabeling rule that maps `__meta_kubernetes_pod_node_name` to a `node` label, enabling per-node storage metrics in dashboards.
4. **PrometheusRule with 13 alert rules** across two groups (`linstor.rules` and `drbd.rules`):
   - LINSTOR alerts: controller offline, satellite error rate (PromQL `increase()` over 15m), satellite not online, storage pool errors, storage pool capacity < 20%
   - DRBD alerts: reactor offline, connection not connected, device not UpToDate, unintentional diskless (indicates I/O errors on backing device), quorum loss, resource suspended, resync without progress (uses `delta()` over 5m to detect stalled resyncs), resource with zero UpToDate replicas

The alert descriptions include actionable remediation commands — for example, the satellite error rate alert tells the operator to run `linstor error-reports list --nodes {{ $labels.hostname }} --since 15minutes`.

---

## Evidence: Piraeus API TLS — Dual Certificate Strategy

The `charts/storage/piraeus/linstor-cluster/templates/api-tls.yaml` (64 lines) implements two distinct TLS provisioning strategies selected by `{{- if eq .Values.createApiTLS "cert-manager" }}` vs `"helm"`:

- **cert-manager mode**: Creates a self-signed CA `Certificate` (10-year duration, `isCA: true`) and an `Issuer` referencing it. The CA then signs controller and client certificates via cert-manager's normal flow.
- **Helm-generated mode**: Uses Helm's `genCA` and `genSignedCert` functions to generate a complete certificate chain at render time. The template generates a CA, a controller certificate with multiple SANs (fully qualified service name, short service name, bare name), and then iterates over a deduplicated list of client secret names (CSI controller, CSI node, general client) to generate signed client certificates. The `without (uniq $clientCerts) $controllerSecretName` expression avoids generating a duplicate secret when the controller and a client share the same secret name.

This is a non-trivial use of Helm's cryptographic template functions to produce a complete mTLS PKI inline.

## Key Files

- `homelab:charts/storage/piraeus/linstor-cluster/templates/monitoring.yaml` — full monitoring stack: dashboard, ServiceMonitor, PodMonitor, 13 PrometheusRules
- `homelab:charts/storage/piraeus/linstor-cluster/templates/api-tls.yaml` — dual-strategy TLS provisioning with Helm-generated PKI

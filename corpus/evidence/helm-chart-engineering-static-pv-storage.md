---
title: Helm Chart Engineering — Static PV Generation and Storage Taxonomy
tags: [helm, kubernetes, persistent-volumes, storage, nfs, csi, infrastructure-as-code, gitops]
related:
  - evidence/helm-chart-engineering.md
  - evidence/helm-chart-engineering-arr-app-base-chart.md
  - evidence/helm-chart-engineering-linstor-monitoring.md
  - evidence/infrastructure-devops.md
  - projects/homelab-infrastructure.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/helm-chart-engineering.md
---

# Helm Chart Engineering — Static PV Generation and Storage Taxonomy

The homelab manages static PersistentVolumes through two charts (`config-pvs`, `media-pvs`) that iterate a values list to generate PVs with custom `storage.homelab/` labels for inventory filtering. These charts implement the static PV layer that the dynamic PVR app charts bind against, and together with `nfs-csi`, `piraeus`, `snapscheduler`, and `snapshot-controller` they form the four-tier storage architecture as composable Helm charts.

---

## Evidence: Static PV Generation and Storage Taxonomy

The `charts/storage/config-pvs/templates/static-pvs.yaml` generates static PersistentVolumes by iterating over a values list:

```yaml
{{- range .Values.staticPVs }}
---
apiVersion: v1
kind: PersistentVolume
metadata:
  name: {{ .name }}
  labels:
    storage.homelab/type: static-config
    storage.homelab/namespace: {{ .namespace }}
spec:
  storageClassName: {{ .storageClass }}
  csi:
    driver: {{ .csiDriver }}
    volumeHandle: {{ .volumeHandle }}
{{- end }}
```

The custom labels (`storage.homelab/type`, `storage.homelab/namespace`) enable filtering and inventory of statically provisioned storage across the cluster. This pattern is replicated in `charts/storage/media-pvs/` for NFS-backed media volumes. Together, these charts manage the static PV layer that the dynamic PVR app charts bind against.

The overall storage chart structure — `config-pvs`, `media-pvs`, `nfs-csi`, `piraeus`, `snapscheduler`, `snapshot-controller` — implements the four-tier storage architecture (local-path, endurance, performance, general-ha) as composable Helm charts.

## Key Files

- `homelab:charts/storage/config-pvs/templates/static-pvs.yaml` — templated static PV generation with custom labels

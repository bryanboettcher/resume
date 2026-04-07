---
title: Helm Chart Engineering — Reusable Base Chart Pattern (arr-app)
tags: [helm, kubernetes, chart-composition, templating, pvr, local-dependencies, infrastructure-as-code, gitops]
related:
  - evidence/helm-chart-engineering.md
  - evidence/helm-chart-engineering-ollama-dual-deployment.md
  - evidence/helm-chart-engineering-static-pv-storage.md
  - evidence/infrastructure-devops.md
  - projects/homelab-infrastructure.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/helm-chart-engineering.md
---

# Helm Chart Engineering — Reusable Base Chart Pattern (arr-app)

The homelab Kubernetes cluster deploys 9 PVR applications (Sonarr, Radarr, Lidarr, Prowlarr, SABnzbd, Overseerr, Tautulli, Bazarr, Plex) from a single shared base chart. Each consumer chart declares `arr-app` as a local filesystem dependency and provides only a short values file (30-50 lines); the base chart handles all shared concerns including dynamic media volume mounts, tiered storage classes, and optional init containers for first-run configuration.

---

## Evidence: Reusable Base Chart Pattern (`arr-app`)

The `charts/apps/arr-app/` chart is a shared base chart for deploying LinuxServer.io PVR applications (Sonarr, Radarr, Lidarr, Prowlarr, SABnzbd, Overseerr, Tautulli, Bazarr, Plex). Individual app charts like `charts/apps/pvr/radarr/` declare `arr-app` as a local filesystem dependency:

```yaml
# charts/apps/pvr/radarr/Chart.yaml
dependencies:
  - name: arr-app
    version: "1.0.0"
    repository: "file://../../arr-app"
```

Each consumer provides only app-specific values, while the base chart handles all shared concerns:

- **Deployment template** (`arr-app/templates/deployment.yaml`, 168 lines): Generates the full Deployment spec including LinuxServer.io environment variables (PUID/PGID/TZ), health probes, security context with `fsGroup`, and dynamic volume mounts generated from a `media.types` list.
- **Dynamic media volume mounts**: The deployment iterates `{{- range .Values.media.types }}` to generate per-media-type PVC mounts. Radarr declares `media.types: [movies, downloads]` and gets volume mounts for both; SABnzbd declares only `[downloads]`. Access modes vary per consumer — Radarr uses `ReadWriteMany` for shared access, while read-only consumers can use `ReadOnlyMany`.
- **Optional init containers**: The base chart conditionally generates init containers for app-specific first-run configuration. SABnzbd uses `initHostWhitelist` to pre-seed `sabnzbd.ini` with allowed hostnames before the main container starts. Tautulli uses `initDisableAuth` to disable internal authentication when running behind Authelia. These init containers handle both first-run (create config) and subsequent-run (patch existing config) scenarios with idempotent shell logic.
- **Optional extra volumes**: SABnzbd enables an extra `incomplete` volume (400Gi on the `endurance` storage tier) for write-heavy temporary downloads, while most apps don't need it. This is toggled via `extra.enabled` in values.
- **Tiered storage mapping**: Config PVCs use `configStorageClass` (typically `performance` or `general-ha`), media PVCs bind to pre-provisioned static NFS PVs, and extra volumes target the `endurance` tier for write-heavy workloads. Each consumer selects the appropriate tier for its workload characteristics.

The result: 9 PVR applications deployed from a single base chart, each differentiated only by a short values file (typically 30-50 lines). Adding a new PVR app requires a `Chart.yaml` with the dependency declaration and a `values.yaml` with app-specific settings.

## Key Files

- `homelab:charts/apps/arr-app/templates/deployment.yaml` — base deployment template with conditional init containers, dynamic media mounts
- `homelab:charts/apps/arr-app/values.yaml` — base chart defaults with documented user preferences vs technical defaults
- `homelab:charts/apps/pvr/radarr/Chart.yaml` — consumer dependency declaration
- `homelab:charts/apps/pvr/radarr/values.yaml` — minimal consumer values
- `homelab:charts/apps/pvr/sabnzbd/values.yaml` — consumer with extra volume and init container configuration

---
title: Helm Chart Engineering — ArgoCD ApplicationSets for Fleet Deployment
tags: [helm, kubernetes, argocd, gitops, applicationsets, go-templates, sync-waves, fleet-management, infrastructure-as-code]
related:
  - evidence/helm-chart-engineering.md
  - evidence/helm-chart-engineering-arr-app-base-chart.md
  - evidence/helm-chart-engineering-ollama-dual-deployment.md
  - evidence/helm-chart-engineering-data-restore-ops.md
  - evidence/infrastructure-devops.md
  - projects/homelab-infrastructure.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/helm-chart-engineering.md
---

# Helm Chart Engineering — ArgoCD ApplicationSets for Fleet Deployment

The homelab uses ArgoCD `ApplicationSet` resources with Go template generators to deploy chart groups. `goTemplateOptions: ["missingkey=error"]` causes rendering to fail on missing keys, catching typos at render time rather than deploying broken applications. Self-healing, pruning, exponential backoff retries, and sync waves are configured per ApplicationSet — adding a new application to the fleet requires a single list entry.

---

## Evidence: ArgoCD ApplicationSets for Fleet Deployment

The `kubernetes/argocd/applicationsets/` directory uses ArgoCD's `ApplicationSet` resource with Go template generators to deploy chart groups. The `apps.yaml` and `games.yaml` files define list generators — each element specifies a name, namespace, and chart path, and the template generates a full ArgoCD `Application` with:

- **Go templates with strict mode**: `goTemplateOptions: ["missingkey=error"]` causes template rendering to fail on missing keys rather than silently producing empty values — a defensive choice that catches typos in element definitions.
- **Automated sync with self-healing**: `selfHeal: true` and `prune: true` ensure the cluster converges to the Git state even if someone manually modifies resources.
- **Exponential backoff retries**: Failed syncs retry up to 3 times with `duration: 5s, factor: 2, maxDuration: 3m`.
- **Sync waves**: ApplicationSets use `argocd.argoproj.io/sync-wave: "30"` to control deployment ordering relative to infrastructure components.

The chart taxonomy enables this: infrastructure charts deploy in early waves, storage in mid-waves, and application charts in later waves. Adding a new application to the fleet requires adding one element to the appropriate ApplicationSet list.

## Key Files

- `homelab:kubernetes/argocd/applicationsets/apps.yaml` — ApplicationSet for user applications (Nextcloud, Homarr, Qdrant, resume-chat)
- `homelab:kubernetes/argocd/applicationsets/games.yaml` — ApplicationSet for game servers (Valheim, DST, Sunshine)

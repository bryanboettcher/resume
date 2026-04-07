---
title: Cloud-Orca 3D Printer Slicer
tags: [dotnet, angular, threejs, 3d-printing, docker, adapter-pattern, webgl, docker-compose, minimal-api]
related:
  - evidence/frontend-web-development.md
  - evidence/hardware-embedded-systems.md
  - evidence/infrastructure-devops.md
category: project
contact: resume@bryanboettcher.com
---

# Cloud-Orca — Project Narrative

## Context

A web-based 3D printer slicer that wraps existing slicing engines (CuraEngine, with OrcaSlicer planned) with a modern web UI featuring Three.js 3D model visualization. Slicer software converts 3D models (STL files) into machine instructions (GCode) for 3D printers.

## Architecture

- **Backend:** ASP.NET Core Minimal API (.NET 9) serving as the slicing orchestrator
- **Frontend:** Angular 19 SPA with Three.js for 3D model rendering and manipulation
- **Adapter pattern:** Pluggable slicer engine backends — CuraEngine implemented first, OrcaSlicer planned
- **API:** `/api/slice` (submit job), `/api/printers` (list configured printers), `/api/health`
- **Docker Compose:** Full stack orchestration with CuraEngine built from source

## Significance for Resume

- Full-stack web application with non-trivial 3D visualization (Three.js/WebGL)
- Adapter pattern enabling pluggable backends
- Docker orchestration including building C++ dependencies from source
- Domain knowledge in 3D printing/manufacturing toolchain

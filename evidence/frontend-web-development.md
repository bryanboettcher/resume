---
skill: Frontend & Web Development
tags: [Angular, TypeScript, Three.js, HTML5, responsive, SPA, testing]
relevance: Demonstrates modern frontend capability complementing deep backend expertise — not just a backend dev who touches HTML
---

# Frontend & Web Development — Evidence Portfolio

## Overview

Bryan's frontend work centers on Angular (currently v19) with TypeScript. He builds production admin UIs and developer tools, not just demo apps. His frontend projects demonstrate modern practices: standalone components, reactive patterns (RxJS), comprehensive test coverage, and Docker deployment with runtime configuration injection.

---

## Evidence: KbClient — Angular Admin Application

**Repository:** https://github.com/bryanboettcher/KbClient
**Local path:** ~/src/bryanboettcher/KbClient/
**Tech:** Angular 19, TypeScript 5.8, RxJS, Jest, Mock Service Worker (MSW)

### Purpose
Single-page backoffice application for RC hobby shop staff (KbStore platform). Full CRUD for product catalog and inventory management.

### Technical Details
- **Angular 19** with standalone components (no NgModules)
- **TypeScript 5.8** with strict mode
- **RxJS** reactive patterns for state management and API communication
- **Jest** for unit testing (not Karma/Jasmine — deliberate choice for speed)
- **Mock Service Worker (MSW)** for API mocking in tests and development
- **Docker deployment** with nginx serving the SPA
- **Runtime environment injection** — configuration loaded at runtime, not baked into the build (allowing a single Docker image across environments)

### Architecture Patterns
- Standalone component architecture (Angular 19 best practice)
- Reactive data flow via RxJS
- Component-level test isolation with MSW
- Docker multi-stage builds (Node for build → nginx for serve)

---

## Evidence: Madera/Call-Trader — Angular Admin Platform

**Repository:** github.com/Call-Trader/madera-apps (private)
**Tech:** Angular 19, TypeScript 5.8, Angular Material, CoreUI, PapaParse, RxJS

### Purpose
Full-featured admin platform for direct mail campaign management. This is a substantially more complex frontend than KbClient.

### Technical Details
- **Angular Material + CoreUI** component libraries
- **Custom reusable components:** Dropdowns, date pickers, multi-select controls, paginated data tables
- **PapaParse** for client-side CSV parsing with interactive column mapping UI
- **Levenshtein distance matching service** for fuzzy field name matching during import (client-side ML-adjacent logic)
- **Jasmine/Karma** testing (earlier project, before migration to Jest in later work)

### Notable Features
- **CSV Import UI:** Users upload CSVs, the frontend parses them client-side, presents column headers, and uses Levenshtein distance to suggest field mappings. This is a sophisticated UX pattern that reduces import errors without requiring exact column naming.
- **Complex filtering interface:** 12 composable filter types for mail file population, each with its own UI component and validation
- **Real-time updates:** RxJS-driven reactive updates for import progress and status changes

---

## Evidence: Cloud-Orca — Three.js 3D Visualization

**Local path:** ~/src/bryanboettcher/cloud-orca/
**Tech:** Angular 19, Three.js, TypeScript

### Purpose
Web-based 3D printer slicer with in-browser model visualization.

### Technical Details
- **Three.js** integration for rendering 3D models (STL files) in the browser
- Camera controls, model manipulation, and slice preview
- Communicates with .NET backend API for actual slicing operations
- Demonstrates ability to work with WebGL/3D rendering — not a typical CRUD frontend skill

---

## Evidence: Stack Overflow — Frontend Knowledge

### Webpack Style Loader (Score: 15, 9,422 views)
Answered a question about webpack style loader behavior, demonstrating understanding of frontend build tooling internals.

### Node.js Networking (Score: 18, 9,252 views)
Explained `0.0.0.0` vs `127.0.0.1` binding for Node.js servers — demonstrating full-stack networking knowledge.

### HTML5 Local Storage for Assets (Score: 33, Software Engineering SE)
Answered a question about using HTML5 local storage to cache CSS and JavaScript, demonstrating web platform knowledge and performance optimization thinking applied to frontend delivery.

---

## Summary

Bryan's frontend work demonstrates:
- **Modern Angular:** v19 with standalone components, latest TypeScript, reactive patterns
- **Production deployment:** Docker + nginx with runtime configuration injection
- **Testing discipline:** Jest + MSW (later projects), Jasmine/Karma (earlier projects)
- **Complex UX:** CSV import with fuzzy matching, 12-filter composition UIs, 3D model visualization
- **Full-stack integration:** Frontends connect to his own backends via typed API contracts
- **Build tooling:** Webpack understanding, Docker multi-stage builds for frontend assets

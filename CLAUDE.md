# Resume Project

## Purpose
This project contains structured evidence documents, project narratives, and link registries that serve as source material for Bryan Boettcher's resume. The eventual goal is a static PDF resume (via Typst) AND a dynamic web resume with an embedded RAG chatbot that answers "how would Bryan solve this?" using real examples from his work.

## Current Phase
**Static resume creation** — building a professional PDF resume using Typst.

## Directory Structure

```
evidence/          — One file per skill claim, each self-contained with concrete examples, links, and metrics
  performance-optimization.md
  ai-driven-development.md
  distributed-systems-architecture.md
  open-source-contributions.md
  infrastructure-devops.md
  hardware-embedded-systems.md
  dotnet-csharp-expertise.md
  frontend-web-development.md
  data-engineering-etl.md
  leadership-mentoring.md

projects/          — One file per major project/role with full narratives
  call-trader-madera.md        — Direct mail platform (June 2024 – Oct 2025) **NOT ON CURRENT RESUME, MUST ADD**
  kbstore-ecommerce.md         — DDD e-commerce platform
  fastaddress-research.md      — Semantic address matching research
  homelab-infrastructure.md    — 3-node Talos K8s cluster
  wyoming-rust.md              — Embedded Rust voice satellite
  mpc-ups-hardware.md          — Custom UPS hardware (KiCAD)
  cloud-orca-slicer.md         — Web-based 3D slicer
  career-history.md            — Complete corrected career timeline

links/             — URL registries with descriptions (fragile to reconstruct, preserve carefully)
  github-prs.md                — All PR URLs with status, repo stars, descriptions
  github-repos.md              — Public + local-only repositories
  stackoverflow.md             — Profile URLs, every high-scored Q&A with scores/views
  external-profiles.md         — Website, blog, LinkedIn, email
```

## Key Facts

- **Name:** Bryan Boettcher
- **Email:** resume@bryanboettcher.com
- **Location:** Kansas City, KS area
- **Experience:** 25+ years in software engineering
- **Current status:** Between roles (since October 2025)
- **Target role:** AI-first workflow with RAG experience, performance focus
- **Primary tech:** C#/.NET 9, ASP.NET Core, MassTransit, Angular 19, TypeScript, Docker, Kubernetes, Rust

## Career Timeline (Corrected)

1. Call-Trader — Senior/Lead Engineer (Jun 2024 – Oct 2025) — **MISSING FROM CURRENT RESUME**
2. Taylor Summit Consulting — Software Architect/Lead (2023 – Oct 2025) — concurrent with Call-Trader
3. Kansys, Inc. — Software Architect/Lead (2020 – 2023)
4. Henry Wurst / Mittera Creative Services — Sr. Developer (2018 – 2020)
5. Service Management Group — Sr. Developer (2016 – 2018)
6. Earlier roles (2001–2016): iModules, VI Marketing, Ticket Solutions, Softek Solutions, Cities Unlimited

## Resume Tooling

- **Format:** Typst (`.typ` files)
- **Compile:** `typst compile resume.typ` → produces `resume.pdf`
- **Watch mode:** `typst watch resume.typ` for live reload during development
- **View rendered output:** Read the PDF via the Read tool to check layout

## Conventions

- The evidence docs are the RAG corpus — keep them verbose, self-contained, and accurate
- The link registries are fragile to reconstruct — verify URLs still work before citing them
- The resume PDF should be concise (2 pages max) but the evidence docs backing it are intentionally detailed
- Bryan is self-aware about overengineering and has a good sense of humor about it, but the resume itself should be professional
- Do NOT be sycophantic — accuracy over flattery. His livelihood depends on this document.
- Most resumes are now LLM-ingested — structure content for both human and machine readability

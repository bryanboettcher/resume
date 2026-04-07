---
title: AI-Driven Development & Agentic Workflows
tags: [ai, agentic, claude-code, llm, automation, multi-agent, workflow, ml, genetic-algorithm, onnx, openai, azure-cognitive-services]
related:
  - evidence/agent-first-development.md
  - evidence/open-source-contributions.md
  - evidence/healthcare-pharma-domain.md
  - projects/call-trader-madera.md
  - projects/homelab-infrastructure.md
  - projects/fastaddress-research.md
  - projects/taylor-summit.md
category: evidence
contact: resume@bryanboettcher.com
---

# AI-Driven Development — Evidence Portfolio

## Philosophy

Bryan treats AI as a force multiplier for engineering judgment, not a replacement for it. His usage pattern spans from tactical (using AI to contribute in unfamiliar languages) to strategic (orchestrating multi-agent development workflows across entire projects). He is an early and sophisticated adopter of Claude Code with custom agent configurations, hooks, and specialized subagent delegation patterns.

---

## Evidence: Cross-Language Open Source Contribution via AI

### LINSTOR-CSI Bug Fix (Go — a language Bryan does not know)
**Issue:** https://github.com/piraeusdatastore/linstor-csi/issues/410
**PR:** https://github.com/piraeusdatastore/linstor-csi/pull/411 (Merged February 2026)
**Repo:** 126 stars, critical Kubernetes CSI storage plugin

Bryan discovered a deadlock bug in his homelab's Kubernetes storage layer where PersistentVolumeClaims would get permanently stuck in "Terminating" state. The root cause was in the LINSTOR-CSI plugin's `ListVolumes` implementation, which incorrectly reported permanent replicas and tie-breaker diskless nodes in `published_node_ids`.

**What makes this notable for AI-driven development:**
- Bryan does not know Go. He used agentic AI tooling to:
  1. Identify and classify the bug through log analysis and code tracing
  2. Understand the Go codebase well enough to propose a fix
  3. Implement the fix with proper Go idioms and test coverage
  4. Iterate on the approach after code review feedback from maintainer WanzenBug
- The initial approach (resource-type filtering) was refined to a more precise property-based approach (`Aux/csi-created-for=temporary-diskless-attach`) based on maintainer feedback
- The PR passed all CI checks, was reviewed, and merged — the upstream maintainer accepted it as production-quality code
- This demonstrates AI as a tool for extending an engineer's reach into unfamiliar territory while maintaining engineering rigor (proper root cause analysis, iterative code review, test coverage)

---

## Evidence: Multi-Agent Development Orchestration (Madera/Call-Trader)

**Repository:** github.com/Call-Trader/madera-apps (private)
**Local path:** ~/src/bryanboettcher/madera-apps/

The later development phases of the Madera platform show sophisticated Claude Code agent orchestration with 11 specialized agents configured in `.claude/agents/`:

### Agent Architecture
1. **systems-architect** — High-level architectural analysis, cross-system integration design
2. **dotnet-backend-engineer** — Performance-critical C# implementation, hot path optimization, database schema design
3. **angular-frontend-engineer** — Angular UI components, responsive layouts, user workflows
4. **integration-test-engineer** — Integration tests, performance tests, Docker-based test environments
5. **project-manager** — Requirements gathering, scoping, acceptance criteria, verification
6. **code-review** — Pre-commit review for correctness, security, standards compliance
7. **feature-dev:code-explorer** — Codebase analysis, execution path tracing, dependency mapping
8. **feature-dev:code-reviewer** — Confidence-based bug/vulnerability filtering
9. **feature-dev:code-architect** — Feature architecture design with implementation blueprints
10. **git-workflow-manager** — Autonomous Git operations with descriptive commit messages
11. **issue-tracker** — Centralized task management and sprint organization

### Workflow Design
- **Two-phase complexity triage:** Tasks are assessed for complexity before agent assignment. Simple tasks go directly to implementation; complex tasks route through architecture → planning → implementation → review pipeline.
- **Specialized context windows:** Each agent operates with focused context rather than bloating a single conversation. The systems-architect sees the full system; the backend-engineer sees only the relevant service code.
- **Agent-driven legacy analysis:** The project-manager and code-explorer agents were used to systematically analyze the predecessor Node.js system (Madera Digital), producing 45+ feature requirement documents that guided the .NET rewrite.

---

## Evidence: Homelab Infrastructure Management via AI Agents

**Local path:** ~/src/bryanboettcher/homelab/

Bryan's 3-node Talos Kubernetes cluster (288GB RAM, 48 cores) is managed through a delegation-first AI workflow:

### Agent Delegation Pattern
- **homelab-manager** — Investigation and cluster state analysis
- **platform-executor** — Kubernetes operations (deployments, scaling, troubleshooting)
- **git-workflow-manager** — GitOps commits for ArgoCD-driven deployments
- **deployment-manager** — Full deployment lifecycle from planning through validation

### Documented Workflow Principles (from CLAUDE.md)
- "Prefer subagent delegation over direct Bash/tool use for complex operations. Subagents have focused context and expertise; you maintain high-level reasoning while they handle implementation details."
- This is not just using AI for code completion — it's using AI as an operational tool for infrastructure management, with specialized agents handling different operational domains.

---

## Evidence: Claude Code Integration Across Projects

Bryan has `.claude/` configuration directories with CLAUDE.md files across multiple projects, each tailored to the project's specific patterns and constraints:

- **FastAddress:** Domain-specific agent configurations for the semantic matching codebase (fastaddress-codebase-guru, semantic-match-guru)
- **KbStore:** E-commerce platform with specialized agents for business domain knowledge (kb3d-guru)
- **Madera/Call-Trader:** Full 11-agent development team configuration
- **Homelab:** Infrastructure operations agents
- **Wyoming-Rust:** Rust development guidance

Each configuration demonstrates understanding of:
- How to scope agent context for optimal results
- When to delegate vs. when to work directly
- How to encode domain knowledge into agent instructions
- How to chain agent outputs into coherent workflows

---

## Evidence: AI-Assisted Architecture Documentation

Multiple projects contain AI-generated but human-curated architectural decision records (ADRs):
- **Homelab:** `adr-011-rust-wyoming-satellite.md` documenting the decision to write the Wyoming satellite in Rust
- **Madera:** Feature requirement documents generated through agent-driven legacy system analysis
- **FastAddress:** Research specifications and optimization analyses produced through structured AI-assisted research sessions

The pattern is consistent: AI generates comprehensive analysis, Bryan reviews and curates for accuracy, the result becomes project documentation that informs future development (including future AI agent sessions).

---

## Evidence: ML/AI Engineering — FastAddress Training Infrastructure

**Repository:** https://github.com/bryanboettcher/FastAddress (neural-embeddings branch)

Bryan's AI usage extends beyond consuming AI services into designing and building ML training systems:

### Genetic Algorithm Pattern Evolution
Built a GeneticSharp-based evolutionary search system for regex pattern discovery with grammar-aware chromosome representation (19 typed regex tokens across 5 categories), 7 mutation operators, parallelized fitness evaluation, and zero-allocation scoring using `ReadOnlyMemory<WeightedTrainingPair>` and `ref struct`.

### ML.NET Training Infrastructure
Dual-mode training system: AutoML (1-hour sweep across LightGBM, FastTree, SdcaMaximumEntropy) vs. manual LightGBM with optional CUDA GPU acceleration. Text featurization via word 3-grams + character 5-grams. Automatic strategy planning based on property cardinality analysis.

### Training Orchestration
TPL Dataflow pipeline (`TransformBlock<TrainingPlan, ITrainingResult>`) for concurrent training execution with pluggable trainer adapters. MediatR-based request/response for decoupled algorithm selection.

### Empirical Research at Scale
1.5 million training examples analyzed against Lob API validated data. Zipf's law distribution analysis for vocabulary sizing. Self-contained Soundex and Metaphone implementations for phonetic similarity research. Empirical CRC64 collision rate validation at scale.

---

## Evidence: Applied AI in Regulated Healthcare — Taylor Summit

**Project:** Clinical Care Platform for a regional mental health provider (Taylor Summit Consulting)

Bryan built AI-powered clinical workflows in a HIPAA-sensitive healthcare context:

- **Real-time speech-to-text** via Azure Cognitive Services Speech with SignalR streaming during behavioral health counseling sessions
- **Clinical session summarization** via OpenAI with iterative prompt engineering (4 prompt iterations visible) incorporating patient demographics, treatment modality identification, and coping skills extraction
- **Facial recognition for patient identity** via FaceAiSharp/ONNX Runtime for on-device ML inference — no cloud dependency for inference, embedding vector comparison via dot product

This is AI in a regulated context where accuracy, auditability, and data privacy aren't optional features — they're compliance requirements.

---

## Summary

Bryan's AI-driven development practice operates at four levels:
1. **Tactical:** Using AI to extend reach into unfamiliar languages and codebases (Go contribution to linstor-csi)
2. **Operational:** Using specialized AI agents to manage infrastructure and development workflows (homelab, Madera)
3. **Strategic:** Designing multi-agent systems that decompose complex development tasks into specialized roles with appropriate context scoping (11-agent Madera configuration)

4. **Engineering:** Designing ML training infrastructure (genetic algorithms, ML.NET pipelines, training orchestration) and integrating AI services into regulated healthcare workflows (clinical transcription, facial recognition, session summarization)

This is not "I use Copilot for autocomplete." This is treating AI as a composable engineering tool with intentional architecture around how and when different AI capabilities are applied — and building the training systems that produce the models.

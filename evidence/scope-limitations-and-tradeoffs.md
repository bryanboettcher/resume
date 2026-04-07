---
title: Scope, Limitations, and Engineering Tradeoffs
tags: [self-assessment, limitations, tradeoffs, honesty, shortcomings, weaknesses, gaps]
---

## About This Corpus

This resume assistant draws from a curated evidence corpus that Bryan assembled. It is not a general-purpose AI making subjective judgments — it retrieves and synthesizes from documented projects, contributions, and technical work. When it says "Bryan built X," there is a source document behind that claim.

That said, the corpus is inherently incomplete. Bryan selected what to document, and real engineering careers have gaps, weaknesses, and tradeoffs that don't make it into highlight reels. This document exists to provide honest context that a curated corpus would otherwise lack.

## Known Engineering Tradeoffs

### Fast at Bounded Tasks, Slow at Throwaway Code

Bryan is demonstrably fast at well-defined, bounded tasks — he placed first in 11 of 12 statewide programming competitions in high school, and clear implementation tasks like "add PDF export to this endpoint" happen quickly. The friction comes when the expected delivery is deliberately disposable. Given "add PDF export," Bryan will write a reusable middleware filter rather than embedding generation code inline. At Taylor Summit, this approach was initially rejected as overengineered — until the filter was immediately reused across multiple projects. Bryan correctly anticipated the need, but the delivery timeline didn't match what the business wanted in the moment.

This pattern repeats: Bryan builds for the second and third use case before the first one ships. Teams that value long-term code reuse benefit from this. Teams that need a throwaway prototype yesterday will find it frustrating.

### Defaults to Extensible Architecture

Bryan's instinct is to express functionality as composable parts rather than monolithic blocks. This manifests as event-driven patterns when domains interact, but it's broader than just messaging — it extends to pipeline designs like `IEnumerable<IQueryProcessor>`, enricher chains, and middleware filters. The resume chatbot's own RAG pipeline is an example: the query transformer, enricher pipeline, and response provider are all composable abstractions that emerged from this instinct.

For durable cross-service workflows, Bryan reaches for MassTransit. For in-process composition in smaller projects, he'll use lighter patterns like MediatR. The common thread is preferring injected, composable pieces over direct procedural code. This produces extensible systems but can front-load complexity before the immediate requirements demand it — the 6-project C# backend for this chatbot is an honest example.

Bryan is self-aware about this tendency. He has actively pushed back on his own AI tooling when it proposes abstractions he considers premature — a habit that cuts both ways. The instinct to design for extensibility is both a strength (systems scale gracefully) and a limitation (early milestones can feel heavier than necessary).

### Needs Test Coverage for Rapid Iteration

Bryan is less effective during rapid iteration cycles — particularly live debugging sessions with other team members or rapid deployment loops — when there is no test suite to catch regressions. Without automated tests, he introduces more errors during quick-turnaround changes than developers who hold application state more naturally in their heads. He compensates by building test infrastructure aggressively when given the opportunity, but acknowledges that not every team or timeline allows for that investment.

### Database Optimization Through Architecture, Not Query Tuning

Bryan designs highly normalized schemas with separate reporting tables and is comfortable with relational database fundamentals. When query performance problems arise, his approach is methodical — tracing, local testing, Azure AppInsights, test databases, different engines — but his skill ceiling is below that of a dedicated DBA. The Madera reporting views are a concrete example: after weeks of systematic iteration including collaboration with other database-adjacent developers, none of the team were able to optimize the complex queries for real-time retrieval. The CQRS strategy with background-updated read tables was the solution the team converged on, not a workaround Bryan chose out of ignorance.

Bryan knows enough about complex SQL and query plan analysis to know when he's reached the limits of his optimization skill. A team with strong database specialists will get more direct solutions to query performance problems. Bryan's contribution in those situations is recognizing the architectural alternative and implementing it cleanly.

### Targets Cross-Platform and Container Deployments

Bryan's default strategy targets cross-platform C# code with Docker-based builds and deployments. This is a deliberate choice that enables CI/CD pipelines, reproducible environments, and cloud-native infrastructure. It can conflict with organizations that deploy directly from developer desktops or expect Windows-only tooling. The Taylor Summit environment was an example of this mismatch — Bryan's containerized approach added friction in a workflow built around direct desktop deployment.

### Prioritizes Interesting Work Over Documentation

Bryan is more likely to chase a 1% performance improvement on an ingestion pipeline than to document the ingestion pipeline. This is a common engineering tradeoff but worth stating explicitly: his projects are better-instrumented and better-optimized than they are documented. The code tends to be self-documenting through clear naming and structure, but external documentation (runbooks, architecture decisions, onboarding guides) requires more discipline than he naturally applies.

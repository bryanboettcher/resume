---
skill: C# / .NET Ecosystem Expertise
tags: [csharp, dotnet, aspnet, ef-core, dapper, linq, async, testing, architecture]
relevance: Primary professional language for 20+ years with deep framework knowledge demonstrated through production systems, open source contributions, and community engagement
---

# C# / .NET Ecosystem Expertise — Evidence Portfolio

## Overview

C# and .NET have been Bryan's primary professional language for over 20 years, spanning from .NET Framework 1.x through .NET 9. His expertise is not just "I write C#" — it includes deep framework internals knowledge (contributing to IoC containers and distributed frameworks), performance engineering (SIMD, zero-allocation patterns), and modern architectural patterns (DDD, event-driven, vertical slice).

---

## Evidence: Production Systems Built

### Madera Direct Mail Platform (Call-Trader, 2024–2025)
- **.NET 9, C# 13** — latest framework version in production
- **ASP.NET Core Minimal APIs** with vertical slice architecture
- **MassTransit** saga state machines over RabbitMQ
- **Dapper + SQL Server 2022** for high-performance data access
- **Entity Framework Core** for later migration work
- **SqlBulkCopy** for batch data ingestion (5,000 records/batch)
- **System.IO.Hashing (CRC64)** for address deduplication
- **Microsoft.ClearScript (V8)** for embedded JavaScript transformation engine
- **Lamar IoC** with registry-based dependency injection
- **Sep + Sylvan** for high-performance CSV parsing
- **ClosedXML** for Excel generation
- **FluentValidation** for request validation
- **OpenTelemetry** for observability instrumentation
- **JWT + API Key** dual authentication
- **.NET Aspire** for local orchestration

**Scale:** 14 projects in the solution, 625+ source files, 203 SQL migrations, processing 30M recipients across 10-15M unique addresses.

### KbStore E-Commerce Platform (Personal, 2025–present)
- **.NET 9** with DDD bounded contexts
- **PostgreSQL + MongoDB** polyglot persistence
- **MassTransit + RabbitMQ** for inter-domain messaging
- **.NET Aspire** for orchestration
- **NUnit** for testing

### FastAddress Semantic Matching (Personal, 2025–present)
- **.NET 9, C# 13** with unsafe code and raw pointers
- **SIMD:** AVX2/AVX-512 vectorized operations
- **Zero-allocation:** `stackalloc`, `Span<T>`, value semantics throughout
- **BenchmarkDotNet** (planned) for regression tracking
- Target: <500ns per comparison, 1-2M comparisons/second

---

## Evidence: Framework-Level Contributions

### Lamar IoC Container (PR #362, Merged)
**URL:** https://github.com/JasperFx/lamar/pull/362

Fixed `StackOverflowException` in the Lamar IoC container by converting recursive method calls to iterative processing. Touched topological sort, expression writing, and resolver building — internal compilation pipeline components. Code reviewed by Jeremy Miller (StructureMap/Lamar creator).

### MassTransit (PRs #6039 and #5956, Closed)
**URLs:**
- https://github.com/MassTransit/MassTransit/pull/6039
- https://github.com/MassTransit/MassTransit/pull/5956

Implemented complete ADO.NET saga repositories (MySQL, PostgreSQL, SQL Server) with optimistic/pessimistic concurrency. Overhauled Dapper integration addressing generic type misuse and concurrency control issues. Code reviewed by Chris Patterson (MassTransit creator).

### NPoco Micro-ORM (PR #605, Merged)
**URL:** https://github.com/schotime/NPoco/pull/605

Added `ConfigureAwait(false)` across async code paths — demonstrates understanding of .NET synchronization context behavior and library development best practices.

### Custom MassTransit.DapperIntegration Library
Built a local library for Dapper-based saga persistence at Call-Trader, motivated by performance requirements that EF Core couldn't meet. This library directly informed the upstream MassTransit contributions.

---

## Evidence: Stack Overflow — C# Depth

Bryan's Stack Overflow C# tag stats: **44 answers, score 227, 25 questions, score 73** — overwhelmingly his dominant technology.

### Notable Answers Demonstrating Deep C# Knowledge

**Custom value types and performance benchmarking (Score: 62)**
Built a complete `Data256Bit` struct with `IEquatable<T>`, `IComparable<T>`, custom `GetHashCode()`, and benchmarked HashSet vs. sorted array binary search. Demonstrates understanding of value type semantics, equality implementation, and data structure performance characteristics.

**LINQ internals (Score: 29)**
Top answer on a 190K-view question about composing multiple WHERE clauses with LINQ extension methods. Demonstrates understanding of deferred execution and expression tree composition.

**Generic extension methods (Score: 15)**
Created `DataReaderExtensions.Read<T>()` for type-safe database reads with DBNull handling. Demonstrates generic constraint usage and extension method patterns.

**AutoMapper performance analysis (Score: 14)**
Benchmarked AutoMapper at 85K maps/sec and demonstrated 60x overhead vs. manual mapping. Quantitative analysis informing architectural decisions about convenience vs. performance tradeoffs.

**Async/await understanding**
The NPoco ConfigureAwait contribution and multiple SO answers demonstrate deep understanding of the .NET async machinery — synchronization contexts, continuation scheduling, and deadlock scenarios.

---

## Evidence: Testing Practices

### Madera/Call-Trader Testing Architecture
- **NUnit3** with BDD nested class patterns (Given/When/Then as nested classes)
- **NSubstitute** for mocking
- **Shouldly** for fluent assertions
- **Three-tier strategy:**
  - Unit tests: Faked dependencies, testing business logic in isolation
  - Integration tests: SQLite in-memory database, testing data access layer
  - E2E tests: .NET Aspire-orchestrated distributed tests with Testcontainers
- Replaced xUnit with NUnit3 specifically for BDD-friendly nested test fixtures

### Kansys (2020–2023)
Resume states: 85% unit test coverage, 95% integration test coverage — exceptional coverage metrics for enterprise software.

### Stack Overflow Unit Testing Answer (Score: 26)
**Site:** Software Engineering SE

Wrote a detailed answer decomposing a `LoginUser` method into testable units with repository pattern and dependency injection. Explains SRP, test isolation, and mock-based verification. Used as a teaching example for unit testing methodology.

---

## Evidence: Architectural Patterns

### Patterns Actively Used (with production evidence)
- **Domain-Driven Design:** Bounded contexts in KbStore (Catalog vs. Storefront with separate databases)
- **Event-Driven Architecture:** MassTransit sagas in both KbStore and Madera
- **Vertical Slice Architecture:** Madera's API organized by feature, not by layer
- **CQRS principles:** Command/query separation in API design
- **Pipeline/Chain of Responsibility:** FastAddress's multi-stage comparison pipeline, Madera's ETL Source→Processor→Sink pattern
- **Adapter Pattern:** Cloud-Orca's pluggable slicer engine backends
- **Template Method:** FastAddress's `BaseDomainProvider` for custom domain extensions
- **Repository Pattern:** Throughout, with both Dapper and EF Core implementations
- **State Machine:** MassTransit saga state machines with explicit state transitions and fault handling

### Stack Overflow Architecture Questions
- **"Interfaces on abstract classes"** (Score: 36, 27K views) — OO design patterns question demonstrating architectural thinking
- **"Storing large, rarely-changing data"** (Score: 15, 29K views) — Data architecture question about C# in-memory data management
- **"Are we queueing and serializing properly?"** (Score: 13, 2K views) — Distributed systems architecture validation

---

## Career Timeline in .NET

| Period | Role | .NET Version | Key Technologies |
|--------|------|-------------|------------------|
| 2001–2006 | Early career | .NET Framework 1.x–2.0 | VB.NET, ASP.NET WebForms, ADO.NET |
| 2006–2012 | Mid career | .NET Framework 3.5–4.0 | C#, WCF, LINQ, WPF |
| 2012–2016 | Senior roles | .NET Framework 4.5 | MVC, WebAPI, Entity Framework |
| 2016–2020 | Sr. Developer | .NET Core 2.x–3.x | ASP.NET Core, EF Core migration |
| 2020–2023 | Architect/Lead | .NET 5–7 | Microservices, MassTransit, CI/CD |
| 2023–2025 | Architect/Lead | .NET 8–9 | Minimal APIs, Aspire, DDD, SIMD |

---

## Summary

Bryan's C#/.NET expertise is not just breadth (25 years) — it's depth:
- Contributes to the frameworks he uses (Lamar, MassTransit, NPoco)
- Understands performance at the CPU level (SIMD, cache lines, zero-allocation)
- Implements sophisticated architectural patterns (DDD, event-driven sagas, pipeline architectures)
- Maintains high test coverage with principled testing strategies
- Stays current (targeting .NET 9, C# 13, Aspire in active projects)

---
type: career-history
description: Complete career timeline with corrected dates and details from user
---

# Bryan Boettcher — Career History

## Timeline (Most Recent First)

### Currently Between Roles (October 2025 – Present)
Active personal projects: FastAddress, KbStore, Wyoming-Rust, MPC-UPS, Cloud-Orca, homelab infrastructure. Open source contributions continuing.

### Call-Trader — Senior/Lead Engineer (June 2024 – October 2025)
**Product:** Madera Direct Mail Platform
**Team:** 3 engineers (Bryan + Sophie Walker + Lillian Fleming)
**What he did:**
- Led ground-up rewrite from Node.js/Express to .NET 9
- Designed multi-source ETL pipeline framework (4 data sources)
- Built MassTransit saga state machines for import/mail file orchestration
- Implemented address normalization pipeline (30M recipients, 10-15M unique addresses)
- Real-time target optimizer, prediction reporting, 12-filter recipient selection
- Embedded V8 JavaScript engine for configurable field transformations
- Established testing strategy (NUnit3 BDD, NSubstitute, Shouldly, Testcontainers)
- Designed CI/CD pipelines (8 GitHub Actions workflows)
- Expanded system from 45 features/9 domains to 100+ features/12 domains
**NOTE:** This role is NOT on the current resume. Must be added.
**NOTE:** Overlaps with Taylor Summit (concurrent employment).

### Taylor Summit Consulting, LLC — Software Architect/Lead (2023 – October 2025)
**Focus:** Healthcare/pharmaceutical platform spanning pharmacy management, clinical patient care, and mobile device management
**Domains:** Pharmacy dispensing & adjudication (NCPDP), drug supply chain traceability (DSCSA/EPCIS/GS1), clinical telehealth (behavioral health), mobile device management (Apple MDM/Android Enterprise), medical device tracking
**Tech:** .NET 6–9, ASP.NET Core, MassTransit, Dapper, EF Core, Azure (Functions, Service Bus, Cognitive Services, Notification Hubs), AWS (Lambda, S3), Angular 17, ONNX Runtime, ML.NET, .NET Aspire 9.3.1
**Key achievements:**
- Built full MDM server from scratch with PKI certificate chain management (self-signed CA, device identity certs, Apple push certs)
- Implemented NCPDP Telecommunications binary protocol for pharmacy claims adjudication
- Designed AI-powered clinical workflows: real-time speech-to-text, facial recognition for patient ID, OpenAI session summarization
- Rearchitected medical device tracking from imperative CRUD (330 files) to event-sourced MassTransit sagas (126 files)
- Extended Dapper.Contrib with IL Emit-based column-level dirty tracking
- Co-authored internal NuGet package (TaylorSummit-Core) shared across 20+ microservices
**NOTE:** End date corrected from "present" to October 2025. Concurrent employment with Call-Trader.

### Kansys, Inc. — Software Architect/Lead (2020 – 2023)
**Domain:** Configurable invoicing system for hospitality, manufacturing, retail, service, and subscription verticals
**Business context:**
- The prior version implemented billing rules as Win32 C++ COM components. The team couldn't maintain them, and the Windows dependency forced on-premises datacenter hosting for every client
- Rewrite: reactive eventing rules engine in C#, dramatically increasing the ability to add, chain, parallelize, and test rules. The 4–5x performance improvement was achieved on existing hardware, deferring costly infrastructure expansion
- Eliminating the Win32 dependency enabled Linux deployment, which eliminated datacenter hosting costs for most clients
- Also enabled selling to new clients with legal or compliance hosting requirements that precluded Windows-only on-prem infrastructure
- Zero automated tests in the original codebase → fully covered rules engine in the rewrite → all rule behavior documented and tuned against real customer expectations
**Key metrics:** 85% unit test coverage, 95% integration test coverage

### Henry Wurst, Inc. / Mittera Creative Services — Senior Developer (2018 – 2020)
**Domain:** Sheet-press printing company; IT had been treated as a necessary cost since the 1970s
**Business context:**
- The company had contracted most development work to third-party consultants — expensive, slow, and produced one-off systems with no shared foundation
- Bryan was a full-time hire. The direct savings on consulting fees were measurable, but the larger impact was structural: building a shared core pipeline eliminated the disparate one-off integrations and cut client integration time from months to weeks
- Introduced git, pull requests, code reviews, coding standards, kanban + sprints — none of these existed before Bryan joined
- The business grew during this period, caught Mittera Creative Services' attention, and was acquired
**Key achievements:**
- Modernized development practices (git, PRs, code reviews, sprints — introduced from scratch)
- Shared core pipeline replaced disparate one-off client integrations

### Service Management Group — Senior Developer (2016 – 2018)
**Domain:** Customer satisfaction survey platform; Bryan's work was on the analytics side — sentiment analysis and aggregate feedback processing
**Business context:**
- Analytics operations that previously took minutes were reduced to seconds through software optimization on existing infrastructure, avoiding hardware scaling costs
- The core business depended on fast, accurate aggregation of survey data; slow analytics meant delayed client reporting
**Key achievement:** 80% performance improvements in some applications

### iModules Software (2014 – 2016)

### VI Marketing and Branding (2014, 6-month contract)

### Ticket Solutions, Inc. / VeriShip, Inc. (2011 – 2013)

### Softek Solutions, Inc. (2006 – 2011)

### Cities Unlimited (2001 – 2006)

## Technical Stack (Current)
**Primary:** C#/.NET 9, ASP.NET Core, MassTransit, Dapper, EF Core, SQL Server, PostgreSQL, MongoDB, RabbitMQ, Angular 19, TypeScript, Docker, Kubernetes, Rust
**Secondary:** Go (AI-assisted), Python, PowerShell, KiCAD, Three.js
**Infrastructure:** Talos Linux, LINSTOR/DRBD, ArgoCD, Traefik, MetalLB, GitHub Actions
**Practices:** DDD, event-driven architecture, saga state machines, SIMD optimization, zero-allocation patterns, BDD testing, GitOps

## Education
Intentionally omitted from resume.

## Location
Kansas City, KS area

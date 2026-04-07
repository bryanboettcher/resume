---
project: Taylor Summit Healthcare Platform
company: Taylor Summit Consulting, LLC
dates: 2023 – October 2025
role: Software Architect/Lead
tags: [dotnet, healthcare, pharma, mdm, pki, azure, masstransit, dapper, angular, ai, onnx, regulatory-compliance]
---

# Taylor Summit Healthcare Platform — Project Narrative

## Context

Taylor Summit Consulting builds healthcare and pharmaceutical software spanning the full spectrum from drug supply chain management through pharmacy dispensing to clinical patient care. Bryan served as Software Architect/Lead, responsible for platform architecture across 20+ microservice projects targeting .NET 6–9, concurrent with his Call-Trader role.

## System Domains

### Pharmacy Management Platform (bulk pharmaceutical provider)

A full pharmacy management platform handling dispensing, inventory, drug pedigree tracking, insurance adjudication, and regulatory reporting.

#### NCPDP Telecommunications Standard Implementation
Complete implementation of the NCPDP (National Council for Prescription Drug Programs) binary protocol for pharmacy claims adjudication. Raw byte-level segment building with field IDs (`D2` RxReferenceNumber, `D7` ProductDispensedId, `E7` QuantityDispensed), coordinate of benefit (COB), DUR (Drug Utilization Review) segments, compound medication handling, and prior authorization flows. The `NdcClaimBroker` implements raw TLS socket communication with NDC Health claim switches.

#### SureScripts E-Prescribing Integration
Full NCPDP SCRIPT XML message parsing for electronic prescriptions — hundreds of data model classes implementing the SureScripts/NCPDP specification including veterinary prescriber support, transfer requests, and controlled substance prescribing.

#### GS1/EPCIS Supply Chain Compliance
EPCIS (Electronic Product Code Information Services) document generation for DSCSA (Drug Supply Chain Security Act) FDA compliance. Implements GS1 standard with TraceLink-specific extensions: SGTIN/SSCC/SGLN URN generation, ObjectEvent (commissioning), AggregationEvent (packaging), ObserveEvent (shipping). GS1 DataMatrix barcode parsing for pharmaceutical product identification (GTIN, lot number, expiration, serial number).

#### Pharmaceutical Regulatory Compliance
- **Prescription Monitoring Program (PMP)** reporting for DEA-tracked controlled substances (Schedule II–V)
- **Drug recall management** — lot tracking, contact management, affected location profiling, multi-channel notification
- **Pharmaceutical pricing engine** — multi-tier pricing with AWP (Average Wholesale Price), WAC (Wholesale Acquisition Cost), brand vs. generic tiered markup, contract-based pricing with multiplier/percentage/dollar markup types
- **MediSpan drug database integration** — automated import of Wolters Kluwer MediSpan MF2/IPM drug reference data (NDC, pricing, therapeutic class grouping), 25+ import targets with template method pattern, CRC64 hash verification, transactional truncate-and-reload

#### Architecture
- .NET 8 Web API, Dapper + stored procedures, Lamar IoC, Mapster, API versioning, FluentValidation, Serilog
- Multi-site pharmacy chain support (`ForSite(siteId)` pattern throughout)
- SCIM integration for Azure AD user provisioning
- EDI (Electronic Data Interchange) processing via AWS Lambda for healthcare X12 transactions (837/835/270/271) with ReadyToRun publishing for cold-start optimization

### Clinical Care Platform (regional mental health provider)

A telehealth/behavioral health platform for clinical facilities.

#### AI-Powered Clinical Workflows
- **Real-time speech-to-text transcription** during counseling sessions via Azure Cognitive Services Speech + SignalR streaming, with bulk SQL TVP inserts for transcript storage
- **AI session summarization** — OpenAI-powered clinical note generation with prompt engineering incorporating patient demographics (age bracket, gender), treatment modality identification, and coping skills extraction
- **Facial recognition for patient identity verification** — `FaceAiSharp.Bundle` with ONNX Runtime for on-device ML inference, single-face enforcement, embedding vector comparison via dot product confidence scoring

#### Crisis Escalation Infrastructure
Huntgroup-tiered escalation for behavioral health crisis calls — Azure Functions process queue messages for tier escalation, timer-triggered functions handle session cleanup and reminder scheduling. Twilio integration for SMS/voice, Azure Notification Hubs for mobile push.

#### Data Synchronization Framework
Configuration-driven ETL framework with dynamic pipeline composition:
- `IDynamicSource` → `IDynamicTarget` interfaces with HTTP endpoint and SQL stored procedure implementations
- **Runtime TVP schema introspection** — dynamically discovers SQL table-valued parameter schemas by inspecting stored procedure parameter metadata, then creates `SqlDataRecord` with matching schema
- **Schema-agnostic streaming** via `IAsyncEnumerable<IDictionary<string, object?>>`
- **Roslyn source generator** (`ISourceGenerator`) emitting streaming JSON deserializer with manual buffer management

### MDM — Mobile Device Management Server

A complete Apple/Android MDM server built from scratch — a rare and specialized system.

#### Apple MDM Protocol Implementation
Full protocol compliance including:
- CheckIn service parsing Apple plist payloads
- Device enrollment profile generation with embedded PKCS12 identity certificates
- Profile signing, client certificate validation middleware
- Apple Push Notification Service (APNs) integration
- Apple Business Manager / DEP (Device Enrollment Program) integration with OAuth1
- 20+ MDM command handlers: EraseDevice, ClearPasscode, InstallApplication, EnableLostMode, ScheduleOsUpdate, etc.

#### PKI Certificate Chain Management
- MDM Certificate Authority generation (self-signed CA)
- Device identity certificate generation (CA → device cert chain)
- Apple Push Certificate Request and MDM Vendor Certificate Request generation
- DEP Server Token generation/decryption
- PEM/PKCS8 private key importers, PKCS7 MIME content type support

#### Android Enterprise
Google Android Management API (`Google.Apis.AndroidManagement.v1`) integration for Android device fleet management.

#### Additional MDM Patterns
- **Dynamic device group tagging** via `NCalcAsync` expression evaluation — rule-based group membership
- **Multi-tenant architecture** with per-tenant certificate management
- **CQRS with MassTransit** for device state change event publishing
- Profile types: WiFi, Single-App, Brand, General, Applications, OS Updates

### Medical Device Tracking — Event-Sourced System

.NET 9 + Aspire 9.3.1 system tracking medical devices through their lifecycle.

#### Saga Compensation Pattern
`DeviceTagStateMachine` (500 lines) implementing:
- Three-state lifecycle (Active/Removed/Compensated) with tag superseding
- Fault-based compensation — subscribes to `Fault<T>` messages for rollback, transitions to Compensated state, publishes undo messages
- Multi-strategy event correlation — DeviceTagId, DeviceId+TagType, SupersededBy pointer, AuditId, or type/device fallback
- State restoration from both Removed and Compensated states
- Location migration and bulk transfer acceptance as cross-aggregate operations

#### Architecture Evolution
Rearchitected from imperative CRUD (old: MediatR + Dapper, 330 files) to event-sourced sagas (new: MassTransit + EF Core + Aspire, 126 files) — 60% reduction in file count while adding compensation, correlation, and audit capabilities.

#### Comprehensive Saga Testing
18+ test files covering: superseding, compensation, restoration, location migration, transfer acceptance, correlation fallback, device loss, and multi-tag-type scenarios.

### TaylorSummit-Core — Internal NuGet Package

Co-authored shared infrastructure library (`Authors: Paul Walls, Bryan Boettcher`):
- `StartupBuilder` pattern — fluent API bootstrapping with partial classes for Auth, Caching, CORS, Data, Endpoints, ExceptionHandler, Serilog, SignalR, Swagger, Versioning
- Report file conversion framework (CSV/Excel/JSON bidirectional)
- Dapper wrappers and Lamar IoC conventions
- GitHub Actions CI/CD for build and release

### Dapper.Contrib — IL Emit Extensions (Fork)

Forked and extended the official Dapper.Contrib with column-level dirty tracking:
- `IProxyDetails` interface exposing `GetDirtyFields()` and `ResetChanges()` — partial UPDATE statements writing only changed columns
- **IL Emit proxy generation** — runtime code emitting proxy types via `System.Reflection.Emit` that intercept property setters and record modifications to a `HashSet<string>`
- Reduces SQL bandwidth and unnecessary column locks in high-concurrency healthcare scenarios

## Technology Stack

**Backend:** .NET 6–9, C#, ASP.NET Core, MassTransit, Dapper, EF Core, SQL Server, Azure Service Bus, RabbitMQ, Lamar IoC, Mapster, FluentValidation, Serilog, Hangfire, .NET Aspire 9.3.1

**AI/ML:** Azure Cognitive Services Speech, OpenAI API, FaceAiSharp/ONNX Runtime, ML.NET

**Cloud:** Azure AD/Entra ID, Azure Functions, App Service, Container Apps, Blob Storage, Service Bus, Notification Hubs, SignalR Service, Application Insights, Azure SQL

**AWS:** Lambda, S3, SFTP

**Frontend:** Angular 17, TypeScript, SignalR, ngx-gauge, ngx-charts

**Security/PKI:** X.509 certificate chain management, PKCS7/PKCS8/PKCS12, Apple APNs, OAuth1, SCIM

**Protocols/Standards:** NCPDP Telecommunications, NCPDP SCRIPT XML, GS1/EPCIS, DSCSA, HL7/X12 EDI, Apple MDM, Google Android Enterprise

**Testing:** NUnit, comprehensive saga testing (18+ test files for DeviceManagement alone)

## Business Context

Each domain at Taylor Summit had a specific business driver:

**Pharmacy platform:** The platform was absorbing another acquired product — integration/absorption work, not greenfield development. The NCPDP Telecommunications implementation, SureScripts e-prescribing, and DSCSA/GS1 traceability were all compliance-deadline regulatory requirements. Missing a regulatory deadline in pharmacy has licensure consequences.

**Clinical speech-to-text/summarization:** Real-time transcription and AI-powered note generation saved several minutes per clinical session and increased accuracy of clinical notes. In a behavioral health setting with high session volume, clinician time is the primary cost constraint. More accurate notes also reduce administrative rework.

**MDM server:** This was built to sit alongside Jamf — it filled gaps in hierarchical device grouping that Jamf didn't support for multi-location distribution. Organizations managing device fleets across many physical locations with complex group structures needed capabilities Jamf couldn't provide.

**Medical device tracking rearchitecture — honest assessment:** The event-sourced saga system was rearchitected from imperative CRUD (330 files) to MassTransit sagas (126 files). The architecture was technically sound — 60% code reduction, proper compensation, full audit capabilities. However, it was ultimately scrapped after Bryan's departure, deemed too complex for the team that inherited it. The lesson: architectural sophistication has to be calibrated to the team that will maintain it. A correct architecture that a team can't operate isn't a net win.

**Role constraints:** This was a consulting/architect role with pre-scoped assignments. Bryan was not setting product strategy — he was executing design and implementation within requirements handed down by the client organizations. The technical decisions (protocol implementations, saga design, AI integrations) were Bryan's; the scope and priorities were not.

## Significance for Resume

- **Regulated domain expertise:** FDA drug traceability (DSCSA), DEA controlled substances (PMP), healthcare compliance — demonstrates ability to work in highly regulated environments with real deadline consequences
- **Protocol implementation depth:** NCPDP binary protocol, Apple MDM protocol, GS1/EPCIS XML — shows ability to implement complex standards from specification documents
- **PKI/Security engineering:** Full certificate chain management, self-signed CA, device identity certs — deep cryptography/security work beyond typical application development
- **Applied AI in healthcare:** Facial recognition, real-time transcription, clinical summarization — AI in a regulated context with measurable clinical workflow impact
- **Architecture evolution:** CRUD → event-sourced sagas with 60% code reduction — and an honest lesson about the difference between technically correct and operationally sustainable
- **IL Emit metaprogramming:** Runtime code generation for Dapper proxy types — low-level .NET internals knowledge
- **Scale:** 20+ microservice projects across pharmacy, clinical care, device management, and mobile device management domains, concurrent with Call-Trader employment

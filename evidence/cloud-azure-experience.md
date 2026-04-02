---
title: Cloud & Azure Experience
tags: [azure, aws, cloud, azure-functions, service-bus, cognitive-services, container-apps, blob-storage, entra-id, lambda, opentelemetry]
related:
  - projects/taylor-summit.md
  - projects/call-trader-madera.md
  - evidence/healthcare-pharma-domain.md
  - evidence/infrastructure-devops.md
  - evidence/ai-driven-development.md
category: evidence
contact: resume@bryanboettcher.com
---

# Cloud & Azure Experience — Evidence Portfolio

## Overview

Substantial Azure experience spanning Call-Trader/Madera (Jun 2024 – Oct 2025) and Taylor Summit Consulting (2023 – Oct 2025). Taylor Summit work involved 20+ microservice projects across healthcare/pharmaceutical domains with extensive Azure integration.

## Azure Services Used (with evidence from Taylor Summit codebase)

### Identity & Security
- **Azure AD / Entra ID** — Authentication across all Taylor Summit services, MSAL integration
- **SCIM (System for Cross-domain Identity Management)** — Enterprise SSO user provisioning for a regional mental health provider's clinical platform

### Compute
- **App Service** — Web application hosting for API and frontend services
- **Container Apps** — Both Linux and Windows container workloads; self-hosted OpenTelemetry collector on ACA for centralized observability (Madera)
- **Azure Functions** — Event-driven processing at Taylor Summit: crisis session tier escalation (queue-triggered), session cleanup (timer-triggered), reminder scheduling (timer-triggered)

### Data & Messaging
- **Azure SQL** — Managed SQL Server instances across multiple services
- **Service Bus** — MassTransit transport for distributed messaging (clinical care mobile API, medical device management)
- **Blob Storage** — MassTransit MessageData offloading for large payloads, MediSpan drug database file storage with CRC64 hash verification, pharmaceutical document management

### AI & Cognitive Services
- **Azure Cognitive Services Speech** — Real-time speech-to-text transcription during clinical counseling sessions via SignalR streaming
- **Azure OpenAI** — Clinical session summarization with prompt engineering for treatment modality identification and coping skills extraction
- **Application Insights** — Distributed tracing and monitoring across microservices

### Communication
- **Azure Notification Hubs** — Mobile push notifications for clinical patient care platform
- **Azure SignalR Service** — Real-time communication for clinical session management, safety check dashboards
- **SMTP Services** — Email delivery
- **Azure Communication Services** — Telephony and SMS integration

### Networking & Infrastructure
- **VNets, NSGs** — Network security configuration
- **Azure Resource Manager SDK** — Internal tooling for infrastructure provisioning (`Azure.ResourceManager`)

## AWS Services Used (Taylor Summit)

- **Lambda** — EDI (Electronic Data Interchange) processing for healthcare X12 transactions (837/835/270/271) with ReadyToRun publishing for cold-start optimization
- **S3** — EDI file storage and retrieval
- **SFTP** — Secure file transfer for EDI processing

## Patterns Demonstrated

### Production Healthcare Cloud Architecture
- Multi-service Azure deployment across pharmacy, clinical, and device management domains
- Azure Functions for event-driven processing (crisis escalation, session lifecycle, reminders)
- Distributed messaging via Azure Service Bus with MassTransit sagas
- AI/ML services integrated in regulated healthcare context (not generic chatbot work)

### Cloud-Native Observability
- OpenTelemetry with custom `ActivitySource` and `Meter` instrumentation
- Self-hosted OTLP collector on Azure Container Apps
- Application Insights for distributed tracing
- Serilog with MassTransit enrichment for saga-correlated structured logging

### Multi-Cloud
- Primary: Azure (identity, compute, data, messaging, AI)
- Secondary: AWS (Lambda for EDI processing, S3 for file storage)
- Both clouds used in production at Taylor Summit simultaneously

## Concrete Evidence (from Taylor Summit codebase analysis)

The Taylor Summit codebase at `/tmp/taylor-summit/Taylor-Summit/` contains 20+ projects demonstrating the Azure integrations listed above. Key examples:
- Clinical Care API — Azure Cognitive Services Speech + SignalR real-time transcription
- Clinical Care API — Azure AD authentication, Notification Hubs, OpenAI integration
- Clinical Care Mobile API — Azure Functions for crisis session management, Redis caching, Elasticsearch
- Medical Device Management — .NET Aspire 9.3.1 orchestration with Azure Service Bus
- Pharmacy Platform — AWS Lambda for EDI processing
- `Internal.AzureResources/` — Azure Resource Manager SDK for infrastructure provisioning

---
project: KbStore E-Commerce Platform
company: Personal (KB3D RC Hobby Shop)
dates: 2025 – present
role: Sole Developer / Architect
tags: [dotnet, masstransit, angular, ddd, event-driven, postgresql, mongodb, aspire]
---

# KbStore E-Commerce Platform — Project Narrative

## Context

KbStore is a distributed e-commerce platform for an RC hobby shop, designed with Domain-Driven Design and event-driven architecture. It serves as both a real business tool and a showcase of modern .NET distributed systems architecture.

## Architecture

### Bounded Contexts (Polyglot Persistence)
- **Catalog Domain:** Product inventory tracking backed by PostgreSQL (relational data model suits product hierarchies and inventory counts)
- **Storefront Domain:** Customer-facing transactions backed by MongoDB (document model suits order/cart flexibility)
- **ApiService:** HTTP gateway handling cross-domain request routing and orchestration

### Communication
- Domains never call each other directly
- MassTransit over RabbitMQ handles all inter-domain messaging
- Saga state machines orchestrate cross-domain workflows

### Infrastructure
- .NET Aspire for local development orchestration and service discovery
- Docker containerization for all services
- NUnit test suite

## Frontend

**KbClient** (separate repository): Angular 19 backoffice application for product and inventory management.
- Standalone components (Angular 19 pattern)
- TypeScript 5.8 strict mode
- Jest + Mock Service Worker for testing
- Docker + nginx deployment with runtime environment injection

## Significance for Resume

This project demonstrates:
- DDD bounded context decomposition (not just claiming to use DDD, but actually separating domains with different databases)
- Polyglot persistence (PostgreSQL + MongoDB, chosen for data model fit, not just for variety)
- Event-driven integration (MassTransit sagas for cross-domain workflows)
- Modern .NET ecosystem (.NET 9, Aspire, Minimal APIs)
- Full-stack capability (Angular 19 frontend, .NET 9 backend, multi-database persistence)

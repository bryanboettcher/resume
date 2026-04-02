---
title: Angular Service Patterns — Facade Service Composing Multiple Repositories
tags: [angular, typescript, facade-pattern, dependency-injection, rxjs, http, design-patterns]
related:
  - evidence/angular-service-patterns.md
  - evidence/angular-service-patterns-rest-repository.md
  - evidence/angular-service-patterns-observable-composition.md
  - evidence/frontend-web-development.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/angular-service-patterns.md
---

# Angular Service Patterns — Facade Service Composing Multiple Repositories

The `DirectMailService` in the Madera Angular frontend composes eight private repository classes into a single injection point for components. The private repositories are defined in the same file, each a two-line class that inherits from `RestRepository` or `ReportRepository` and sets an API fragment. The facade then delegates to these for CRUD operations and uses direct `HttpClient` calls for domain-specific RPC-style endpoints — a pragmatic split between pattern use and pragmatic deviation.

---

## Evidence: Facade Service Composing Multiple Repositories

**File:** `Madera/madera.ui.client/src/app/services/direct-mail.service.ts` (223 lines)

The `DirectMailService` is the largest service in the application. It composes eight injected repositories into a single facade:

```typescript
export class DirectMailService {
  constructor(
    private importRepo: DirectMailImportRepository,
    private sagaStatusRepo: DirectMailSagaStatusRepository,
    private sagaSummaryRepo: DirectMailSagaSummaryRepository,
    private importLogReportRepository: DirectMailImportLogReportRepository,
    private filePerformanceReportRepository: DirectMailFilePerformanceReportRepository,
    private planningReportRepository: DirectMailPlanningReportRepository,
    private fileRepo: DirectMailFileRepository,
    private fileSummaryRepo: DirectMailFileSummaryRepository,
    private httpClient: HttpClient
  ) { }
}
```

The private repository classes are defined in the same file, each inheriting from `RestRepository` or `ReportRepository` with a single constructor that sets the API fragment:

```typescript
class DirectMailImportRepository extends RestRepository<string, DirectMailImport> {
  constructor(hc: HttpClient) { super(hc); this.setFragment('direct-mail/imports'); }
}
class DirectMailImportLogReportRepository extends ReportRepository<DirectMailImportLogReport, DirectMailImportLogReportParams> {
  constructor(hc: HttpClient) { super(hc); this.setFragment('direct-mail/importlog'); }
}
```

The facade then delegates to these repositories for standard operations and uses direct `HttpClient` calls for one-off endpoints like `populateMailFile()`, `estimateRecipientCount()`, and `restartAddresses()`. This shows a pragmatic split: use the generic repository when the operation fits the CRUD pattern, use direct HTTP for domain-specific RPC-style calls.

## Key Files

- `madera-apps:Madera/madera.ui.client/src/app/services/direct-mail.service.ts` — Facade composing 8 repositories

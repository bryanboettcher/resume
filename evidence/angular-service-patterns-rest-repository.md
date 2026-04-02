---
title: Angular Service Patterns — Generic REST Repository Base Class
tags: [angular, typescript, generics, http, rxjs, dependency-injection, pagination, rest]
related:
  - evidence/angular-service-patterns.md
  - evidence/angular-service-patterns-facade-service.md
  - evidence/angular-service-patterns-caching-dropdown.md
  - evidence/frontend-web-development.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/angular-service-patterns.md
---

# Angular Service Patterns — Generic REST Repository Base Class

The Madera direct mail platform Angular frontend uses a `RestRepository<TKey, TModel>` generic base class as a typed HTTP wrapper that all entity-specific services inherit from. The generic parameter constrains `TModel` to implement `IKeyed<TKey>`, providing standard CRUD plus pagination, automatic JSON/FormData switching based on file detection, and nested repository support for hierarchical REST resources.

---

## Evidence: Generic REST Repository Base Class

**File:** `Madera/madera.ui.client/src/app/services/rest-repository.service.ts`

The `RestRepository<TKey, TModel>` class provides a typed, generic HTTP client wrapper that all entity-specific services inherit from. The generic parameters constrain `TModel` to implement `IKeyed<TKey>`, ensuring every model has an `id` property of the correct key type.

```typescript
export class RestRepository<TKey, TModel extends IKeyed<TKey>> {
  private resourceUrl: string = '';
  constructor(protected readonly httpClient: HttpClient) {}
  protected setFragment(fragment: string) {
    this.resourceUrl = `${environment.apiUrl}/api/${fragment}`;
  }
}
```

The class provides standard CRUD operations (`get`, `getAll`, `create`, `update`, `delete`) plus pagination via `getPaginated()`, which returns `Observable<PaginatedResult<TModel>>`. It also handles file uploads transparently: `create()` and `update()` check whether the model contains `File` instances and automatically switch between JSON POST/PUT and `FormData` multipart uploads:

```typescript
create(model: TModel) {
  return this.containsFile(model)
    ? this.createForm(model)
    : this.httpClient.post(this.resourceUrl, model);
}
```

The `getNestedRepo()` method supports hierarchical REST resources (e.g., `/brokers/{id}/publishers`) by creating a new `RestRepository` instance with a composed URL:

```typescript
public getNestedRepo<TSubModel extends IKeyed<TKey>>(id: TKey, path: string) {
  const r = new RestRepository<TKey, TSubModel>(this.httpClient);
  r.setResourceUrl(`${this.resourceUrl}/${id}/${path}`);
  return r;
}
```

The `PaginatedResult<T>` contract mirrors the server-side C# type (`Madera.UI.Server\Endpoints\PaginatedResult.cs` — the source comment says so explicitly):

```typescript
export interface PaginatedResult<T> {
  results: T[];
  pageNumber: number;
  pageSize: number;
  totalRows: number;
  totalPages: number;
}
```

This means the Angular frontend and .NET backend share the same pagination shape, reducing integration bugs.

A second abstract base class, `ReportRepository<TModel, TParams>`, handles the separate `/reports/` endpoint namespace with typed parameter objects — separating the API namespace convention into the base class rather than leaking it into every call site.

## Key Files

- `madera-apps:Madera/madera.ui.client/src/app/services/rest-repository.service.ts` — Generic CRUD repository with file upload detection
- `madera-apps:Madera/madera.ui.client/src/app/services/report-repository.service.ts` — Generic report repository base class
- `madera-apps:Madera/madera.ui.client/src/app/contracts/paginated-result.ts` — Shared pagination contract mirroring server type

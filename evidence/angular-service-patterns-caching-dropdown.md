---
title: Angular Service Patterns — Caching Dropdown Service with Interface Abstraction
tags: [angular, typescript, rxjs, caching, interface-design, dependency-injection, testing, http]
related:
  - evidence/angular-service-patterns.md
  - evidence/angular-service-patterns-rest-repository.md
  - evidence/angular-service-patterns-string-matching.md
  - evidence/frontend-web-development.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/angular-service-patterns.md
---

# Angular Service Patterns — Caching Dropdown Service with Interface Abstraction

The Madera Angular frontend uses an `IDropdownService` interface with a `CacheDropdownService` implementation that provides in-memory caching for dropdown option lists. The RxJS pipeline normalizes the API response shape (mapping `name` to `value`), caches the transformed result via `tap`, and short-circuits with `of()` on cache hits. The interface means the caching layer can be swapped without touching consumers. A 239-line test suite validates cache-hit/miss behavior using `HttpTestingController`.

---

## Evidence: Caching Dropdown Service with Interface Abstraction

**File:** `Madera/madera.ui.client/src/app/services/idropdown.service.ts`

The `CacheDropdownService` implements an `IDropdownService` interface and provides in-memory caching for dropdown option lists. The cache is a `Map<string, any>` keyed by API URL:

```typescript
export interface IDropdownService {
  getOptions(apiUrl: string): Observable<any>;
}

export class CacheDropdownService implements IDropdownService {
  private cache = new Map<string, any>();

  getOptions(apiUrl: string): Observable<any> {
    if (this.cache.has(apiUrl)) {
      return of(this.cache.get(apiUrl));
    }
    return this.http.get<any>(`${environment.apiUrl}/api/direct-mail/${apiUrl}`).pipe(
      map(response => ({
        ...response,
        results: response.results?.map((item: any) => ({
          id: item.id,
          value: item.name
        })) || []
      })),
      tap(data => this.cache.set(apiUrl, data)),
      catchError(error => { throw error; })
    );
  }
}
```

The RxJS pipeline transforms the API response shape (normalizing `name` to `value` for dropdown display), caches the transformed result via `tap`, and short-circuits with `of()` on cache hits. The interface abstraction (`IDropdownService`) means the caching layer could be swapped without touching consumers.

**File:** `Madera/madera.ui.client/src/app/services/idropdown.service.spec.ts` (239 lines)

The corresponding test suite validates caching behavior thoroughly: cache miss triggers HTTP, cache hit returns without HTTP, different endpoints cache separately, and failed requests do not populate the cache. Tests use `HttpTestingController` to verify exact request counts and URLs.

## Key Files

- `madera-apps:Madera/madera.ui.client/src/app/services/idropdown.service.ts` — Caching dropdown service with interface
- `madera-apps:Madera/madera.ui.client/src/app/services/idropdown.service.spec.ts` — Cache behavior test suite

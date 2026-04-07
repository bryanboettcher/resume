---
title: Angular Service Patterns — Observable Composition and State-Guarded Operations
tags: [angular, typescript, rxjs, switchmap, observable, state-machine, validation, http]
related:
  - evidence/angular-service-patterns.md
  - evidence/angular-service-patterns-facade-service.md
  - evidence/angular-service-patterns-string-matching.md
  - evidence/angular-service-patterns-auth-lifecycle.md
  - evidence/frontend-web-development.md
  - projects/call-trader-madera.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/angular-service-patterns.md
---

# Angular Service Patterns — Observable Composition and State-Guarded Operations

The Madera Angular frontend chains multi-step server interactions into single subscribable streams using RxJS `switchMap`. The import creation flow POSTs metadata, then uses the correlation ID from the response to upload the file — two HTTP operations composed into one Observable. The `DirectMailFileService` adds a `restrictToStates()` guard that reflects the backend saga state machine: invalid operations surface a toast rather than making the API call.

---

## Evidence: Multi-Step Observable Composition for File Imports

**File:** `Madera/madera.ui.client/src/app/services/direct-mail-import.service.ts`

The `DirectMailImportService` chains HTTP operations using RxJS `switchMap`. Creating an import is a two-step process: POST the metadata, then upload the file using the correlation ID from the first response:

```typescript
createImport(payload: CreateImportPayload, file: File): Observable<any> {
  return this.httpClient.post<any>(this.apiUri, payload).pipe(
    switchMap(response => {
      return this.uploadImport(response.correlationId, file);
    })
  );
}
```

The `CreateImportPayload` class includes domain-specific validation:

```typescript
isValid(): boolean {
  return Boolean(this.publisher && this.broker && this.vertical && this.filename && this.transformScript && this.sourceFileType);
}
```

The `UpdateImportPayload` exposes `static getPropertyNames()` for dynamic property iteration, and both payload classes extend a `Payload` base class that provides index-signature access (`[key: string]: any`) and a generic `validate()` method.

---

## Evidence: State-Guarded Operations

**File:** `Madera/madera.ui.client/src/app/services/direct-mail-file.service.ts`

The `DirectMailFileService` includes a `restrictToStates()` method that guards operations based on saga state:

```typescript
restrictToStates(sagaStates: string[], currentState: string, callback: () => void): void {
  if (sagaStates.includes(currentState)) {
    callback();
  } else {
    this.toastService.text(`Can only perform during ${sagaStates}, current state is ${currentState}`);
  }
}
```

This enforces business rules at the service layer: certain operations (populating a mail file, generating output) are only valid in specific saga states. Invalid attempts surface a user-facing toast message rather than failing silently or crashing. This is the Angular frontend reflecting the same saga state machine that runs on the MassTransit backend.

The file also handles CSV file download with explicit content type negotiation:

```typescript
getMailFileOutput(fileId: string, mailHouseName: string) {
  const headers = new HttpHeaders({ 'Accept': 'text/csv' });
  return this.httpClient.get(uri, { headers, params, responseType: 'blob' });
}
```

## Key Files

- `madera-apps:Madera/madera.ui.client/src/app/services/direct-mail-import.service.ts` — Multi-step import with switchMap
- `madera-apps:Madera/madera.ui.client/src/app/services/direct-mail-file.service.ts` — State-guarded file operations

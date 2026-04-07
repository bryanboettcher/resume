---
title: Testing Infrastructure — Playwright E2E Tests and Angular Unit Tests
tags: [testing, playwright, e2e, angular, jest, typescript, accessibility, aria, keyboard-navigation, kb-platform]
related:
  - evidence/testing-infrastructure.md
  - evidence/testing-infrastructure-bdd-organization.md
  - projects/kbstore-ecommerce.md
category: evidence
contact: resume@bryanboettcher.com
parent: evidence/testing-infrastructure.md
---

# Testing Infrastructure — Playwright E2E Tests and Angular Unit Tests

The Angular AdminUI frontend in kb-platform has extensive Playwright E2E coverage for modal dialog behavior (including accessibility) and Jest-based unit tests for services and components.

---

## Evidence: Playwright E2E Tests (kb-platform AdminUI)

The Angular AdminUI frontend includes Playwright E2E tests under `src/frontends/AdminUI/e2e/`. Three test files cover the modal dialog system:

**`dialog/product-delete.spec.ts`** (267 lines) — Tests the complete product deletion workflow: opening the confirmation dialog, canceling, confirming, Escape key dismissal, backdrop click dismissal, verifying the dialog doesn't close when modal content is clicked, mobile viewport responsiveness (375x667), API error handling with route mocking, and sequential delete operations.

**`dialog/dialog-accessibility.spec.ts`** (387 lines) — Dedicated accessibility testing: ARIA attributes (`role="dialog"`, `aria-modal`, `aria-labelledby`, `aria-describedby`, `role="presentation"` on backdrop), keyboard navigation (Tab/Shift+Tab cycling, Space/Enter activation, Escape dismissal), focus trapping (verifying focus stays within dialog via `el.closest('[role="dialog"]')` evaluation), focus restoration to the trigger element after dialog close, and visual focus indicator verification via computed styles.

**`dialog/dialog-edge-cases.spec.ts`** (391 lines) — Race condition and boundary testing: rapid double-clicks on confirm/cancel buttons, opening multiple dialogs simultaneously, backdrop clicks during animation, and sequential open/close cycles.

The E2E tests use Playwright's `page.route()` for API mocking and `page.setViewportSize()` for responsive testing. Test organization follows Playwright's `test.describe()` nesting.

---

## Evidence: Angular Unit Tests

The AdminUI also includes Jest-based unit tests (`*.spec.ts` files co-located with source). `product.service.spec.ts` (637 lines) tests the `ProductService` with mocked `HttpClient` and `NGXLogger`, verifying HTTP call parameters and error handling. The `product-list.component.spec.ts` (851 lines) is the largest Angular test file, covering component initialization, filtering, pagination, and state management.

---

## Key Files

- `kb-platform:src/frontends/AdminUI/e2e/dialog/product-delete.spec.ts` — Playwright product delete E2E workflow
- `kb-platform:src/frontends/AdminUI/e2e/dialog/dialog-accessibility.spec.ts` — Playwright ARIA and keyboard accessibility tests
- `kb-platform:src/frontends/AdminUI/e2e/dialog/dialog-edge-cases.spec.ts` — Playwright race condition and boundary tests

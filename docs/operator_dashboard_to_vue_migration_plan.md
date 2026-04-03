# Tycoon.OperatorDashboard -> Tycoon.OperatorDashboard.Vue Migration Plan

Date: 2026-04-02
Owner: TBD

## Goal
Move operator capabilities currently in `Tycoon.OperatorDashboard` (Blazor) into `Tycoon.OperatorDashboard.Vue` in phased, low-risk increments.

## Current gap summary
Vue currently covers a subset of operator surfaces (dashboard/users/moderation/anti-cheat/economy/transactions/notifications/security/escalations/season points).
Blazor still contains additional operational workflows and mature page behaviors.

## Migration principles
1. API-contract first (no hidden server behavior in UI code).
2. Page-by-page parity with acceptance criteria.
3. Keep Blazor as fallback until Vue feature parity is signed off.
4. Instrument each migrated page with analytics/error telemetry.

## Proposed phases

### Phase 0 — Inventory + acceptance criteria
- Build a page/feature matrix:
  - Blazor page
  - Vue equivalent page
  - status: `parity`, `partial`, `missing`
- Define acceptance checks per page:
  - route works
  - data loads
  - create/update flows work
  - error envelope handling
  - auth/role behavior

### Phase 1 — Shared contract hardening
- Ensure all UI actions map to typed DTOs in `Tycoon.Shared.Contracts`.
- Remove implicit assumptions in UI by documenting request/response schemas.
- Add API contract snapshots for high-risk admin endpoints.

### Phase 2 — Navigation and IA parity
- Align Vue navigation sections with Blazor information architecture.
- Add missing placeholders/routes in Vue for pages that are still Blazor-only.
- Keep labels and terminology consistent across both dashboards.

### Phase 3 — Feature migration batches
Batch A (high-value daily ops):
- Users
- Moderation
- Economy
- Anti-cheat

Batch B (content + lifecycle):
- Questions
- Events/Seasons
- Notifications

Batch C (advanced/admin):
- Security audit
- Powerups/skills
- Match/admin diagnostics

Each batch done only when:
- API integration tests pass
- route smoke checks pass
- operator UAT sign-off completed

### Phase 4 — Test automation and CI gates
- Add Vue UI smoke tests for critical routes.
- Add API-backed scenario tests for operator write actions.
- Keep `alpha-p0-smoke` route-check gate and extend with live mode in CI once environment services are available.

### Phase 5 — Cutover + deprecation
- Enable Vue as primary operator UI.
- Keep Blazor behind fallback flag for one release window.
- Remove Blazor pages after stability period and telemetry review.

## Deliverables checklist
- [ ] Feature parity matrix doc
- [ ] Route parity map (`Blazor route` -> `Vue route`)
- [ ] API contract diff report
- [ ] UAT checklist per batch
- [ ] Cutover runbook and rollback plan

## Risks and mitigations
- Risk: hidden Blazor-only business logic.
  - Mitigation: move logic to API and test via contracts.
- Risk: role/auth drift between UIs.
  - Mitigation: explicit auth tests for admin scopes per page.
- Risk: inconsistent labels and operator confusion.
  - Mitigation: single terminology table enforced in both UIs.

## Immediate next actions
1. Create parity matrix doc from current routes/pages.
2. Select Batch A pages and define “done” criteria.
3. Implement one migrated page end-to-end (pilot), then scale pattern.

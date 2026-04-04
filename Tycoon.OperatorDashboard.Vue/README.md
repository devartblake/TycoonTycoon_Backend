# Tycoon.OperatorDashboard.Vue

Vue front-end target for Operator Dashboard migration.

## Phase-1 bootstrap (in progress)
- [x] App shell + router scaffold
- [x] Initial Wave-A routes: Dashboard, Audit Log, Users
- [x] RBAC route guard helper skeleton
- [x] Session bootstrap integration (`/api/me`) for route guard permission checks
- [x] Shared API client conventions (`src/lib/apiClient.js`, `src/api/*`)
- [ ] Error envelope normalization

## NPM scripts
- `npm run dev`
- `npm run build`
- `npm run preview`
- `npm run test`
- `npm run lint`

## Notes
This directory is intentionally lightweight in the initial migration commit.
Do not commit `node_modules`.

## Current Status (April 4, 2026)
- Wave A shell pages are API-backed via:
  - `/api/dashboard/overview`
  - `/api/audit-log`
  - `/api/users`
- Router guard now resolves permissions via `/api/me` session bootstrap.
- Audit Log and Users now render table/paging views from API payloads.
- Dashboard now renders KPI-style cards from overview payload.
- Audit Log status filter and Users search/ban filters are now wired to Wave A API query params.
- Users ban/unban action workflow is now wired via BFF (`/api/users/{id}/ban|unban`).
- Users action workflow now supports operator-provided ban reason and in-flight action button state.
- Next: add moderation/economy Wave A action workflows and richer table UX polish.

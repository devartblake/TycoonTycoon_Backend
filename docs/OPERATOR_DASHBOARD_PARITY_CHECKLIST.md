# Operator Dashboard Parity Checklist (Django vs Legacy) — April 8, 2026

## Goal

Validate critical operator workflows before hard cutover from `operator-dashboard-blazor` to Django `operator-dashboard`.

## Core Workflow Parity

- [x] Login/logout flow (`/login`, `/logout`)
- [x] Aggregated health/status view (`/`, `/api/operator/health`)
- [x] Users triage page with filters/sorting/pagination
- [x] Users bulk actions with guardrails (`dry-run`, explicit confirmation)
- [x] Moderation logs view + CSV export
- [x] Security audit view + CSV export
- [x] Media intent workflow page
- [x] MinIO diagnostics view
- [x] Store flash sales view + cancel action (`/store/flash-sales`)
- [x] Store stock policies view with SKU/active filters (`/store/stock-policies`)
- [x] Store purchase analytics with date-range filter (`/store/analytics`)

## Missing — Blazor Workflows Not Yet in Django (Wave B/C)

- [ ] Questions list / approve / reject / bulk actions (`Questions.razor`)
- [ ] Game events create / manage (`Events.razor`)
- [ ] Seasons lifecycle management (`Seasons.razor`)
- [ ] Economy / coin grant / reward adjustments (`Economy.razor`)
- [ ] Anti-cheat review queue (`AntiCheat.razor`)
- [ ] Notifications send / schedule / dead-letter (`Notifications.razor`)
- [ ] Player stock overrides + bulk reset (`/admin/store/player-stock/*`)

## Safety/Operations

- [x] Permission-gated routes and API responses
- [x] Request/session expiration refresh handling
- [x] Incident runbook in place (`docs/OPERATOR_DASHBOARD_INCIDENT_RUNBOOK.md`)
- [x] Legacy fallback service remains available in compose as `operator-dashboard-blazor`

## Release Gates

- [ ] Execute one full parallel-run validation in staging with real operator accounts.
- [ ] Capture and attach operator sign-off notes.
- [ ] Confirm rollback drill execution timestamp in release notes.

## Status Update — April 28, 2026

- ✅ Store section added: Flash Sales, Stock Policies, Purchase Analytics (backed by admin store P2 endpoints).
- ✅ Parity checklist updated with Wave B/C gap matrix.
- ⚠️ Cutover risk assessment created: `docs/OPERATOR_DASHBOARD_CUTOVER_RISK_2026-04-28.md`.
- ⚠️ Parallel-run sign-off still outstanding — must complete before May 15 hard cutover.
- 🚧 Wave B (Questions, Events, Seasons) and Wave C (Economy, Anti-cheat, Notifications) not started.

## Status Update — April 8, 2026

- ✅ Auth header/key parity landed between Django and Blazor clients (`ADMIN_OPS_HEADER`, `AdminOps__Key` fallback).
- ✅ Auth-client test coverage expanded for custom ops-header behavior.
- 🚧 Staging parallel-run kickoff document created (`docs/OPERATOR_PARALLEL_RUN_STAGING_2026-04-08.md`).
- 🚧 Parallel-run evidence artifact initialized (`docs/OPERATOR_PARALLEL_RUN_EVIDENCE_2026-04-08.md`).
- ⚠️ Parallel-run sign-off and rollback-drill artifacts are still outstanding release gates.

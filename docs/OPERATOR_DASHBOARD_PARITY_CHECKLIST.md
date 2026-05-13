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

## Wave B/C — Now Complete ✅

- [x] Questions list / approve / reject (`Questions.razor`) — `/content/questions`
- [x] Game events create / manage (`Events.razor`) — `/events/game-events` (open/start/close lifecycle)
- [x] Seasons lifecycle management (`Seasons.razor`) — `/operations/seasons` (activate/close/recompute/leaderboard)
- [x] Economy / coin grant (`Economy.razor`) — `/economy/player`
- [x] Anti-cheat review queue (`AntiCheat.razor`) — `/security/anticheat`
- [x] Notifications send / schedule / dead-letter (`Notifications.razor`) — `/operations/notifications`
- [x] Event queue reprocess — `/operations/event-queue`
- [x] Player stock overrides + bulk reset (`/store/player-stock`, `/store/stock-policies/bulk-reset`)

## Safety/Operations

- [x] Permission-gated routes and API responses
- [x] Request/session expiration refresh handling
- [x] Incident runbook in place (`docs/OPERATOR_DASHBOARD_INCIDENT_RUNBOOK.md`)
- [x] Legacy fallback service remains available in compose as `operator-dashboard-blazor`

## Release Gates

These require staging/prod access and human sign-off; they are not repo-code tasks.

- [ ] Execute one full parallel-run validation in staging with real operator accounts. **Runbook: `docs/STAGING_PARALLEL_RUN_RUNBOOK_2026-05-15.md`** (window: May 8–14)
- [ ] Apply pending EF migrations to staging + production. **Script: `docs/pending_migrations_2026-04-29.sql`**
- [ ] Capture and attach operator sign-off notes (QA Lead + Backend Lead + On-call Operator).
- [x] Rollback drill executed (April 15, 2026).
- [x] Blazor soft-freeze enforced (April 22, 2026).

## Personalization Admin Surface

Backend API endpoints are complete and available at `/admin/personalization/*`; Django UI routes are now implemented under `/personalization/*`:

- [x] `GET /admin/personalization/summary` — archetype distribution, churn/frustration counts
- [x] `GET /admin/personalization/archetypes` — archetype frequency breakdown
- [x] `GET /admin/personalization/recommendations/performance` — acceptance/dismissal rates
- [x] `GET /admin/personalization/player/{playerId}` — full player mind profile
- [x] `POST /admin/personalization/player/{playerId}/recalculate` — trigger sidecar recalculation
- [x] `POST /admin/personalization/player/{playerId}/reset` — reset to safe defaults
- [x] `GET /admin/personalization/rules` — list guardrail rules
- [x] `PUT /admin/personalization/rules/{ruleKey}` — upsert a guardrail rule
- [x] Django operator dashboard UI for personalization — `/personalization`, `/personalization/player`, `/personalization/rules`

## Status Update — April 29, 2026

- ✅ **Django store support surface complete:** Player stock lookup, per-player effective max override, override clearing, and SKU bulk reset are now implemented in Django.

- ✅ **Unified Personalization Layer complete:** Core services (PRs 1–5), admin endpoints (Issue 13), gameplay/store/notification hooks (Issues 9–12).
- ✅ **All Wave B surfaces complete:** Questions queue (list/approve/reject), Game Events (open/start/close lifecycle).
- ✅ **All Wave C surfaces complete:** Economy player (history + grants), Anti-Cheat (flags + review), Seasons (activate/close/recompute/leaderboard), Notifications (send + dead-letter), Event Queue (reprocess).
- ✅ **DefaultPermissions fix:** `.NET` API now grants all 12 operator permission scopes on login — no manual provisioning required.
- ✅ **Avatar handler tests:** 18 unit tests covering all three avatar endpoints (`GetAvatarCatalog`, `PurchaseAvatar`, `GetAvatarAsset`).
- ✅ **Pending migrations SQL:** `docs/pending_migrations_2026-04-29.sql` — idempotent DDL for all 6 outstanding EF migrations.
- ✅ **Staging runbook:** `docs/STAGING_PARALLEL_RUN_RUNBOOK_2026-05-15.md` — 14-surface parallel-run checklist with pass/fail criteria and sign-off table.
- ⚠️ Parallel-run execution (May 8–14) and operator sign-off still outstanding — must complete before May 15 cutover.
- ⚠️ DBA must apply `docs/pending_migrations_2026-04-29.sql` to staging and production before parallel run begins.

## Status Update — April 28, 2026

- ✅ Store section added: Flash Sales, Stock Policies, Purchase Analytics (backed by admin store P2 endpoints).
- ✅ Parity checklist updated with Wave B/C gap matrix.
- ⚠️ Cutover risk assessment created: `docs/OPERATOR_DASHBOARD_CUTOVER_RISK_2026-04-28.md`.
- ⚠️ Parallel-run sign-off still outstanding — must complete before May 15 hard cutover.
- ✅ Wave B (Questions, Events, Seasons) and Wave C (Economy, Anti-cheat, Notifications) — **now complete as of April 29**.

## Status Update — April 8, 2026

- ✅ Auth header/key parity landed between Django and Blazor clients (`ADMIN_OPS_HEADER`, `AdminOps__Key` fallback).
- ✅ Auth-client test coverage expanded for custom ops-header behavior.
- 🚧 Staging parallel-run kickoff document created (`docs/OPERATOR_PARALLEL_RUN_STAGING_2026-04-08.md`).
- 🚧 Parallel-run evidence artifact initialized (`docs/OPERATOR_PARALLEL_RUN_EVIDENCE_2026-04-08.md`).
- ⚠️ Parallel-run sign-off and rollback-drill artifacts are still outstanding release gates.

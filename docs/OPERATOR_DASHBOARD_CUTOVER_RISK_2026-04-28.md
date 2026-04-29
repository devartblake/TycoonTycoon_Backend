# Operator Dashboard Cutover Risk Assessment — 2026-04-28

> **Target deprecation date:** May 15, 2026
> **Blazor soft-freeze:** April 22, 2026 (already passed — no new Blazor changes)
> **Rollback window:** through June 12, 2026

---

## Executive Summary

**Updated: 2026-04-29** — Wave B/C implementation is complete. The Django dashboard now covers all Blazor operator surfaces except player stock overrides (intentionally deferred — support-only, low operational impact). The May 15 hard cutover is on track. All remaining blockers are operational (parallel-run execution, migrations apply, sign-off) rather than code gaps.

**Recommendation:** Proceed with **full cutover on May 15** — Django as the sole operator UI. Blazor fallback container remains warm through June 12 per the rollback window. Execute staging parallel run May 8–14 using `docs/STAGING_PARALLEL_RUN_RUNBOOK_2026-05-15.md` and collect sign-off before flipping nginx.

---

## Feature Gap Matrix

### Covered by Django (cutover safe)

| Surface | Django URL | Backend endpoint | Notes |
|---------|-----------|-----------------|-------|
| Health / command center | `/` | `/api/operator/health` | ✅ Live |
| Users triage | `/users` | `/admin/users/*` | ✅ Live — sort, filter, bulk actions, saved views |
| Moderation logs | `/moderation/logs` | `/admin/moderation/*` | ✅ Live — CSV export |
| Security audit | `/audit/security` | `/admin/audit/*` | ✅ Live — CSV export |
| Media intents | `/media/intent` | `/admin/media/*` | ✅ Live |
| MinIO diagnostics | `/minio/diagnostics` | N/A | ✅ Live |
| Flash sales | `/store/flash-sales` | `/admin/store/flash-sales` | ✅ **Added 2026-04-28** |
| Stock policies | `/store/stock-policies` | `/admin/store/stock-policies` | ✅ **Added 2026-04-28** |
| Purchase analytics | `/store/analytics` | `/admin/store/analytics/purchases` | ✅ **Added 2026-04-28** |

### Added in Wave B/C — 2026-04-29 (cutover safe)

| Surface | Django URL | Backend endpoint | Notes |
|---------|-----------|-----------------|-------|
| Questions queue | `/content/questions` | `/admin/questions/*` | ✅ **Added 2026-04-29** — list, approve, reject |
| Game events | `/events/game-events` | `/admin/game-events/*` | ✅ **Added 2026-04-29** — list, open, start, close |
| Seasons | `/operations/seasons` | `/admin/seasons/*` | ✅ **Added 2026-04-29** — list, activate, close, recompute, leaderboard |
| Economy player | `/economy/player` | `/admin/economy/*` | ✅ **Added 2026-04-29** — history + coin grant |
| Anti-cheat flags | `/security/anticheat` | `/admin/anti-cheat/*` | ✅ **Added 2026-04-29** — list, review |
| Notifications | `/operations/notifications` | `/admin/notifications/*` | ✅ **Added 2026-04-29** — send, history, dead-letter replay |
| Event queue | `/operations/event-queue` | `/admin/event-queue/*` | ✅ **Added 2026-04-29** — reprocess |

### Intentionally deferred (not blocking cutover)

| Surface | Blazor page | Backend endpoint | Risk |
|---------|------------|-----------------|------|
| Player stock overrides | N/A | `/admin/store/player-stock/*` | LOW — support-only workflow, deferred |
| Bulk stock reset | N/A | `/admin/store/stock-policies/bulk-reset` | LOW |
| Reward claim limits | N/A | `/admin/store/reward-limits/*` | LOW |

---

## Release Gate Status

| Gate | Status | Blocker |
|------|--------|---------|
| Wave B/C Django surfaces | ✅ **Complete — 2026-04-29** | — |
| DefaultPermissions fix (.NET) | ✅ **Complete — 2026-04-29** | — |
| Pending migrations SQL script | ✅ **Ready — `docs/pending_migrations_2026-04-29.sql`** | DBA must apply |
| Staging parallel-run runbook | ✅ **Ready — `docs/STAGING_PARALLEL_RUN_RUNBOOK_2026-05-15.md`** | — |
| Staging parallel-run with real operator accounts | ⚠️ **Pending** | Window: May 8–14 |
| Operator sign-off notes captured | ⚠️ **Pending** | Waiting on parallel-run execution |
| Pending migrations applied to staging/prod | ⚠️ **Pending** | DBA action required before parallel run |
| Rollback drill executed | ✅ Complete (April 15, 2026) | — |
| CI pipeline (lint + tests) | ✅ Complete | — |
| Auth / session hardening | ✅ Complete | — |
| Blazor soft-freeze enforced | ✅ April 22 passed | — |

---

## Risks

### R1 — Content team loses Questions workflow on cutover ✅ RESOLVED
Django Questions page is live at `/content/questions` — list, approve, reject. Wave B complete 2026-04-29.

### R2 — Parallel-run sign-off not completed before cutover (OPEN)
The parity checklist requires a full staging parallel-run with real operator accounts before hard cutover. Gate still open — execution window is May 8–14.

**Mitigation:** Runbook in place at `docs/STAGING_PARALLEL_RUN_RUNBOOK_2026-05-15.md`. Schedule 2-hour session with operators before May 8.

### R3 — Economy/reward workflows have no Django UI ✅ RESOLVED
Django Economy page is live at `/economy/player` — player lookup, transaction history, coin grant. Wave C complete 2026-04-29.

### R4 — Permission scopes not in operator role definitions ✅ RESOLVED
`AdminAuthEndpoints.cs` `DefaultPermissions` array updated 2026-04-29 to include all 12 scopes: `users`, `questions`, `events`, `store`, `economy`, `anticheat`, `notifications`, `seasons`, `eventqueue` (read + write each). Operators receive full permissions on next login.

### R5 — Vue project creates confusion about the migration target ✅ RESOLVED
`DEPRECATED.md` added to `Tycoon.OperatorDashboard.Vue` and `Tycoon.OperatorDashboard.Web`. Django is the canonical path.

---

## Recommended Immediate Actions (Priority Order)

1. **Apply `docs/pending_migrations_2026-04-29.sql`** to staging and production — DBA action required before parallel run begins.
2. **Schedule staging parallel-run** (May 8–14) — block 2 hours with operators using `docs/STAGING_PARALLEL_RUN_RUNBOOK_2026-05-15.md`. Required before May 15 cutover.
3. **Collect operator sign-off** — QA Lead, Backend Lead, On-call Operator must sign the runbook's sign-off table before May 15.
4. **Flip nginx upstream on May 15** — switch from Blazor port to Django port. Blazor stays warm through June 12 as the rollback target.

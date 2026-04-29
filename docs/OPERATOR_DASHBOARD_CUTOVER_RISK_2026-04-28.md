# Operator Dashboard Cutover Risk Assessment — 2026-04-28

> **Target deprecation date:** May 15, 2026
> **Blazor soft-freeze:** April 22, 2026 (already passed — no new Blazor changes)
> **Rollback window:** through June 12, 2026

---

## Executive Summary

The Django dashboard is the default Docker Compose service and has passed a staging rollback drill. However, **feature parity with Blazor is materially incomplete**. The May 15 deprecation date is achievable only for the operator surfaces Django already covers. Cutting over the full Blazor surface on that date without completing Wave B/C workflows would force operators to context-switch between two UIs or lose access to critical workflows entirely.

**Recommendation:** Proceed with a **partial cutover on May 15** — Django as default for covered surfaces, Blazor fallback flag retained for uncovered ones. Retire Blazor fully only after Wave B/C are complete (estimated 3–4 weeks post–May 15).

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

### Not yet in Django (cutover blocked)

| Surface | Blazor page | Backend endpoint | Risk if missing |
|---------|------------|-----------------|----------------|
| Questions CRUD | `Questions.razor` | `/admin/questions/*` | **HIGH** — daily ops for content team |
| Questions import/bulk approve | `Questions.razor` | `/admin/questions/bulk-*` | **HIGH** |
| Game events create/manage | `Events.razor` | `/admin/game-events/*` | HIGH |
| Seasons lifecycle | `Seasons.razor` | `/admin/seasons/*` | MEDIUM |
| Economy / reward adjustments | `Economy.razor` | `/admin/economy/*` | HIGH |
| Anti-cheat review | `AntiCheat.razor` | `/admin/anti-cheat/*` | MEDIUM |
| Notifications send/schedule | `Notifications.razor` | `/admin/notifications/*` | MEDIUM |
| Dead-letter queue viewer | `Notifications.razor` | `/admin/event-queue/*` | LOW (ops-only) |
| Player stock overrides | N/A | `/admin/store/player-stock/*` | LOW (support-only) |
| Bulk stock reset | N/A | `/admin/store/stock-policies/bulk-reset` | LOW |
| Reward claim limits | N/A | `/admin/store/reward-limits/*` | LOW |

---

## Release Gate Status

| Gate | Status | Blocker |
|------|--------|---------|
| Staging parallel-run with real operator accounts | ⚠️ **Incomplete** | Sign-off evidence artifact initialized but not filled in |
| Operator sign-off notes captured | ⚠️ **Incomplete** | Waiting on parallel-run execution |
| Rollback drill executed | ✅ Complete (April 15, 2026) | — |
| CI pipeline (lint + tests) | ✅ Complete | — |
| Auth / session hardening | ✅ Complete | — |
| Blazor soft-freeze enforced | ✅ April 22 passed | — |

---

## Risks

### R1 — Content team loses Questions workflow on cutover (HIGH)
Questions management is a daily operation. If Blazor is shut off on May 15 without a Django Questions page, content operators have no UI path to approve/reject questions or import question sets.

**Mitigation:** Keep Blazor fallback container running (`operator-dashboard-blazor`) and document the fallback URL in the runbook. Begin Questions Wave B implementation immediately — it's the highest-priority Django page to add.

### R2 — Parallel-run sign-off not completed before cutover (HIGH)
The parity checklist requires a full staging parallel-run with real operator accounts before hard cutover. This gate is currently open.

**Mitigation:** Schedule the parallel-run session in the next 5 business days. Required: at least one operator from content team and one from support to walk through their daily workflows on Django and sign off.

### R3 — Economy/reward workflows have no Django UI (HIGH)
Operators currently use the Blazor economy page for one-off coin grants, transaction reversals, and reward cap changes. These are support-critical paths.

**Mitigation:** Retain Blazor fallback. Add Django economy page as Wave C priority 1.

### R4 — `store:read` / `store:write` permissions not yet in operator role definitions (MEDIUM)
The new store pages require `store:read` and `store:write` permissions. These permission strings need to be added to the admin role definitions in the .NET API and reflected in operator session tokens.

**Mitigation:** Confirm with backend team that these permission strings are provisioned. If not, the store pages will return 403 for all operators until resolved. Short-term workaround: relax permission check to `users:read` as a bootstrap measure.

### R5 — Vue project creates confusion about the migration target (LOW)
`Tycoon.OperatorDashboard.Vue` and `Tycoon.OperatorDashboard.Web` are still in the repo and have partial Wave A implementation. This creates ambiguity about which project is canonical.

**Mitigation:** Add a `DEPRECATED.md` to the Vue and Web projects making clear that Django is the chosen path, and update `REMAINING_TASKS.md` accordingly.

---

## Recommended Immediate Actions (Priority Order)

1. **Schedule staging parallel-run** — block 2 hours with operators within 5 business days. Required before May 15 hard cutover.
2. **Implement Django Questions page** (Wave B P1) — list, approve/reject, filter by status. Uses existing `/admin/questions/*` endpoints.
3. **Confirm `store:read`/`store:write` permission provisioning** with backend team before store pages go live.
4. **Mark Vue/Web projects deprecated** — add `DEPRECATED.md` to both projects to avoid confusion.
5. **Implement Django Economy page** (Wave C P1) — at minimum: player balance view and single-player coin grant.
6. **Update cutover plan** to reflect partial cutover on May 15 (covered surfaces only) with Blazor fallback retained until Wave B/C complete.

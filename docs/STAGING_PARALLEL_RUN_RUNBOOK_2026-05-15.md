# Staging Parallel-Run Runbook — May 15 Blazor → Django Cutover

**Target cutover date:** 2026-05-15  
**Parallel-run window:** 2026-05-08 → 2026-05-14 (complete before cutover)  
**Rollback window:** 2026-05-15 → 2026-06-12 (Blazor kept warm)

---

## Prerequisites

Before starting the parallel run, verify all gates are clear:

| Gate | Owner | Status |
|------|-------|--------|
| Pending EF migrations applied to staging | Backend / DevOps | See `docs/pending_migrations_2026-04-29.sql` |
| Django dashboard deployed to staging | DevOps | `docker-compose up operator-dashboard` |
| Blazor dashboard deployed to staging | DevOps | Keep running on alternate port |
| Two real operator accounts provisioned in staging | Backend | Both must exist in `admin_email_acl` table |
| Both operator accounts have logged in once (permissions cached) | QA | Login via Django `/login` |

---

## Session Setup

1. Open Django dashboard in browser: `https://operator-staging.synaptix.internal/`
2. Log in with **Operator A** credentials (full permissions).
3. Open Blazor dashboard in a second browser window (or incognito).
4. Log in to Blazor with the same account.

For each test below, perform the action in both dashboards and compare results. Flag any discrepancy.

---

## Test Surfaces

### 1. Auth & Permissions

| Check | Django | Blazor | Match? |
|-------|--------|--------|--------|
| Login succeeds | ☐ | ☐ | ☐ |
| Profile email shown in sidebar footer | ☐ | ☐ | ☐ |
| All nav links visible (no 403 on any surface) | ☐ | n/a | ☐ |
| Logout clears session | ☐ | ☐ | ☐ |

**Permission verification:** After login, inspect the session profile in Django dev tools (`/api/operator/health` → verify `permissions` array contains all expected scopes: `users:read`, `store:read`, `economy:read`, `questions:read`, `events:read`, `anticheat:read`, `seasons:read`, `notifications:read`).

---

### 2. Command Center (Health)

| Check | Django | Blazor | Match? |
|-------|--------|--------|--------|
| All service tiles render | ☐ | ☐ | ☐ |
| Health statuses match | ☐ | ☐ | ☐ |

---

### 3. Users

| Check | Django | Blazor | Match? |
|-------|--------|--------|--------|
| User list loads | ☐ | ☐ | ☐ |
| Filter by email partial works | ☐ | ☐ | ☐ |
| Filter by `banned=true` returns same set | ☐ | ☐ | ☐ |
| Ban a test user | ☐ | ☐ | ☐ |
| Unban the same user | ☐ | ☐ | ☐ |
| User detail panel shows activity | ☐ | ☐ | ☐ |

---

### 4. Moderation

| Check | Django | Blazor | Match? |
|-------|--------|--------|--------|
| Moderation log renders | ☐ | ☐ | ☐ |
| Filter by date range narrows results | ☐ | ☐ | ☐ |
| Player moderation profile loads | ☐ | ☐ | ☐ |
| Set moderation status succeeds | ☐ | ☐ | ☐ |

---

### 5. Security Audit

| Check | Django | Blazor | Match? |
|-------|--------|--------|--------|
| Audit log loads | ☐ | ☐ | ☐ |
| Filter by event type works | ☐ | ☐ | ☐ |

---

### 6. Questions Queue

| Check | Django | Blazor | Match? |
|-------|--------|--------|--------|
| Pending questions list renders | ☐ | ☐ | ☐ |
| Approve a question → status changes to Approved | ☐ | ☐ | ☐ |
| Reject a question → status changes to Rejected | ☐ | ☐ | ☐ |
| Filter by status=Approved shows only approved | ☐ | ☐ | ☐ |

---

### 7. Economy — Player

| Check | Django | Blazor | Match? |
|-------|--------|--------|--------|
| Player lookup by UUID loads history | ☐ | ☐ | ☐ |
| Transaction history shows correct amounts | ☐ | ☐ | ☐ |
| Grant 10 coins to test player | ☐ | ☐ | ☐ |
| New transaction appears in history | ☐ | ☐ | ☐ |
| Deduction of 5 coins succeeds | ☐ | ☐ | ☐ |

---

### 8. Store

| Check | Django | Blazor | Match? |
|-------|--------|--------|--------|
| Flash sales list renders | ☐ | ☐ | ☐ |
| Cancel a flash sale (use test promo) | ☐ | ☐ | ☐ |
| Stock policies list renders | ☐ | ☐ | ☐ |
| Filter stock policies by SKU | ☐ | ☐ | ☐ |
| Purchase analytics loads with date range | ☐ | ☐ | ☐ |

---

### 9. Game Events

| Check | Django | Blazor | Match? |
|-------|--------|--------|--------|
| Event list renders | ☐ | ☐ | ☐ |
| Filter by status=Scheduled works | ☐ | ☐ | ☐ |
| Open a Scheduled event → status = Open | ☐ | ☐ | ☐ |
| Start an Open event → status = Live | ☐ | ☐ | ☐ |

---

### 10. Seasons

| Check | Django | Blazor | Match? |
|-------|--------|--------|--------|
| Season list renders | ☐ | ☐ | ☐ |
| Activate a Scheduled season | ☐ | ☐ | ☐ |
| Leaderboard page loads for Active season | ☐ | ☐ | ☐ |
| Recompute tiers completes without error | ☐ | ☐ | ☐ |

---

### 11. Anti-Cheat Flags

| Check | Django | Blazor | Match? |
|-------|--------|--------|--------|
| Flags list renders | ☐ | ☐ | ☐ |
| Filter unreviewedOnly=true narrows results | ☐ | ☐ | ☐ |
| Review a flag → row shows reviewed state | ☐ | ☐ | ☐ |

---

### 12. Notifications

| Check | Django | Blazor | Match? |
|-------|--------|--------|--------|
| Channels list renders | ☐ | ☐ | ☐ |
| Send notification → job ID returned | ☐ | ☐ | ☐ |
| History shows sent notification | ☐ | ☐ | ☐ |
| Dead-letter queue renders (may be empty) | ☐ | ☐ | ☐ |

---

### 13. Event Queue

| Check | Django | Notes |
|-------|--------|-------|
| Event queue reprocess page renders | ☐ | |
| Reprocess with scope `*` limit 1 | ☐ | Verify job ID returned |

---

### 14. Storage & Media

| Check | Django | Blazor | Match? |
|-------|--------|--------|--------|
| Media intent page renders | ☐ | ☐ | ☐ |
| Create upload intent succeeds | ☐ | ☐ | ☐ |
| MinIO diagnostics renders health status | ☐ | ☐ | ☐ |

---

## Avatar Purchase Path (API-level, no Blazor equivalent)

These are new endpoints with no Blazor surface — test via curl or Postman.

```bash
# 1. Catalog (anonymous — owned: false on all items)
curl https://api-staging.synaptix.internal/store/catalog?category=avatar

# 2. Catalog (authenticated player — verify owned: false initially)
curl -H "Authorization: Bearer <player-jwt>" \
     https://api-staging.synaptix.internal/store/catalog?category=avatar

# 3. Purchase avatar (player with enough coins)
curl -X POST -H "Authorization: Bearer <player-jwt>" \
     -H "Content-Type: application/json" \
     -d '{"playerId": "<player-uuid>"}' \
     https://api-staging.synaptix.internal/store/avatars/hero-v1/purchase

# 4. Catalog again — same item should now have owned: true
curl -H "Authorization: Bearer <player-jwt>" \
     https://api-staging.synaptix.internal/store/catalog?category=avatar

# 5. Re-purchase → expect 409 already_owned
curl -X POST -H "Authorization: Bearer <player-jwt>" \
     -H "Content-Type: application/json" \
     -d '{"playerId": "<player-uuid>"}' \
     https://api-staging.synaptix.internal/store/avatars/hero-v1/purchase

# 6. Asset download URL (owned player)
curl -H "Authorization: Bearer <player-jwt>" \
     https://api-staging.synaptix.internal/v1/assets/avatars/hero-v1

# 7. Asset download URL (non-owner) → expect 403 not_owned
curl -H "Authorization: Bearer <different-player-jwt>" \
     https://api-staging.synaptix.internal/v1/assets/avatars/hero-v1
```

Expected responses documented in `docs/full_api_handoff_2026-04-28.md`.

---

## Pass / Fail Criteria

**PASS — proceed with cutover on May 15:**
- All Django checks ☑ (100 %)
- Django vs Blazor discrepancies: 0 functional differences (cosmetic/layout differences are acceptable)
- Avatar API checks all return expected status codes

**HOLD — delay cutover, keep Blazor primary:**
- Any data-altering action (ban, grant, approve, reject) produces a different result in Django vs Blazor
- Any Django surface returns 500 on the golden path
- Django login fails for either test account

**ROLLBACK TRIGGER (post-cutover):**
- Any production operator reports a functional regression not caught in parallel run
- Blazor remains warm until June 12 — flip nginx upstream back to Blazor port within 5 minutes

---

## Sign-off

| Role | Name | Date | Signature |
|------|------|------|-----------|
| QA Lead | | | |
| Backend Lead | | | |
| On-call Operator | | | |

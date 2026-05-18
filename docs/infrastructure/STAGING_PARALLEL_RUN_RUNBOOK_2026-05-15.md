# Staging Parallel-Run Runbook — May 15 Blazor → Django Cutover

**Target cutover date:** 2026-05-15  
**Parallel-run window:** 2026-05-08 → 2026-05-14 (complete before cutover)  
**Rollback window:** 2026-05-15 → 2026-06-12 (Blazor kept warm)

---

## May 14 Evidence-Capture Preface

Use this runbook with
[`docs/OPERATOR_DASHBOARD_MAY_CUTOVER_COMPLETION_GUIDE.md`](OPERATOR_DASHBOARD_MAY_CUTOVER_COMPLETION_GUIDE.md).
Record live execution evidence in
[`docs/OPERATOR_PARALLEL_RUN_EVIDENCE_2026-04-08.md`](OPERATOR_PARALLEL_RUN_EVIDENCE_2026-04-08.md)
before marking release gates complete in the parity checklist.

Repo preparation completed on 2026-05-14:

- CI/readiness automation is prepared for JSON/Markdown evidence.
- Evidence tables and release artifact slots are prepared.
- Repo verification baseline is recorded in the May completion guide.

These do not replace live staging execution, migration/readiness evidence, or human sign-off.

For each workflow, capture:

- staging environment identifier and deployed image tags;
- operator account role used for the check, without secrets;
- pass/fail result;
- discrepancy notes and defect links;
- screenshot or log reference when the workflow mutates data.

The Django dashboard now includes some Django-only surfaces with no Blazor equivalent. Run those as
supplemental checks after the legacy parity matrix.

---

## Prerequisites

Before starting the parallel run, verify all gates are clear:

| Gate | Owner | Status |
|------|-------|--------|
| CI/readiness automation prepared | Backend / DevOps | Complete — run `.github/workflows/operator-cutover-readiness.yml` for live artifacts |
| Evidence-capture package prepared | Backend / QA | Complete — update `docs/OPERATOR_PARALLEL_RUN_EVIDENCE_2026-04-08.md` during execution |
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
| `/users/{userId}` detail page loads account summary and activity | ☐ | n/a | ☐ |
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
| `/moderation/logs/{logId}` detail page loads reason, notes, and related flag | ☐ | n/a | ☐ |
| `/moderation/players/{playerId}` page loads profile and filtered history | ☐ | n/a | ☐ |
| Set moderation status succeeds | ☐ | ☐ | ☐ |

---

### 5. Security Audit

| Check | Django | Blazor | Match? |
|-------|--------|--------|--------|
| Audit log loads | ☐ | ☐ | ☐ |
| Filter by event type works | ☐ | ☐ | ☐ |
| `/audit/security/{eventId}` detail page loads formatted metadata | ☐ | n/a | ☐ |

---

### 6. Questions Queue

| Check | Django | Blazor | Match? |
|-------|--------|--------|--------|
| Pending questions list renders | ☐ | ☐ | ☐ |
| `/content/questions/{questionId}` detail/edit page loads full question and options | ☐ | n/a | ☐ |
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
| Schedule notification → schedule row appears | ☐ | ☐ | ☐ |
| Cancel scheduled notification | ☐ | ☐ | ☐ |
| Create/update/delete template | ☐ | ☐ | ☐ |
| Upsert notification channel | ☐ | ☐ | ☐ |
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

## Supplemental Django-Only Checks

These do not need Blazor comparison. They must pass before cutover because they are active Django
operator workflows.

| Surface | Check | Result | Evidence |
|---------|-------|--------|----------|
| User investigation | `/users/{userId}/investigation` loads account, activity, moderation, economy, personalization, and store links | ☐ | |
| Personalization overview | `/personalization` renders summary, archetypes, and recommendation performance | ☐ | |
| Personalization player debug | `/personalization/player?playerId=<uuid>` renders profile/debug/audit rows | ☐ | |
| Personalization rules | Rule JSON update rejects invalid JSON and accepts valid JSON | ☐ | |
| Player stock | `/store/player-stock?playerId=<uuid>` renders stock rows | ☐ | |
| Player stock override | Effective max override and clear-override actions complete | ☐ | |
| Stock bulk reset | Bulk reset accepts SKU list and records success message | ☐ | |
| Notification advanced admin | schedule/cancel, template CRUD, and channel upsert complete | ☐ | |

---

## Pass / Fail Criteria

**PASS — proceed with cutover on May 15:**
- All Django checks ☑ (100 %)
- Django vs Blazor discrepancies: 0 functional differences (cosmetic/layout differences are acceptable)
- Avatar API checks all return expected status codes
- Staging and production migration/readiness evidence has been attached.
- QA Lead, Backend Lead, and On-call Operator sign-off rows are complete.

**HOLD — delay cutover, keep Blazor primary:**
- Any data-altering action (ban, grant, approve, reject) produces a different result in Django vs Blazor
- Any Django surface returns 500 on the golden path
- Django login fails for either test account
- Any live migration/readiness artifact is missing or failed.
- Any required signer withholds approval.

**ROLLBACK TRIGGER (post-cutover):**
- Any production operator reports a functional regression not caught in parallel run
- Blazor remains warm until June 12 — flip nginx upstream back to Blazor port within 5 minutes

---

## Sign-off

The sign-off table closes only the staging parallel-run and cutover approval gate.
The overall May cutover remains open until the production route cutover is recorded,
post-cutover smoke passes, and Blazor stays available through 2026-06-12.

| Role | Name | Date | Signature |
|------|------|------|-----------|
| QA Lead | | | |
| Backend Lead | | | |
| On-call Operator | | | |

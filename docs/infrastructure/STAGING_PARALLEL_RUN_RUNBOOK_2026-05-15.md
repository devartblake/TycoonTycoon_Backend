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

## How To Execute User-Interactive Checks

Use this loop for every browser-driven checklist row:

1. Start with Django. Load the target page, confirm the expected controls and data are visible, and note the exact test object ID.
2. If the row has a Blazor equivalent, perform the same lookup or action in Blazor using the same operator account and same test object.
3. Compare functional results, not pixel-perfect layout. Cosmetic spacing, colors, and table density may differ. Data values, permissions, status transitions, error states, and history/audit visibility must match.
4. For read-only checks, capture a screenshot or evidence link showing the loaded page and key ID/timestamp.
5. For mutating checks, capture evidence before the action, immediately after the action, and after refreshing or revisiting the related history/status page.
6. If staging lacks the required test data but the UI behaves correctly, mark the row as `Needs investigation` and record the missing object or seed data needed. Do not mark it failed unless the product behavior is broken.

### Interactive Pass / Fail Rubric

| Result | Use When |
|--------|----------|
| Pass | Page loads without 500/403, required controls are visible, expected data appears, and any mutation is reflected in status/history/audit output. |
| Fail | Page errors, required controls are missing, data is stale or wrong, permission behavior is incorrect, or a mutating action cannot be verified. |
| Needs investigation | Staging data is missing or ambiguous, but the UI/API path appears healthy and no functional defect is proven yet. |

### Evidence Notes Template

Use this format in the evidence artifact for each meaningful workflow. Do not include secrets, passwords, bearer tokens, or raw PII beyond the minimum IDs already needed for operator validation.

```text
Surface:
Checklist row:
Operator account / role:
Test user/player/object ID:
Django result:
Blazor result:
Evidence link or screenshot reference:
Mutation evidence before / after:
Discrepancy ticket:
Rollback relevance:
Notes:
```

---

## Test Surfaces

### 1. Auth & Permissions

| Check | Django | Blazor | Match? |
|-------|--------|--------|--------|
| Login succeeds | x | x | x |
| Profile email shown in sidebar footer | x | ☐ | ☐ |
| All nav links visible (no 403 on any surface) | x | n/a | ☐ |
| Logout clears session | x | x | x |

**Permission verification:** After login, inspect the session profile in Django dev tools (`/api/operator/health` → verify `permissions` array contains all expected scopes: `users:read`, `store:read`, `economy:read`, `questions:read`, `events:read`, `anticheat:read`, `seasons:read`, `notifications:read`).

**What to look for:**

- Sidebar footer shows the logged-in operator email and `Session active`; it must not collapse, truncate to blank, or show another operator.
- Full-permission Operator A should see every navigation group. Lower-permission accounts should hide or block only the surfaces they do not own.
- A `403` is acceptable only when testing a deliberately missing permission. It is a failure for the full-permission account.
- Logout must return to the login page and prevent access to a previously open protected URL after refresh.

---

### 2. Command Center (Health)

| Check | Django | Blazor | Match? |
|-------|--------|--------|--------|
| All service tiles render | x | x | x |
| Health statuses match | x | ☐ | ☐ |

**What to look for:**

- Each tile should show a service name, status, and enough detail to diagnose degraded/offline services.
- Django and Blazor do not need identical styling, but status values and failing service names must match.
- Record any degraded service with timestamp and whether it blocks operator workflows.

---

### 3. Users

| Check | Django | Blazor | Match? |
|-------|--------|--------|--------|
| User list loads | x | x | x |
| Filter by email partial works | x | ☐ | ☐ |
| Filter by `banned=true` returns same set | x | x | x |
| `/users/{userId}` detail page loads account summary and activity | x | n/a | ☐ |
| Ban a test user | x | x | x |
| Unban the same user | x | x | x |
| User detail panel shows activity | x | x | x |

**What to look for:**

- List page: filters should preserve entered values after submit, pagination should remain usable, and rows should show stable IDs/emails/usernames.
- Detail page: account summary, activity, editable username field, ban controls, and raw payload should render without overlapping long IDs or email addresses.
- Investigation link from detail should open `/users/{userId}/investigation`; Cancel should return to the prior user detail page.
- Ban/unban: capture the user status before, submit with a test reason, refresh the list/detail page, and confirm status/history reflects the change in both dashboards.
- Do not use production-like real players for destructive tests. Use a named staging test account and record the user ID.

---

### 4. Moderation

| Check | Django | Blazor | Match? |
|-------|--------|--------|--------|
| Moderation log renders | x | x | x |
| Filter by date range narrows results | x | ☐ | ☐ |
| Player moderation profile loads | x | ☐ | ☐ |
| `/moderation/logs/{logId}` detail page loads reason, notes, and related flag | x | n/a | ☐ |
| `/moderation/players/{playerId}` page loads profile and filtered history | x | n/a | ☐ |
| Set moderation status succeeds | x | ☐ | ☐ |

**What to look for:**

- Log list filters should narrow results without losing the selected filter values.
- Log detail must show log ID, player ID, status, reason, notes, related flag, expiry, and raw payload where available.
- Player moderation page must show current profile plus filtered history for the same player ID.
- Status changes require before/after screenshots and a refreshed profile/log row proving the new status, reason, operator, and timestamp.

---

### 5. Security Audit

| Check | Django | Blazor | Match? |
|-------|--------|--------|--------|
| Audit log loads | x | ☐ | ☐ |
| Filter by event type works | x | ☐ | ☐ |
| `/audit/security/{eventId}` detail page loads formatted metadata | x | n/a | ☐ |

**What to look for:**

- Audit rows should include event/title, status, actor/IP where available, and timestamp.
- Detail page must display the same event ID from the list and readable formatted metadata JSON.
- Filtering must not silently drop recent known events. If no matching data exists, mark `Needs investigation` and record the filter used.

---

### 6. Questions Queue

| Check | Django | Blazor | Match? |
|-------|--------|--------|--------|
| Pending questions list renders | x | x | x |
| `/content/questions/{questionId}` detail/edit page loads full question and options | x | n/a | ☐ |
| Approve a question → status changes to Approved | ☐ | ☐ | ☐ |
| Reject a question → status changes to Rejected | ☐ | ☐ | ☐ |
| Filter by status=Approved shows only approved | ☐ | ☐ | ☐ |

**What to look for:**

- Queue rows should show enough text/category/status context to identify the question.
- Detail/edit page must show full question text, all options, correct option, tags/media fields when present, and permission-appropriate approve/reject/delete/edit controls.
- Approve/reject actions require before/after status evidence and a filtered list refresh proving the row moved to the expected status.
- If editing is tested, use a staging-only question and capture the original text/options plus the final saved payload.

---

### 7. Economy — Player

| Check | Django | Blazor | Match? |
|-------|--------|--------|--------|
| Player lookup by UUID loads history | x | ☐ | ☐ |
| Transaction history shows correct amounts | ☐ | ☐ | ☐ |
| Grant 10 coins to test player | ☐ | ☐ | ☐ |
| New transaction appears in history | ☐ | ☐ | ☐ |
| Deduction of 5 coins succeeds | ☐ | ☐ | ☐ |

**What to look for:**

- Lookup must use the same player UUID in both dashboards.
- History should show transaction type, amount, reason, operator/source, and timestamp consistently.
- For grants/deductions, capture balance/history before action, submitted amount/reason, success state, and the new history row after refresh.
- Any mismatch in amount sign, currency, reason, or player ID is a functional failure.

---

### 8. Store

| Check | Django | Blazor | Match? |
|-------|--------|--------|--------|
| Flash sales list renders | ☐ | ☐ | ☐ |
| Cancel a flash sale (use test promo) | ☐ | ☐ | ☐ |
| Stock policies list renders | ☐ | ☐ | ☐ |
| Filter stock policies by SKU | ☐ | ☐ | ☐ |
| Purchase analytics loads with date range | ☐ | ☐ | ☐ |

**What to look for:**

- Flash sales and stock policy rows should show IDs/SKUs, active status, limits, and effective dates clearly enough to identify the test object.
- Cancel flash sale only against a staging test promo; verify the row status changes after refresh and is consistent in Blazor.
- Stock policy filtering should return only matching SKUs or a clear empty state.
- Analytics date-range changes should preserve filter values and update counts/charts/tables without errors.

---

### 9. Game Events

| Check | Django | Blazor | Match? |
|-------|--------|--------|--------|
| Event list renders | ☐ | ☐ | ☐ |
| Filter by status=Scheduled works | ☐ | ☐ | ☐ |
| Open a Scheduled event → status = Open | ☐ | ☐ | ☐ |
| Start an Open event → status = Live | ☐ | ☐ | ☐ |

**What to look for:**

- Event rows should show event ID/name, status, schedule window, and action controls appropriate for the current status.
- Use a staging-only scheduled/open event. Capture the status before action, after action, and after a page refresh.
- The next valid action should appear only after the prior status transition succeeds.

---

### 10. Seasons

| Check | Django | Blazor | Match? |
|-------|--------|--------|--------|
| Season list renders | ☐ | ☐ | ☐ |
| Activate a Scheduled season | ☐ | ☐ | ☐ |
| Leaderboard page loads for Active season | ☐ | ☐ | ☐ |
| Recompute tiers completes without error | ☐ | ☐ | ☐ |

**What to look for:**

- Season rows should show status, date window, and action eligibility.
- Activation must change the selected season to Active and must not accidentally activate the wrong season.
- Leaderboard should load for the same season ID and show either player rows or a clear empty state.
- Recompute tiers should produce a visible success result or job confirmation and no 500/timeout.

---

### 11. Anti-Cheat Flags

| Check | Django | Blazor | Match? |
|-------|--------|--------|--------|
| Flags list renders | ☐ | ☐ | ☐ |
| Filter unreviewedOnly=true narrows results | ☐ | ☐ | ☐ |
| Review a flag → row shows reviewed state | ☐ | ☐ | ☐ |

**What to look for:**

- Flag rows should show flag ID, player ID, rule/type, severity, reviewed state, and timestamp.
- Filtering `unreviewedOnly=true` should exclude reviewed rows after refresh.
- Review action must persist reviewed state, operator/reviewer where available, and notes/annotation if entered.

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

**What to look for:**

- Channel/template lists should show IDs/keys, status, and last-updated context.
- Send/schedule actions require a staging-safe audience and evidence of returned job or schedule ID.
- Cancel scheduled notification should remove or mark the schedule as cancelled after refresh.
- Template/channel CRUD should preserve edited values and not expose malformed JSON or validation errors.
- History/dead-letter pages should show matching IDs/statuses or a clear empty state.

---

### 13. Event Queue

| Check | Django | Notes |
|-------|--------|-------|
| Event queue reprocess page renders | ☐ | |
| Reprocess with scope `*` limit 1 | ☐ | Verify job ID returned |

**What to look for:**

- The page should explain scope/limit inputs and show validation errors for invalid values.
- Reprocess should return a job ID or equivalent confirmation. Capture the scope, limit, job ID, and timestamp.
- Do not use a large limit during staging validation.

---

### 14. Storage & Media

| Check | Django | Blazor | Match? |
|-------|--------|--------|--------|
| Media intent page renders | ☐ | ☐ | ☐ |
| Create upload intent succeeds | ☐ | ☐ | ☐ |
| MinIO diagnostics renders health status | ☐ | ☐ | ☐ |

**What to look for:**

- Media intent should return a usable upload intent envelope without exposing secrets in screenshots.
- MinIO diagnostics should show status, latency/probe details, and clear degraded/offline messaging if storage is unhealthy.
- If Blazor lacks an equivalent control for a storage check, record Django result and mark Blazor as `n/a`.

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

| Surface | Check | What To Inspect | Result | Evidence |
|---------|-------|-----------------|--------|----------|
| User detail | `/users/{userId}` loads summary, activity, editable fields, ban controls, and raw payload | Long user ID/email wrap without overlap; activity rows match backend; edit/ban controls appear only with `users:write` | ☐ | |
| User investigation | `/users/{userId}/investigation` loads account, activity, moderation, economy, personalization, and store links | Workbench is read-only; Cancel returns to user detail; cross-surface player ID lookup preserves query values; editable actions link to authoritative pages | ☐ | |
| Question detail | `/content/questions/{questionId}` loads full question and options | Text, options, correct option, tags/media, approve/reject/delete/edit controls render as permissions allow | ☐ | |
| Moderation log detail | `/moderation/logs/{logId}` loads one log | Reason, notes, related flag, expiry, player link, and raw payload are visible | ☐ | |
| Moderation player | `/moderation/players/{playerId}` loads profile and filtered history | Current status, reason, expiry, set-by operator, filtered logs, and status update form behave correctly | ☐ | |
| Security audit detail | `/audit/security/{eventId}` loads one event | Event ID matches list row; status/title/channel/timestamp render; metadata JSON is formatted and readable | ☐ | |
| Personalization overview | `/personalization` renders summary, archetypes, and recommendation performance | KPI values, charts/tables, and empty/degraded states are legible | ☐ | |
| Personalization player debug | `/personalization/player?playerId=<uuid>` renders profile/debug/audit rows | Profile risk values, behavior events, recommendation audit rows, and reset/recalculate controls are permission-gated | ☐ | |
| Personalization rules | Rule JSON update rejects invalid JSON and accepts valid JSON | Invalid JSON shows validation error; valid JSON persists after refresh | ☐ | |
| Player stock | `/store/player-stock?playerId=<uuid>` renders stock rows | SKU, used quantity, remaining quantity, effective max, and override state are clear | ☐ | |
| Player stock override | Effective max override and clear-override actions complete | Before/after stock row proves override changed and clear restored policy behavior | ☐ | |
| Stock bulk reset | Bulk reset accepts SKU list and records success message | Submitted SKU list, success message, and refreshed stock state are captured | ☐ | |
| Notification advanced admin | schedule/cancel, template CRUD, and channel upsert complete | Job/schedule/template/channel IDs are visible and history reflects the action | ☐ | |

**Django-only pass rule:** These rows are not Blazor parity blockers, but any 500, missing golden-path control, broken return navigation, or unverified mutation is a cutover blocker until fixed or explicitly accepted by QA Lead and Backend Lead.

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

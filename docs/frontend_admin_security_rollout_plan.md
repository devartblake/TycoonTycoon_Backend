# Frontend Rollout Plan for Admin Security Hardening

## Purpose
This document translates the completed backend hardening work into a concrete frontend implementation path.
It focuses only on **non-test backend changes** that alter frontend behavior, contracts, and operational handling.

---

## 1) Backend changes completed (frontend-relevant)

## 1.1 Standardized error envelope for secured/admin-sensitive flows

See also: [`docs/security_error_envelope_contract.md`](./security_error_envelope_contract.md) for a code-by-code handling matrix.

Backend now returns a structured envelope for hardened failures:

```json
{
  "error": {
    "code": "FORBIDDEN | UNAUTHORIZED | RATE_LIMITED | NOT_FOUND | CONFLICT | VALIDATION_ERROR",
    "message": "...",
    "details": {}
  }
}
```

### Confirmed high-impact paths
- Rate limiter rejections are now structured with `RATE_LIMITED`. 
- Match-start denial paths return `FORBIDDEN` envelopes (regular + mobile). 
- Matchmaking and party enqueue forbidden branches return `FORBIDDEN` envelopes.

---

## 1.2 Admin security layers enforced
For `/admin/*` routes, backend expects:
1. **Ops key header** (`X-Admin-Ops-Key`) 
2. Valid bearer token for policy-protected routes
3. Correct role/audience/scope according to admin policy

If any layer fails, frontend now receives consistent `401/403` envelopes suitable for deterministic UX branching.

---

## 1.3 Admin audit + observability surface added
- Security audit events are persisted and queryable through:
  - `GET /admin/audit/security`
- Metrics and runbook/dashboard artifacts exist for admin auth/notification/rate-limit signals.

Frontend can now support:
- admin security timeline views,
- filters (time/status/page),
- incident triage UX linked to envelope codes.

---

## 1.4 Notification operations hardened
Admin notification flows include:
- policy/rate-limit hardening,
- dead-letter list endpoint,
- replay endpoint,
- retry/backoff + dead-letter state behavior in backend runtime.

Frontend should treat replay and send operations as potentially throttled and conflict-prone (`RATE_LIMITED`, `CONFLICT`).

---

## 2) Frontend implementation plan (recommended order)

## Phase A — Cross-app API client contract alignment (highest priority)
1. Add a global API error parser for `error.code`.
2. Normalize handling table:
   - `UNAUTHORIZED` -> auth/session remediation
   - `FORBIDDEN` -> permission/role message
   - `RATE_LIMITED` -> retry-after UX + throttling banners
   - `VALIDATION_ERROR` -> field-level or form-level validation display
   - `NOT_FOUND` / `CONFLICT` -> contextual stale-state refresh + action guidance
3. Ensure every admin API call path uses this parser (no raw status-only branching).

## Phase B — Auth + ops-key aware admin transport
1. Ensure admin HTTP client always sends:
   - bearer token,
   - ops-key header (where required by deployment model).
2. Add interceptor behavior:
   - on `401 UNAUTHORIZED`: route to re-auth/session refresh UX.
   - on `403 FORBIDDEN`: show role/scope denial UI state (not generic failure).
3. Add telemetry tags on frontend errors keyed by `error.code` and endpoint path.

## Phase C — Notification admin console parity
1. Add dead-letter list UI with filters + paging.
2. Add replay action with optimistic/pessimistic handling:
   - success -> refresh list/history,
   - `CONFLICT` -> show "already replayed/non-failed" state,
   - `RATE_LIMITED` -> disable action briefly + hint.
3. Add send/schedule UX resilience:
   - debounce and retry hints on `RATE_LIMITED`.

## Phase D — Admin security audit UI integration
1. Build `admin/audit/security` page:
   - filters: from, to, status,
   - paging controls aligned with backend clamp/fallback behavior,
   - event detail drawer for metadata.
2. Add prebuilt filters for incident triage:
   - unauthorized spikes,
   - forbidden spikes,
   - rate-limit bursts.

## Phase E — Runtime observability hooks in frontend
1. Emit frontend analytics for admin failures grouped by `error.code`.
2. Add dashboards correlating frontend-visible errors with backend metrics/runbook steps.
3. Add SLO widgets (optional): admin action success %, throttled action %, median response latency.

---

## 3) Endpoint-by-endpoint frontend action map

| Endpoint/Surface | New backend expectation | Frontend action |
|---|---|---|
| `/admin/auth/*` | strict policy + ops-key + rate-limit envelopes | robust auth fallback + forbidden UX + throttling UX |
| `/admin/notifications/*` | scoped policy + send rate-limit + dead-letter/replay contracts | add replay/dead-letter UX and envelope-specific handling |
| `/admin/audit/security` | queryable audit stream with filters/paging | implement security timeline page |
| `/matches/start`, `/mobile/matches/start` | structured `FORBIDDEN` when blocked | show player-restriction state using parsed envelope |
| `/matchmaking/enqueue`, `/party/{id}/enqueue` | structured `FORBIDDEN` on blocked enqueue | prevent retry loops; show restriction reason UX |

---

## 4) Suggested frontend PR slicing

1. **PR-FE-1: Error envelope adapter + interceptor updates**
   - Add shared parser and error-code mapping.
2. **PR-FE-2: Admin auth/ops transport hardening**
   - Ensure correct headers/tokens and role-denial UX.
3. **PR-FE-3: Notifications dead-letter + replay UI**
   - Add list/replay + conflict/throttle handling.
4. **PR-FE-4: Admin security audit page**
   - Add filtered timeline and paging.
5. **PR-FE-5: Frontend observability linkage**
   - Add telemetry dimensions and dashboard correlations.

---

## 5) Rollout and validation checklist

- [ ] All admin API calls consume `error.code` centrally.
- [ ] No admin page relies on status-only error handling.
- [ ] `RATE_LIMITED` UX implemented for auth + notifications + enqueue flows.
- [ ] Dead-letter replay UI handles `CONFLICT` and refreshes list state.
- [ ] Admin audit page supports status/time filters and paging.
- [ ] Frontend analytics include endpoint + `error.code` dimensions.
- [ ] End-to-end QA run against staging with hardened backend enabled.

---

## 6) Known environment constraint during backend iteration
Backend local runtime in this container lacks `dotnet`, so full local e2e execution was unavailable here.
CI/runtime environments with .NET SDK should be used for final end-to-end verification.

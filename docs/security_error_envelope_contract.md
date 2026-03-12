# Security Error Envelope Contract (Backend -> Frontend)

## Purpose
This document defines the normalized error-envelope contract that frontend clients should rely on
for hardened auth/admin and protected gameplay flows.

## Envelope shape

```json
{
  "error": {
    "code": "UNAUTHORIZED | FORBIDDEN | RATE_LIMITED | VALIDATION_ERROR | NOT_FOUND | CONFLICT",
    "message": "Human-readable summary",
    "details": {}
  }
}
```

## Codes and expected frontend behavior

- `UNAUTHORIZED`
  - Meaning: missing/invalid auth context (missing token, invalid token, missing ops-key where required).
  - Frontend action: session recovery flow (reauth), key/header remediation for admin transport.

- `FORBIDDEN`
  - Meaning: authenticated but not allowed by role/scope/policy/moderation state.
  - Frontend action: show capability/permission denied state; avoid auto-retry loops.

- `RATE_LIMITED`
  - Meaning: request rejected by rate-limit policy.
  - Frontend action: cool-down UX, retry-after behavior, disable repeated actions temporarily.

- `VALIDATION_ERROR`
  - Meaning: request body/query contract invalid.
  - Frontend action: form-level/field-level validation handling.

- `NOT_FOUND`
  - Meaning: target resource no longer exists or wrong identifier.
  - Frontend action: refresh list/state and show stale-resource guidance.

- `CONFLICT`
  - Meaning: state transition invalid for current resource state.
  - Frontend action: refresh state and present conflict-specific resolution.

## Endpoint families where this contract is now critical

- Admin surface (`/admin/*`), especially:
  - auth/login/refresh/me
  - notifications send/schedule/dead-letter/replay
  - audit query endpoints
- Protected gameplay and queue paths:
  - `/matches/start`
  - `/mobile/matches/start`
  - `/matchmaking/enqueue`
  - `/party/{partyId}/enqueue`
- Rate-limited flows:
  - admin auth/notifications policies
  - `/matches/submit`

## Notes for rollout

- Frontend should parse `error.code` centrally (interceptor/middleware layer), then map to UX behavior.
- Avoid status-only branching where envelope is available.
- Include endpoint + `error.code` in frontend telemetry for correlation with backend dashboards/runbooks.

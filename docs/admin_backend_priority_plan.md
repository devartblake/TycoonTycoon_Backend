# Admin Backend Priorities (Contract Alignment)

This priority split is intended to unblock frontend integration first and keep optional platform work separate.

## P0 — Must Have (for core admin app functionality)

1. **Admin authentication and claims**
   - `POST /admin/auth/login`
   - `POST /admin/auth/refresh`
   - `GET /admin/auth/me`
   - Enforce admin role/permissions server-side.

2. **User management API surface**
   - `GET /admin/users` + pagination/filter/sort
   - `GET /admin/users/{userId}`
   - `POST /admin/users`
   - `PATCH /admin/users/{userId}`
   - `POST /admin/users/{userId}/ban`
   - `POST /admin/users/{userId}/unban`
   - `DELETE /admin/users/{userId}`
   - `GET /admin/users/{userId}/activity`

3. **Question bank API surface**
   - `GET /admin/questions`
   - `POST /admin/questions`
   - `PATCH /admin/questions/{questionId}`
   - `DELETE /admin/questions/{questionId}`
   - `POST /admin/questions/bulk`
   - `GET /admin/questions/export`

4. **Shared API behavior consistency**
   - Pagination envelope
   - Error envelope
   - ISO-8601 UTC timestamps
   - Auth header + role enforcement on `/admin/**`

## P1 — Should Have (high value, can follow P0)

1. **Event queue admin workflows**
   - `POST /admin/event-queue/upload`
   - `POST /admin/event-queue/reprocess` (optional in spec but high operational value)

2. **Audit logging for mutating admin actions**
   - User mutation events
   - Question mutation events
   - Queue reprocess/upload actions

## P2 — Nice to Have (optional platform expansion)

1. **Server-managed notifications**
   - `/admin/notifications/channels`
   - `/admin/notifications/send`
   - `/admin/notifications/schedule`
   - `/admin/notifications/templates`
   - `/admin/notifications/history`

2. **Server-managed admin config**
   - `GET /admin/config`
   - `PATCH /admin/config`


## Current implementation status

- **P0 status:** Completed.
  - Implemented: `/admin/auth/*`, `/admin/questions*`, `/admin/users*`, standardized admin error envelope usage, and centralized admin route protection (`RequireAdminOpsKey` + authenticated admin-claim gate).
- **P1 status:** Completed.
  - Implemented: `/admin/event-queue/upload`, `/admin/event-queue/reprocess` with dedupe + per-event statuses and audit logging for event/user/question mutating actions.
- **P2 status:** Completed.
  - Implemented: `/admin/config` (`GET`, `PATCH`) and `/admin/notifications/*` endpoints for channels/send/schedule/scheduled/templates/history, including history filters (`from`, `to`, `channelKey`, `status`).

## Open decisions to finalize before full rollout

- MFA requirement for admin login
- Token lifetime and refresh rotation
- Canonical enums (`UserStatus`, `UserRole`, `AgeGroup`)
- Default bulk question mode (`upsert` vs `replace`)
- Event dedupe key strategy (`eventId` vs hash)
- Whether notifications/config stay local-only or server-managed


## Next priorities (post P2)

1. Production hardening
   - Persist notifications/config state in durable storage (replace in-memory endpoint state).
   - Add background execution for scheduled notifications and delivery retry handling.
2. Security/compliance hardening
   - Replace allow-list-only admin claims with explicit role/scope claims in JWT and policy-based authorization.
   - Add rate limits and abuse controls for admin auth and notification send endpoints.
3. Observability and operations
   - Add audit tables/streams for admin actions (beyond logs), with query endpoints for governance.
   - Add metrics and dashboards for notification send/schedule success/failure.
4. Contract completeness and QA
   - Run full .NET test suite in CI with SDK installed and add contract tests for error envelopes on all admin endpoints.

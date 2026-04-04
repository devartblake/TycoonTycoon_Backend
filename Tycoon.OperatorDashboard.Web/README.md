# Tycoon.OperatorDashboard.Web

Web/BFF target for Operator Dashboard migration.

## Phase-1 bootstrap (in progress)
- [x] Health/readiness endpoints
- [x] Admin API proxy baseline (`/api/admin/{**path}`)
- [x] Backend base URL configuration (`Backend:BaseUrl`)
- [x] Initial domain-specific proxy groups (`/api/dashboard`, `/api/audit-log`, `/api/users`)
- [x] Session bootstrap endpoint (`/api/me`)
- [x] Initial typed Wave A endpoints (`/api/dashboard/overview`, `/api/audit-log`, `/api/users`)
- [x] Header-based auth/session bootstrap middleware (`X-Operator-User`, `X-Operator-Permissions`)
- [ ] Real auth/session integration layer (cookie/JWT-backed)
- [x] Structured error envelope passthrough refinements (non-JSON upstream errors + timeout/unreachable handling)

## Expected next implementation step
Replace header-based auth/session bootstrap with real cookie/JWT auth and move permissions to server-issued claims.

## Current Status (April 4, 2026)
- Generic + domain proxy endpoints are active for Wave A migration paths.
- Typed Wave A handlers exist for dashboard overview, audit log, and users list.
- Session bootstrap is currently header-assisted (`X-Operator-User`, `X-Operator-Permissions`) and intended only as migration bootstrap.
- Next: switch to real auth middleware and tighten standardized error-envelope passthrough.

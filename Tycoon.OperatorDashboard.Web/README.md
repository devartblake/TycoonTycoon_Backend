# Tycoon.OperatorDashboard.Web

Web/BFF target for Operator Dashboard migration.

## Phase-1 bootstrap (in progress)
- [x] Health/readiness endpoints
- [x] Admin API proxy baseline (`/api/admin/{**path}`)
- [x] Backend base URL configuration (`Backend:BaseUrl`)
- [x] Initial domain-specific proxy groups (`/api/dashboard`, `/api/audit-log`, `/api/users`)
- [x] Session bootstrap endpoint (`/api/me`)
- [ ] Auth/session integration layer
- [ ] Structured error envelope passthrough refinements

## Expected next implementation step
Replace placeholder session model with real auth integration and introduce typed endpoint handlers for Wave A pages.

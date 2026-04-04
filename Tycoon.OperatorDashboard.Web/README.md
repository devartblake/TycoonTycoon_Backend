# Tycoon.OperatorDashboard.Web

Web/BFF target for Operator Dashboard migration.

## Phase-1 bootstrap (in progress)
- [x] Health/readiness endpoints
- [x] Admin API proxy baseline (`/api/admin/{**path}`)
- [x] Backend base URL configuration (`Backend:BaseUrl`)
- [ ] Auth/session integration layer
- [ ] Domain-specific endpoint groups
- [ ] Structured error envelope passthrough refinements

## Expected next implementation step
Tighten auth/session forwarding and split proxy into domain-specific endpoint groups for Dashboard/AuditLog/Users.

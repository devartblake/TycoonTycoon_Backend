# Django Operator Dashboard — Status Update & Remaining Work (April 5, 2026)

## Executive Status

The new `Tycoon.OperatorDashboard.Django` service is now containerized and wired as the default `operator-dashboard` in Docker Compose. It currently provides:

- a web UI status page (`/`)
- service aggregation endpoint (`/api/operator/health`)
- liveness endpoint (`/healthz`)
- upstream checks for `.NET API`, `FastAPI sidecar`, and `MinIO`

## Completed So Far

- ✅ Django dashboard scaffolded and committed (`manage.py`, settings, urls, views, template, service layer)
- ✅ Gunicorn-based Docker image added (`docker/Dockerfile.dashboard-django`)
- ✅ Docker Compose switched to Django as primary dashboard service (`operator-dashboard`)
- ✅ Production compose override updated for Django runtime env
- ✅ MinIO health integrated into dashboard status aggregation
- ✅ Initial centralized upstream error mapping helper added for BFF endpoints
- ✅ Initial tests added and passing for service aggregation + API endpoint responses

## What Is Next (Priority Order)

### 1) Authentication & Operator Session Hardening (P0)

- Integrate dashboard with admin auth flow (`/admin/auth/login`, `/admin/auth/me`)
- Add protected dashboard pages (require authenticated operator session)
- Persist and refresh operator session tokens securely
- Enforce permission-aware rendering (e.g., read-only vs action roles)

### 2) BFF Endpoints Beyond Health (P0)

- Add Django BFF routes for core operator workflows:
  - users
  - audit log
  - moderation
  - media operations
- Implement typed request/response DTO boundaries and centralized error mapping
- Add request timeout/retry policy and correlation IDs for upstream calls

### 3) Media/MinIO Operator Workflows (P1)

- Add operator page for bucket/object diagnostics:
  - MinIO reachability
  - bucket existence
  - object upload verification path
- Surface presigned upload diagnostics from backend API
- Add alert states for MinIO degraded/offline conditions

### 4) UI/UX & Layout Hardening (P1)

- Move inline styles to static assets
- Introduce reusable layout/components (nav, cards, tables, status chips)
- Add actionable error states and operator guidance for incident response

### 5) Operational Readiness & CI (P1)

- Add CI job for Django tests and lint checks
- Add compose smoke test target for dashboard + upstream dependencies
- Add runtime logging conventions and dashboard request tracing

### 6) Cutover/Decommission Plan (P2)

- Validate parity for critical operator flows
- Confirm rollback strategy to legacy dashboard path if needed
- Decommission legacy dashboard service after sign-off window

## Remaining Work Checklist

- [ ] Admin authentication integrated and enforced in Django dashboard
- [ ] Operator role/permission model connected to UI rendering and action guards
- [~] BFF proxy modules in progress: users + security-audit implemented; moderation/media pending
- [ ] MinIO diagnostics page implemented (not only health ping)
- [ ] CI pipeline includes Django tests/lint
- [ ] Runbook updated for dashboard incident triage
- [ ] Legacy dashboard deprecation date agreed and documented

## Known Gaps / Risks

- Current dashboard is still mostly a status surface; it is not yet full feature parity with legacy operator workflows.
- Auth/session integration is the largest functional blocker before production-grade cutover.
- MinIO is monitored for health, but advanced object lifecycle operations are not yet surfaced in Django UI.

## Recommended Immediate Sprint Scope (Next 3–5 Days)

1. Implement login + `/admin/auth/me` integration and protect routes.
2. Deliver first authenticated operator module (`users`) through Django BFF.
3. Add CI checks to prevent regressions.
4. Add a basic MinIO diagnostics view with actionable messages.

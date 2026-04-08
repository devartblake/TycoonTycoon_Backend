# Django Operator Dashboard — Status Update & Remaining Work (April 8, 2026)

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
- ✅ Moderation BFF read/action endpoints added (logs, profile, set-status)
- ✅ Media intent + MinIO diagnostics endpoints added
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

- [x] Admin authentication integrated and enforced in Django dashboard (validated by login/logout flow plus redirect-to-login tests on protected routes)
- [x] Operator role/permission model connected to UI rendering and action guards (validated by permission-gated API + UI route tests)
- [x] BFF proxy modules implemented: users + security-audit + moderation + media diagnostics, each with authenticated UI entry points (users now includes sort/filter presets + bulk-action affordance + DB-backed/team-shared saved views)
- [x] MinIO diagnostics endpoint + dedicated diagnostics UI page implemented
- [x] CI pipeline includes Django lint (`ruff check`), Django system checks, and dashboard test execution (`dotnet-ci` workflow, `django-dashboard-tests` job)
- [x] Runbook updated for dashboard incident triage (`docs/OPERATOR_DASHBOARD_INCIDENT_RUNBOOK.md`)
- [x] Legacy dashboard deprecation date agreed and documented (target date: May 15, 2026; rollback window retained)

## Known Gaps / Risks

- Current dashboard is still mostly a status surface; it is not yet full feature parity with legacy operator workflows.
- Auth/session integration is the largest functional blocker before production-grade cutover.
- MinIO is monitored for health, but advanced object lifecycle operations are not yet surfaced in Django UI.

## Recommended Immediate Sprint Scope (Next 3–5 Days)

1. Execute parity checklist in staging and collect operator sign-off notes.
2. Run first monthly drill using `docs/OPERATOR_DASHBOARD_DRILL_CHECKLIST.md`.
3. Add operator UX polish pass (table density and inline field validation).
4. Add fine-grained audit views for saved-view governance events.

## Execution Update — April 6, 2026

- ✅ Added moderation/audit quick filter presets in UI pages.
- ✅ Added bulk-action guardrails for users workflows (`dry-run` and explicit `YES` confirmation for live execution).
- ✅ Added CSV export capabilities for moderation/audit workflows.
- ✅ Added DB-backed + team-shared saved views for users triage.

## Execution Update — April 7, 2026

- ✅ Fixed Sidecar Docker build pathing so `generate_grpc.sh` is executed from a stable workspace location during image build.
- ✅ Verified local gRPC stub generation script execution (`bash Tycoon.Sidecar/generate_grpc.sh`).
- ✅ Added governance controls for team-shared saved views (archive + ownership transfer + audit events).
- ✅ Added parity checklist doc (`docs/OPERATOR_DASHBOARD_PARITY_CHECKLIST.md`).
- ✅ Added drill checklist doc (`docs/OPERATOR_DASHBOARD_DRILL_CHECKLIST.md`).

## Execution Update — April 8–11, 2026

- ✅ Added Django admin auth header/key configuration parity with the legacy Blazor operator client:
  - configurable auth header name via `ADMIN_OPS_HEADER`
  - key fallback support for `AdminOps__Key` and `ADMIN_OPS_KEY`
- ✅ Added targeted test coverage for custom admin ops header behavior.
- ✅ Updated Django env docs to include `ADMIN_OPS_HEADER`.
- ✅ Executed full staging parallel-run with real operator accounts (April 9–11, 2026); all six workflows passed (`docs/OPERATOR_PARALLEL_RUN_STAGING_2026-04-08.md`).
- ✅ Collected two operator sign-offs (ops-lead-1 on 2026-04-10, ops-lead-2 on 2026-04-11); no P0 parity gaps.
- 🚧 Quarterly rollback drill plan + artifact log template in place (`docs/OPERATOR_ROLLBACK_DRILL_STAGING_2026-Q2.md`); drill execution targeted April 15, 2026.

## Remaining Work (Actionable)

1. ~~Execute full staging parallel-run with real operator accounts and record sign-off evidence.~~ ✅ Complete (April 11, 2026)
2. Complete first quarterly rollback drill in staging and attach artifacts to release notes (target: April 15, 2026).
3. Add compose smoke-test target that validates dashboard login + core BFF endpoints end-to-end.
4. Add saved-view governance audit explorer UI (timeline/filtering/export) for operator review workflows.
5. Finish UX hardening pass (shared layout components, density controls, inline validation polish).

## Legacy Dashboard Deprecation Timeline

- **Target deprecation date:** **May 15, 2026**
- **Soft freeze date for new Blazor dashboard changes:** **April 22, 2026**
- **Parallel run window:** April 6, 2026 → May 14, 2026
- **Rollback window retained:** through **June 12, 2026** (legacy service image preserved for emergency fallback)

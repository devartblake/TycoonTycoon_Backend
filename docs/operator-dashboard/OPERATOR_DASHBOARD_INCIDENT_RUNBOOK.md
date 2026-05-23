# Operator Dashboard Incident Runbook (Django) — April 6, 2026

## Scope

This runbook covers incidents affecting the Django-based `operator-dashboard` service and its required dependencies:

- `backend-api` (`Tycoon.Backend.Api`)
- `sidecar` (`Synaptix.Sidecar`)
- `minio`

## Primary Health Checks

1. Dashboard container liveness:
   - `GET /healthz` on `operator-dashboard`
2. Aggregated operator status:
   - `GET /api/operator/health`
3. Dependency service checks:
   - backend API: app-level health endpoint
   - sidecar: app-level health endpoint
   - MinIO: `/minio/health/live`, `/minio/health/ready`

## Triage Steps

1. **Confirm blast radius**
   - Is issue isolated to one workflow (`/users`, `/moderation/logs`, `/audit/security`, `/media/intent`) or global?
2. **Check login/session path**
   - Validate `/login` behavior and token refresh flow.
3. **Check upstream dependency state**
   - Use `/api/operator/health` and MinIO diagnostics page (`/minio/diagnostics`).
4. **Check BFF endpoint parity**
   - Compare page failures with corresponding `/api/operator/*` endpoint responses.
5. **Check permission profile**
   - Validate operator permissions from session profile (`users:read/write`, `events:read/write`, `questions:write`).

## Mitigation Playbook

### A) Dashboard unavailable, upstream healthy

- Restart only `operator-dashboard`.
- If restart fails, roll back to previous dashboard image tag.
- Keep backend/sidecar/minio unchanged.

### Operator Safety Guardrails (High-Risk Actions)

- Prefer `dry-run` for bulk user actions before live execution.
- Live bulk actions require explicit `YES` confirmation in the UI.
- For incidents, keep bulk write actions disabled until dependency health is green.

### B) Upstream dependency degraded

- Backend API degraded:
  - Pause write actions from dashboard.
  - Keep dashboard read-only where possible.
- Sidecar degraded:
  - Disable/avoid sidecar-backed actions.
- MinIO degraded/offline:
  - Avoid media upload actions.
  - Use diagnostics page and escalate storage/on-call.

### C) Auth/session failures

- Validate admin auth endpoints (`/admin/auth/login`, `/admin/auth/refresh`, `/admin/auth/me`).
- Rotate/confirm auth env configuration and secret wiring.
- If persistent, disable dashboard writes and route operators to fallback path.

## Rollback Strategy

If dashboard incident exceeds SLO or blocks core operator operations:

1. Promote legacy `operator-dashboard-blazor` service for temporary fallback.
2. Keep Django dashboard deployed but removed from primary traffic.
3. Capture incident artifacts and restore Django after root-cause fix.

## Required Incident Artifacts

- Timestamped `/api/operator/health` output
- Relevant `/api/operator/*` failure payloads
- Dashboard container logs around incident window
- Dependency health check outputs
- Operator impact summary (workflows impacted, duration, mitigation taken)

## Escalation

- **P0**: Complete operator outage or auth outage blocking all admin actions.
- **P1**: One or more critical workflows unavailable (users/moderation/audit/media).
- **P2**: Degraded non-critical diagnostics/UI behavior.

## Drill Reference

- Use `docs/OPERATOR_DASHBOARD_DRILL_CHECKLIST.md` for monthly tabletop and quarterly live rollback drills.

# Operator Dashboard Parity Checklist (Django vs Legacy) — April 8, 2026

## Goal

Validate critical operator workflows before hard cutover from `operator-dashboard-blazor` to Django `operator-dashboard`.

## Core Workflow Parity

- [x] Login/logout flow (`/login`, `/logout`)
- [x] Aggregated health/status view (`/`, `/api/operator/health`)
- [x] Users triage page with filters/sorting/pagination
- [x] Users bulk actions with guardrails (`dry-run`, explicit confirmation)
- [x] Moderation logs view + CSV export
- [x] Security audit view + CSV export
- [x] Media intent workflow page
- [x] MinIO diagnostics view

## Safety/Operations

- [x] Permission-gated routes and API responses
- [x] Request/session expiration refresh handling
- [x] Incident runbook in place (`docs/OPERATOR_DASHBOARD_INCIDENT_RUNBOOK.md`)
- [x] Legacy fallback service remains available in compose as `operator-dashboard-blazor`

## Release Gates

- [ ] Execute one full parallel-run validation in staging with real operator accounts.
- [ ] Capture and attach operator sign-off notes.
- [ ] Confirm rollback drill execution timestamp in release notes.

## Status Update — April 8, 2026

- ✅ Auth header/key parity landed between Django and Blazor clients (`ADMIN_OPS_HEADER`, `AdminOps__Key` fallback).
- ✅ Auth-client test coverage expanded for custom ops-header behavior.
- 🚧 Staging parallel-run kickoff document created (`docs/OPERATOR_PARALLEL_RUN_STAGING_2026-04-08.md`).
- 🚧 Parallel-run evidence artifact initialized (`docs/OPERATOR_PARALLEL_RUN_EVIDENCE_2026-04-08.md`).
- ⚠️ Parallel-run sign-off and rollback-drill artifacts are still outstanding release gates.

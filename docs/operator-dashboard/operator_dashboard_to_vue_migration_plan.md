# Tycoon.OperatorDashboard → Synaptix.OperatorDashboard.Vue Migration Plan (Historical — Superseded)

> **Status as of 2026-05-12:** Superseded by `Synaptix.OperatorDashboard.Django`.
>
> This document is preserved for migration history only. It is not an active delivery plan.

## Decision

Django is the primary Operator Dashboard for all operator/admin roles. Super-admin, admin, support, moderation, economy, and audit access should be separated with Django RBAC/permission scopes rather than by routing different roles to different UI frameworks.

## Closed-by-supersession scope

The retired Vue migration plan covered:

- Blazor-to-Vue route inventory.
- API/data contract review.
- Vue information architecture alignment.
- Feature migration batches for users, moderation, economy, anti-cheat, questions, events, seasons, notifications, security audit, and diagnostics.
- Vue smoke tests and API-backed scenario tests.
- A Vue primary cutover with Blazor fallback.

That work should not be resumed for the current operator-dashboard cutover.

## Active direction

Use the Django dashboard and associated cutover docs instead:

- `Synaptix.OperatorDashboard.Django` — canonical operator dashboard.
- `README.md` — current project status.
- `docs/OPERATOR_DASHBOARD_PARITY_CHECKLIST.md` — parity and release gates.
- `docs/STAGING_PARALLEL_RUN_RUNBOOK_2026-05-15.md` — staging validation checklist.

## Historical risks that informed the decision

The Vue split would have kept the following risks open:

- Hidden Blazor-only business logic during migration.
- Role/auth drift between UIs.
- Inconsistent labels and operator confusion.
- Duplicate QA and incident-response paths across two operator systems.

Django-only operator delivery avoids those split-stack risks while preserving role separation through one RBAC model.

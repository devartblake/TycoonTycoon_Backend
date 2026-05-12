# Tycoon.OperatorDashboard.Vue — Deprecated

This project is no longer an active Operator Dashboard migration target.

`Tycoon.OperatorDashboard.Django` is the canonical operator dashboard for all admin/operator roles. Do not add new operator workflows, role splits, or cutover work to this Vue project.

## Historical status

The Vue shell previously contained early migration scaffolding:

- App shell and router scaffold.
- Initial Wave-A routes for Dashboard, Audit Log, and Users.
- RBAC route guard helper skeleton.
- Session bootstrap integration through `/api/me`.
- Shared API client conventions.

The remaining Vue migration work was closed by supersession, not completed in Vue.

## Active replacement

Use `Tycoon.OperatorDashboard.Django` for active operator-dashboard work. Use Django RBAC and permission scopes to distinguish super-admin, admin, support, moderation, economy, audit, and read-only operators.

See also:

- `Tycoon.OperatorDashboard.Vue/DEPRECATED.md`
- `docs/OPERATOR_DASHBOARD_MIGRATION_PLAN.md`
- `docs/OPERATOR_DASHBOARD_CUTOVER_RISK_2026-04-28.md`

# Tycoon.OperatorDashboard.Web — Deprecated

This ASP.NET Web/BFF project is no longer an active Operator Dashboard migration target.

`Tycoon.OperatorDashboard.Django` is the canonical operator dashboard for all admin/operator roles. Do not add new operator workflows, auth/session work, or cutover work to this Web/BFF project.

## Historical status

The Web/BFF project previously contained early migration scaffolding:

- Health/readiness endpoints.
- Admin API proxy baseline.
- Backend base URL configuration.
- Initial domain-specific proxy groups.
- Session bootstrap endpoint.
- Initial typed Wave-A endpoints.
- Header-based auth/session bootstrap middleware.
- Structured error envelope passthrough refinements.

The remaining Web/BFF migration work was closed by supersession, not completed in this project.

## Active replacement

Use `Tycoon.OperatorDashboard.Django` for active operator-dashboard work. Use Django RBAC and permission scopes to distinguish super-admin, admin, support, moderation, economy, audit, and read-only operators.

See also:

- `Tycoon.OperatorDashboard.Web/DEPRECATED.md`
- `docs/OPERATOR_DASHBOARD_MIGRATION_PLAN.md`
- `docs/OPERATOR_DASHBOARD_CUTOVER_RISK_2026-04-28.md`

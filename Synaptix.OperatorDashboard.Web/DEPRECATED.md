# Tycoon.OperatorDashboard.Web — DEPRECATED

This project is **no longer the active migration target** for the Operator Dashboard.

The Vue + ASP.NET Web BFF approach (this project + `Synaptix.OperatorDashboard.Vue`) was
superseded by `Synaptix.OperatorDashboard.Django`, which is now the default operator
dashboard service in Docker Compose and is actively developed toward full Blazor parity.

**Do not add new features or workflows here.**

Role separation belongs in Django RBAC/permission scopes; do not split super-admin/admin/operator workflows between Django and Vue/Web.

Refer to:
- `Synaptix.OperatorDashboard.Django` — active replacement
- `docs/OPERATOR_DASHBOARD_MIGRATION_PLAN.md` — migration history
- `docs/OPERATOR_DASHBOARD_CUTOVER_RISK_2026-04-28.md` — cutover plan and gaps

# Operator Dashboard Migration Plan (Historical — Superseded)

> **Status as of 2026-05-12:** Superseded by the Django operator dashboard.
>
> This Vue + ASP.NET Web BFF migration plan is retained only as historical context. Do not use it as an active backlog, and do not add new operator workflows to `Synaptix.OperatorDashboard.Vue` or `Tycoon.OperatorDashboard.Web`.

## Current decision

`Synaptix.OperatorDashboard.Django` is the canonical Operator Dashboard for all operator/admin roles. Role separation must be implemented through Django-backed RBAC and permission scopes, not by splitting super-admin and operator workflows across different frontend stacks.

## Why this plan was closed

- The Django dashboard reached Wave A/B/C parity with the legacy Blazor dashboard.
- The Vue/Web approach was explicitly deprecated after Django became the active replacement.
- Running separate Django and Vue operator systems would duplicate auth/session handling, RBAC enforcement, audit behavior, QA, and incident-response workflows.
- The remaining Django blockers are operational cutover gates, not a reason to revive Vue/Web.

## Active follow-up work

Use these documents for active work instead:

- `README.md` — current Operator Dashboard status and cutover gates.
- `docs/OPERATOR_DASHBOARD_PARITY_CHECKLIST.md` — Django parity status and remaining release gates.
- `docs/OPERATOR_DASHBOARD_CUTOVER_RISK_2026-04-28.md` — cutover recommendation and risk matrix.
- `docs/STAGING_PARALLEL_RUN_RUNBOOK_2026-05-15.md` — staging parallel-run checklist.
- `docs/REMAINING_TASKS.md` — canonical remaining-work backlog.

## Historical summary of the retired Vue/Web plan

The retired plan was to move the Blazor dashboard into:

- `Synaptix.OperatorDashboard.Vue` for the frontend UI.
- `Tycoon.OperatorDashboard.Web` for the API facade and auth/session layer.

That approach is no longer active. Any remaining work items from the old plan should be treated as closed-by-supersession, not incomplete implementation tasks.

## Historical mapping

| Legacy Blazor page | Former Vue route target | Former Web/BFF module |
|---|---|---|
| `Dashboard.razor` | `/dashboard` | `DashboardController` |
| `Questions.razor` | `/questions` | `QuestionsController` |
| `Events.razor` | `/events` | `EventsController` |
| `Users.razor` | `/users` | `UsersController` |
| `Moderation.razor` | `/moderation` | `ModerationController` |
| `Notifications.razor` | `/notifications` | `NotificationsController` |
| `Economy.razor` | `/economy` | `EconomyController` |
| `AntiCheat.razor` | `/anti-cheat` | `AntiCheatController` |
| `Seasons.razor` | `/seasons` | `SeasonsController` |
| `AuditLog.razor` | `/audit-log` | `AuditLogController` |

## Retired acceptance criteria

The retired Vue/Web plan previously tracked route parity, typed API calls, standardized error handling, loading/error states, action confirmations, tests, and smoke scenarios. These are no longer active Vue/Web acceptance criteria because Django is the canonical dashboard.

# Operator Dashboard Migration Plan (Blazor ➜ Vue + Web)

## Goal
Move Operator Dashboard functionality from `Tycoon.OperatorDashboard` (Blazor) into:
- `Tycoon.OperatorDashboard.Vue` (front-end UI)
- `Tycoon.OperatorDashboard.Web` (BFF/API façade and auth/session integration)

while preserving feature parity, role-based access control, and operational reliability.

## Current State
- Blazor dashboard contains production admin workflows (questions, events, moderation, economy, notifications, anti-cheat, users, audit log, seasons).
- Vue/Web targets exist in the repo workspace but do not yet contain a committed migration execution plan.

## Principles
1. **Strangler migration**: migrate page-by-page, keep Blazor as fallback until each area reaches parity.
2. **No behavior regressions**: each migrated flow must match existing API contracts, validation, and role checks.
3. **Shared contracts first**: define typed DTOs and error envelope handling before moving UI screens.
4. **Operational visibility**: preserve health-check and auditability pathways during transition.

## Workstreams

### 1) Platform Foundations (Week 1)
- [ ] Create and commit scaffolds for `Tycoon.OperatorDashboard.Vue` and `Tycoon.OperatorDashboard.Web`.
- [ ] Establish local dev wiring (ports, CORS, proxy, auth token forwarding).
- [ ] Add CI jobs for Vue lint/typecheck/build and Web build/tests.
- [ ] Add baseline observability/logging in Web BFF.

**Deliverables**
- Vue bootstrapped app with routing/layout shell.
- Web project with health endpoint + authenticated API proxy conventions.
- CI gates for both projects.

### 2) Shared API/Contract Layer (Week 1–2)
- [ ] Define typed API client modules in Vue for each domain (`questions`, `events`, `users`, etc.).
- [ ] Implement standardized error envelope parser and toast/notification handling.
- [ ] Add RBAC guard utilities mirroring existing permissions (e.g. `questions:write`).

**Deliverables**
- `api/*` modules with typed responses and unified error normalization.
- Route guards and permission directives.

### 3) Feature Migration Waves

#### Wave A (Week 2): Read-only/low-risk pages
- [ ] Dashboard overview
- [ ] Audit log
- [ ] Users list

**Exit criteria**: read parity, filters/pagination parity, role-guard parity.

#### Wave B (Week 3): CRUD-heavy workflows
- [ ] Questions (list/create/edit/delete/bulk actions/import)
- [ ] Events (create/open/start/close)
- [ ] Seasons

**Exit criteria**: full action parity, validation parity, bulk action parity.

#### Wave C (Week 4): Operationally sensitive workflows
- [ ] Moderation
- [ ] Notifications (send/schedule/templates/history/dead-letter)
- [ ] Economy
- [ ] Anti-cheat

**Exit criteria**: incident workflows validated with runbooks; no blocking regressions.

### 4) Cutover & Decommission (Week 5)
- [ ] Feature-flag default route to Vue dashboard.
- [ ] Run dual-stack burn-in period (Blazor + Vue) with rollback flag.
- [ ] Remove Blazor pages after sign-off and freeze window.
- [ ] Update docs/runbooks/CI to make Vue+Web canonical.

## Mapping: Blazor Page ➜ Vue Route + Web Module
| Blazor page | Vue route target | Web/BFF module |
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

## Acceptance Criteria (per migrated feature)
- [ ] Route exists in Vue navigation with permission guard.
- [ ] API calls typed; error handling standardized.
- [ ] Empty/loading/error states implemented.
- [ ] Action buttons and confirmations functional.
- [ ] Unit/component tests added (Vue) and endpoint tests added (Web).
- [ ] Smoke scenario documented in QA checklist.

## Risks & Mitigations
- **Risk:** API shape drift breaks UI behavior.
  - **Mitigation:** typed contract adapters + centralized parsing and contract tests.
- **Risk:** Auth/session mismatch between old and new dashboards.
  - **Mitigation:** Web BFF token-forwarding integration tests + staged rollout.
- **Risk:** Feature parity gaps during migration.
  - **Mitigation:** per-wave exit criteria + temporary dual-stack fallback flag.

## Immediate Next Actions (Actionable)
1. Commit minimal Vue/Web project skeletons (no `node_modules` in git).
2. Implement auth + API proxy baseline in `Tycoon.OperatorDashboard.Web`.
3. Migrate **Dashboard**, **AuditLog**, and **Users** first as Wave A.
4. Add CI workflow jobs for Vue/Web build + test gates before Wave B starts.
5. Create a parity checklist issue for each page in the mapping table.

## Kickoff Status
- **Started on April 4, 2026**.
- ✅ Initial migration scaffolding committed for:
  - `Tycoon.OperatorDashboard.Vue` (README, package manifest, local `.gitignore`)
  - `Tycoon.OperatorDashboard.Web` (README + execution notes)
- ✅ Initial `Tycoon.OperatorDashboard.Web` ASP.NET Core baseline added:
  - `Program.cs` with `/health/live`, `/health/ready`
  - `/api/admin/{**path}` proxy skeleton with auth-header forwarding
  - Domain-specific proxy groups for Wave A: `/api/dashboard`, `/api/audit-log`, `/api/users`
  - Session bootstrap endpoint: `/api/me`
  - Typed Wave A endpoints: `/api/dashboard/overview`, `/api/audit-log`, `/api/users`
  - Header-based auth/session bootstrap middleware (`X-Operator-User`, `X-Operator-Permissions`)
  - `appsettings.json` with `Backend:BaseUrl`
- ✅ Initial `Tycoon.OperatorDashboard.Vue` app shell added:
  - Vite + Vue + Vue Router bootstrap
  - Route shell for Wave A pages (`/dashboard`, `/audit-log`, `/users`)
  - RBAC route-guard helper skeleton
  - `/api/me` session bootstrap wired into router guard permission resolution
  - Shared API client conventions (`src/lib/apiClient.js`, `src/api/*`) for Wave A endpoints
- ✅ Plan execution now at **Workstream 1: Platform Foundations**.

## Current Status Update (April 4, 2026)
- ✅ **Web BFF foundations**: health endpoints, generic admin proxy, Wave A domain proxy groups, typed Wave A endpoints, and `/api/me` session bootstrap are in place.
- ✅ **Vue Wave A foundations**: router + RBAC guard + session bootstrap are in place.
- ✅ **Vue data flow kickoff**: shared API client conventions and Wave A API modules now back Dashboard/AuditLog/Users views with loading/error/data states.
- ✅ **Wave A UI parity started**: Audit Log and Users now render table/paging UI (not raw JSON payload dumps).
- ⏳ **Still pending**:
  - Real cookie/JWT auth integration in `Tycoon.OperatorDashboard.Web`
  - Wave A parity completion checklist (filters, paging, table UX, action workflows)

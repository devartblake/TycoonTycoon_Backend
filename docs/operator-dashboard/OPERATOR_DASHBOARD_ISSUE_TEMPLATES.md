# Tycoon.OperatorDashboard — GitHub Issue Templates

> Paste each block below into a new GitHub issue.

---

## ISSUE 1 — Dashboard init/auth cleanup + resilient KPI parsing

### Title
`OperatorDashboard: Dashboard init/auth cleanup and resilient KPI parsing`

### Labels
`area:operator-dashboard`, `type:bug`, `priority:P1`, `tech-debt`, `good-first-fix`

### Owner
`@unassigned`

### Estimate
`0.5d`

### Dependencies
`None`

### Problem
Dashboard init currently calls token attachment twice and reads KPI totals with direct `GetInt32`, which is brittle to token type drift.

### File-level tasks
- [ ] `Tycoon.OperatorDashboard/Components/Pages/Dashboard.razor`
  - [ ] Remove duplicate `TryAttachTokenAsync` call in `OnInitializedAsync`.
  - [ ] Replace direct KPI integer reads with safe conversion helper.
  - [ ] Add fallback behavior for missing/malformed `total` fields.

### Acceptance criteria
- [ ] Exactly one auth attach call in dashboard init.
- [ ] Dashboard no longer throws if `total` fields are strings/null.

---

## ISSUE 2 — Shared JSON-safe helper for Razor pages

### Title
`OperatorDashboard: Add shared JsonElement safe-read helpers and migrate core pages`

### Labels
`area:operator-dashboard`, `type:refactor`, `priority:P1`, `stability`

### Owner
`@unassigned`

### Estimate
`2d`

### Dependencies
`Issue 1`

### Problem
Multiple pages parse raw `JsonDocument` with direct typed accessors (`GetString`, `GetInt32`, etc.), causing render crashes on schema/token drift.

### File-level tasks
- [ ] `Tycoon.OperatorDashboard/Components/Common/JsonSafe.cs` (new)
  - [ ] Implement `GetText`, `GetInt`, `GetBool`, `GetGuid`, `EnumerateArrayOrEmpty`.
- [ ] Migrate usages in:
  - [ ] `Tycoon.OperatorDashboard/Components/Pages/Users.razor`
  - [ ] `Tycoon.OperatorDashboard/Components/Pages/Matches.razor`
  - [ ] `Tycoon.OperatorDashboard/Components/Pages/AuditLog.razor`
  - [ ] `Tycoon.OperatorDashboard/Components/Pages/Moderation.razor`
  - [ ] `Tycoon.OperatorDashboard/Components/Pages/Economy.razor`

### Acceptance criteria
- [ ] No unsafe direct `GetString/GetInt32/GetBoolean/GetGuid` reads in migrated page loops.
- [ ] All migrated pages render with mixed token types.

---

## ISSUE 3 — Users page DTO migration

### Title
`OperatorDashboard: Users page DTO migration and pagination model`

### Labels
`area:operator-dashboard`, `type:enhancement`, `priority:P2`, `api-contract`

### Owner
`@unassigned`

### Estimate
`1d`

### Dependencies
`Issue 2`

### File-level tasks
- [ ] `Tycoon.OperatorDashboard/Models/AdminUsersDtos.cs` (new)
- [ ] `Tycoon.OperatorDashboard/Services/AdminApiClient.cs`
  - [ ] Add typed users list method.
- [ ] `Tycoon.OperatorDashboard/Components/Pages/Users.razor`
  - [ ] Bind table to typed DTOs instead of `JsonDocument`.

### Acceptance criteria
- [ ] Users page no longer depends on `JsonDocument` for table rendering.
- [ ] Existing search + ban/unban behavior preserved.

---

## ISSUE 4 — Matches page DTO migration

### Title
`OperatorDashboard: Matches page typed contract migration`

### Labels
`area:operator-dashboard`, `type:enhancement`, `priority:P2`, `api-contract`

### Owner
`@unassigned`

### Estimate
`0.5d`

### Dependencies
`Issue 2`

### File-level tasks
- [ ] `Tycoon.OperatorDashboard/Models/AdminMatchesDtos.cs` (new)
- [ ] `Tycoon.OperatorDashboard/Services/AdminApiClient.cs`
  - [ ] Add typed matches list method.
- [ ] `Tycoon.OperatorDashboard/Components/Pages/Matches.razor`
  - [ ] Render from DTO model.

### Acceptance criteria
- [ ] Matches page no longer parses JSON directly.
- [ ] Pager behavior remains unchanged.

---

## ISSUE 5 — Security Audit DTO migration

### Title
`OperatorDashboard: Security Audit page typed contract migration`

### Labels
`area:operator-dashboard`, `type:enhancement`, `priority:P2`, `api-contract`

### Owner
`@unassigned`

### Estimate
`0.5d`

### Dependencies
`Issue 2`

### File-level tasks
- [ ] `Tycoon.OperatorDashboard/Models/AdminAuditDtos.cs` (new)
- [ ] `Tycoon.OperatorDashboard/Services/AdminApiClient.cs`
  - [ ] Add typed audit list method.
- [ ] `Tycoon.OperatorDashboard/Components/Pages/AuditLog.razor`
  - [ ] Render from DTO model.

### Acceptance criteria
- [ ] Audit page no longer parses JSON directly.
- [ ] Filters + paging remain functional.

---

## ISSUE 6 — Moderation endpoint contract alignment

### Title
`OperatorDashboard/API: Moderation contract alignment (escalations vs logs)`

### Labels
`area:operator-dashboard`, `area:backend-api`, `type:bug`, `priority:P1`, `api-contract`

### Owner
`@unassigned`

### Estimate
`1d`

### Dependencies
`None`

### File-level tasks
- [ ] `Tycoon.OperatorDashboard/Services/AdminApiClient.cs`
  - [ ] Remove workaround assumptions for moderation routes.
- [ ] `Tycoon.OperatorDashboard/Components/Pages/Moderation.razor`
  - [ ] Ensure tabs map to intentional backend contracts.
- [ ] Backend endpoint/docs as needed (route + payload alignment).

### Acceptance criteria
- [ ] No route workaround comments remain.
- [ ] Moderation tabs each hit explicit, stable contracts.

---

## ISSUE 7 — Permission model hardening by domain

### Title
`OperatorDashboard: Replace broad users:write checks with domain-specific permissions`

### Labels
`area:operator-dashboard`, `type:security`, `priority:P1`, `rbac`

### Owner
`@unassigned`

### Estimate
`1d`

### Dependencies
`Issue 6`

### File-level tasks
- [ ] `Tycoon.OperatorDashboard/Components/Pages/Users.razor`
- [ ] `Tycoon.OperatorDashboard/Components/Pages/Moderation.razor`
- [ ] `Tycoon.OperatorDashboard/Components/Pages/Economy.razor`
- [ ] Any permission mapping service/component used by layout.
  - [ ] Introduce/consume `moderation:write`, `economy:write`, etc.
  - [ ] Update read-only messaging accordingly.

### Acceptance criteria
- [ ] Write actions gated by correct domain permissions.
- [ ] Read-only labels mention accurate missing permission.

---

## ISSUE 8 — Dashboard route smoke suite

### Title
`OperatorDashboard: Route-level smoke tests for all sidebar pages`

### Labels
`area:operator-dashboard`, `type:test`, `priority:P2`, `quality`

### Owner
`@unassigned`

### Estimate
`1d`

### Dependencies
`Issues 2–7`

### File-level tasks
- [ ] `Tycoon.OperatorDashboard/Components/Layout/MainLayout.razor`
  - [ ] Enumerate all routed pages in test matrix.
- [ ] Add test project/files for UI/component smoke checks.
  - [ ] Validate auth-failed/loading/empty/error states for each route.

### Acceptance criteria
- [ ] Every sidebar route has a smoke check.
- [ ] Regression catches render-time exceptions before release.


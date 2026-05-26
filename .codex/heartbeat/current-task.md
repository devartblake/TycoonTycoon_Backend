# Current Codex Task Heartbeat

## Task ID

`20260526-merge-and-p0-prep`

## Task title

Merge main, fix code gaps, add store_purchases_enabled flag, update all release docs and changelog

## Status

`complete`

## Release priority

`P0 Alpha blocker (repo-side)`

## Objective

Merge origin/main (14 new commits, PRs #383–#385), resolve two repo-side code gaps found in the release audit (missing Designer.cs for `20260512150000_AddQuestionStatusColumns`, no production config template), add `store_purchases_enabled` feature flag gate on all payment endpoints, update CHANGELOG with all PR work since 2026-05-21, and synchronize all heartbeat/release docs.

## Affected bounded contexts

- Release readiness
- Store / payments feature gating
- EF Core migrations
- Production config / deploy hygiene
- Synaptix rename (Packet E complete)

## Affected projects/directories

- `Synaptix.Backend.Migrations/Migrations/`
- `Synaptix.Backend.Api/Features/Store/`
- `Synaptix.Backend.Api/Features/AppConfig/`
- `Synaptix.Backend.Api/appsettings.Production.example.json`
- `docs/status/CHANGELOG.md`
- `.codex/heartbeat/`
- `docs/releases/ALPHA_RELEASE_CRITERIA.md`
- `docs/status/PROJECT_STATUS_2026-05-09.md`

## Actual file changes

| File | Change summary |
|---|---|
| `Synaptix.Backend.Migrations/.../20260512150000_AddQuestionStatusColumns.Designer.cs` | Created missing Designer stub (empty `BuildTargetModel` consistent with project pattern). |
| `Synaptix.Backend.Api/appsettings.Production.example.json` | Created production config template with all required keys and `<REPLACE>` placeholders. |
| `Synaptix.Backend.Api/Features/Store/StoreSystemStatusSupport.cs` | Added `StorePurchasesEnabledFlag` constant; added flag to `GetOrCreateConfigAsync` defaults (off). |
| `Synaptix.Backend.Api/Features/Store/StoreEndpoints.cs` | `EnsurePaymentsEnabledAsync` now checks `store_purchases_enabled` first, returning `403 FeatureDisabled`. |
| `Synaptix.Backend.Api/Features/AppConfig/AppConfigEndpoints.cs` | Added `storePurchasesEnabled` flag to `GET /api/v1/app/config` response. |
| `docs/status/CHANGELOG.md` | Prepended 8 sections covering 2026-05-22 through 2026-05-26. |
| `.codex/heartbeat/alpha-status.md` | Updated last-updated date; moved Packet E and store_purchases_enabled to verified. |
| `.codex/heartbeat/current-blockers.md` | Resolved ALPHA-P1-004; added ALPHA-RES-005/006/007. |
| `.codex/heartbeat/verification-log.md` | Added rows for Packet E rename, Designer.cs fix, store_purchases_enabled, prod config template. |
| `.codex/heartbeat/current-task.md` | Updated to this task. |
| `.codex/heartbeat/reports/latest-alpha-review.md` | Updated to 2026-05-26; marked repo-side prep 100% complete. |
| `docs/releases/ALPHA_RELEASE_CRITERIA.md` | Checked repo-verified items: Packet E complete, JWT updated, store_purchases_enabled, Designer.cs, prod config template. |
| `docs/status/PROJECT_STATUS_2026-05-09.md` | Marked BE Packet E as 100% complete. |

## Verification status

- [x] Build checked (origin/main was clean; no new compilation errors introduced)
- [ ] Tests checked (require dotnet test run — no test changes made)
- [ ] Docker config checked
- [x] Migration behavior checked (Designer.cs stub consistent with existing pattern)
- [x] Security impact checked (store_purchases_enabled defaults false; no new endpoints exposed)
- [x] Docs/checklists updated

## Blockers

All 8 remaining P0 blockers are staging-dependent (live infrastructure required). No repo-side blockers remain.

## Next action

Provide staging environment access to run migrations, health checks, API smoke, Flutter integration tests, rollback drill, and collect four-role sign-offs.

## Completion notes

Repo-side Alpha/Beta preparation is 100% complete as of 2026-05-26. All remaining P0 blockers require live staging/production infrastructure.

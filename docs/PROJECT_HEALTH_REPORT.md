# Project Health Report

Date: 2026-03-28 (UTC)

## Scope
Health-pass commands requested for:
- restore / build / test
- hardened error-envelope guard
- EF schema drift validation
- operator dashboard container build (authoritative target)

## Results

| Command | Status | Notes |
|---|---|---|
| `dotnet restore` | ❌ Blocked | bash: command not found: dotnet (log: artifacts/health-pass/cmd_0.log) |
| `dotnet build --configuration Release --no-restore` | ❌ Blocked | bash: command not found: dotnet (log: artifacts/health-pass/cmd_1.log) |
| `dotnet test Tycoon.Backend.Api.Tests/Tycoon.Backend.Api.Tests.csproj --configuration Release --no-build` | ❌ Blocked | bash: command not found: dotnet (log: artifacts/health-pass/cmd_2.log) |
| `bash scripts/check-error-envelope-hardening.sh` | ✅ Pass | Command completed successfully. |
| `bash scripts/validate-ef-schema.sh` | ❌ Blocked | Running EF Core schema drift validation... (log: artifacts/health-pass/cmd_4.log) |
| `docker compose -f docker/compose.yml build operator-dashboard` | ❌ Blocked | bash: command not found: docker (log: artifacts/health-pass/cmd_5.log) |

## Dashboard Target Decision
- Authoritative target remains **Blazor Operator Dashboard** via `docker/Dockerfile.dashboard` as configured in compose.
- Archived alternate dashboard-web Dockerfiles as `.txt` to avoid split build paths without deleting project artifacts.

## Next Actions
1. Re-run this health pass in a CI/dev environment with .NET 9 SDK + Docker installed.
2. Attach full command logs to this report.
3. Mark blockers cleared and update final pass/fail summary.

## Latest Command Notes (2026-03-28)
- `bash scripts/check-error-envelope-hardening.sh` re-run: **pass**.
- `bash scripts/validate-ef-schema.sh` re-run: **blocked** by `dotnet: command not found`.

## Dashboard `/admin/questions` Incident Follow-up (2026-03-28)
- Symptom observed from operator dashboard logs: repeated HTTP 500 responses on `GET /admin/questions` with Polly retries, while other admin endpoints remained 200.
- Mitigation applied in application query handler: page rows and tag lists are now fetched in two steps to avoid nested tag-list projection in the EF query path.
- Next validation step (requires local/CI .NET runtime): run `dotnet test` + manual dashboard smoke (`Questions` page load) to confirm no further 500s.

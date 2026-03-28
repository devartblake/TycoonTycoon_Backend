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
| `dotnet restore` | ✅ Pass | Command completed successfully. |
| `dotnet build --configuration Release --no-restore` | ✅ Pass | Command completed successfully. |
| `dotnet test Tycoon.Backend.Api.Tests/Tycoon.Backend.Api.Tests.csproj --configuration Release --no-build` | ❌ Fail | Test run for C:\Users\lmxbl\Documents\TycoonTycoon_Backend\Tycoon.Backend.Api.Tests\bin\Release\net9.0\Tycoon.Backend.Api.Tests.dll (.NETCoreApp,Version=v9.0) (log: artifacts/health-pass/cmd_2.log) |
| `bash scripts/check-error-envelope-hardening.sh` | ❌ Blocked | scripts/check-error-envelope-hardening.sh: line 23: rg: command not found (log: artifacts/health-pass/cmd_3.log) |
| `bash scripts/validate-ef-schema.sh` | ❌ Fail | Running EF Core schema drift validation... (log: artifacts/health-pass/cmd_4.log) |
| `docker compose -f docker/compose.yml build operator-dashboard` | ✅ Pass | Command completed successfully. |

## Dashboard Target Decision
- Authoritative target remains **Blazor Operator Dashboard** via `docker/Dockerfile.dashboard` as configured in compose.
- Archived alternate dashboard-web Dockerfiles as `.txt` to avoid split build paths without deleting project artifacts.

## Next Actions
1. Ensure prerequisites are installed locally: [health-pass-prereqs] dotnet already available: 9.0.309
[health-pass-prereqs] dotnet-ef already available.
[health-pass-prereqs] docker is available and daemon is reachable.
[health-pass-prereqs] All health-pass prerequisites are ready.
[health-pass-prereqs] Now run: bash scripts/run-health-pass.sh.
2. Re-run this health pass in CI/dev with .NET 9 SDK + Docker available.
3. Attach full command logs if any command fails.
4. Mark blockers cleared and update final pass/fail summary.

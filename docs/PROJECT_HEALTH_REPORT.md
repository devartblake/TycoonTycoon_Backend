# Project Health Report

Date: 2026-04-04 (UTC)

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
| `bash scripts/validate-ef-schema.sh` | ❌ Blocked | scripts/validate-ef-schema.sh: line 62: dotnet: command not found (log: artifacts/health-pass/cmd_4.log) |
| `docker compose -f docker/compose.yml build operator-dashboard` | ❌ Blocked | bash: command not found: docker (log: artifacts/health-pass/cmd_5.log) |

## Dashboard Target Decision
- Authoritative target remains **Blazor Operator Dashboard** via `docker/Dockerfile.dashboard` as configured in compose.
- Archived alternate dashboard-web Dockerfiles as `.txt` to avoid split build paths without deleting project artifacts.

## Next Actions
1. Ensure prerequisites are installed locally: `bash scripts/setup-health-pass-prereqs.sh`.
2. Re-run this health pass in CI/dev with .NET 9 SDK + Docker available.
3. Attach full command logs if any command fails.
4. Mark blockers cleared and update final pass/fail summary.

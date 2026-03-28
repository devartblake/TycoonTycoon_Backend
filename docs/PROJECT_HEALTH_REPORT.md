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
| `dotnet restore` | ❌ Blocked | bash: command not found: dotnet |
| `dotnet build --configuration Release --no-restore` | ❌ Blocked | bash: command not found: dotnet |
| `dotnet test Tycoon.Backend.Api.Tests/Tycoon.Backend.Api.Tests.csproj --configuration Release --no-build` | ❌ Blocked | bash: command not found: dotnet |
| `bash scripts/check-error-envelope-hardening.sh` | ✅ Pass | Command completed successfully. |
| `bash scripts/validate-ef-schema.sh` | ❌ Blocked | Running EF Core schema drift validation... |
| `docker compose -f docker/compose.yml build operator-dashboard` | ❌ Blocked | bash: command not found: docker |

## Dashboard Target Decision
- Authoritative target remains **Blazor Operator Dashboard** via `docker/Dockerfile.dashboard` as configured in compose.
- Archived alternate dashboard-web Dockerfiles as `.txt` to avoid split build paths without deleting project artifacts.

## Next Actions
1. Re-run this health pass in CI/dev with .NET 9 SDK + Docker installed.
2. Attach full command logs if any command fails.
3. Mark blockers cleared and update final pass/fail summary.

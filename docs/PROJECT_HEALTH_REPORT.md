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
| `dotnet restore` | ❌ Blocked | `dotnet: command not found` in current environment |
| `dotnet build --configuration Release --no-restore` | ❌ Blocked | `dotnet: command not found` |
| `dotnet test Tycoon.Backend.Api.Tests/Tycoon.Backend.Api.Tests.csproj --configuration Release --no-build` | ❌ Blocked | `dotnet: command not found` |
| `bash scripts/check-error-envelope-hardening.sh` | ✅ Pass | Hardened endpoint scan passed |
| `bash scripts/validate-ef-schema.sh` | ❌ Blocked | Script starts but fails at `dotnet ef` step because dotnet CLI is unavailable |
| `docker compose -f docker/compose.yml build operator-dashboard` | ❌ Blocked | `docker: command not found` |

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

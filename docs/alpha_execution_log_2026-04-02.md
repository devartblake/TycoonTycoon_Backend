# Alpha Execution Log — 2026-04-02

## Requested immediate actions
1. Build + migration gate
2. API readiness checks
3. Strict IAP validation in non-prod

## Action 1 — Build + migration gate
Commands attempted:
- `dotnet build Tycoon.sln`
- `dotnet ef migrations add AlphaReadinessCheck --project Tycoon.Backend.Migrations --startup-project Tycoon.Backend.Api`

Result:
- Blocked in this container because `dotnet` is not installed (`/bin/bash: dotnet: command not found`).

## Action 2 — API readiness checks (route-level coverage)
Command used:
- `rg -n "Map(Get|Post|Patch|Put|Delete)\(" Tycoon.Backend.Api/Features/{Auth,Users,Questions,Leaderboards,Mobile/Economy,Store,Crypto} -g '*.cs'`

Result:
- Verified route surfaces for requested alpha domains are present in code:
  - Auth endpoints
  - Profile (`/users/me`, preferences)
  - Questions (`/questions/set`, `/questions/check`, `/questions/check-batch`)
  - Leaderboards
  - Economy state (`/mobile/economy/state` and related routes)
  - Store (`/store/catalog`, `/store/purchase`, `/store/iap/validate`)
  - Crypto (`/crypto/link-wallet`, `/crypto/balance/{playerId}`, `/crypto/history/{playerId}`, `/crypto/withdraw`)

## Action 3 — Strict IAP validation in non-prod
Changes made:
- Set `Iap:EnableStrictValidation` to `true` in `Tycoon.Backend.Api/appsettings.Development.json`.
- Updated `/store/iap/validate` behavior:
  - when strict mode is on, endpoint now validates that required provider configuration is present
  - returns `503 IAP_STRICT_CONFIG_MISSING` when strict mode is enabled but required provider config placeholders were not replaced

## Follow-up still required in your local machine
- Install/use local .NET SDK to complete build + migration gate.
- Start API and execute request-level smoke tests for P0 endpoints.
- Provide real non-placeholder IAP provider config values for Apple/Google strict mode tests.

## Next-step command runbook (local)

### 1) Build + migration gate
```bash
dotnet --info
dotnet build Tycoon.sln
dotnet ef migrations list --project Tycoon.Backend.Migrations --startup-project Tycoon.Backend.Api
dotnet ef database update --project Tycoon.Backend.Migrations --startup-project Tycoon.Backend.Api
```

### 2) Start API (development)
```bash
ASPNETCORE_ENVIRONMENT=Development dotnet run --project Tycoon.Backend.Api
```

### 3) P0 smoke API checks (examples)
```bash
# one-command smoke script
./scripts/alpha-p0-smoke.sh

# optional overrides:
# - BASE_URL for non-default port/host
# - EMAIL/PASSWORD for login creds
# - JQ_BIN if jq is installed at a custom path
# - SMOKE_MODE=routes for CI/static route-map validation (no running API required)
# Script now supports either jq or python3 for JSON parsing.
BASE_URL=http://localhost:5000 EMAIL=you@example.com PASSWORD='***' ./scripts/alpha-p0-smoke.sh
SMOKE_MODE=routes ./scripts/alpha-p0-smoke.sh

# auth login (replace payload)
curl -sS -X POST http://localhost:5000/auth/login -H 'Content-Type: application/json' -d '{\"email\":\"demo@example.com\",\"password\":\"demo\"}'

# questions set
curl -sS \"http://localhost:5000/questions/set?count=5\"

# store catalog
curl -sS \"http://localhost:5000/store/catalog\"
```

Windows PowerShell option:
```powershell
pwsh ./scripts/alpha-p0-smoke.ps1 -BaseUrl http://localhost:5000 -Email you@example.com -Password '***'
```

Important:
- `alpha-p0-smoke.sh` is a **Bash script** and should be run with `bash`/`sh`, not with `python`.

### 4) Strict IAP validation prechecks
Before calling `/store/iap/validate` in Development, replace placeholders in `Tycoon.Backend.Api/appsettings.Development.json`:
- `Iap:AppleSharedSecret`
- `Iap:GooglePackageName`
- `Iap:GoogleServiceAccountJsonPath`

Without real values, strict mode intentionally returns:
- `503` + `IAP_STRICT_CONFIG_MISSING`

---

## Status update — 2026-04-03 (UTC)

Continued NOW-step execution with backend-only automation.

### Added helper
- `scripts/alpha-now-status.sh`
  - Purpose: quick backend-only status gate for NOW items.
  - Checks:
    1. whether `.NET` SDK is available (and optionally runs `dotnet build` when `RUN_BUILD=true`)
    2. route-mode P0 smoke gate
    3. optional PowerShell route-mode smoke gate

### Command run
```bash
./scripts/alpha-now-status.sh
```

### Current status in this environment
- `dotnet` SDK: unavailable (build/migration still blocked here)
- Bash route smoke: pass
- PowerShell route smoke: skipped by default (can enable with `RUN_PWSH_ROUTE_SMOKE=true`)

### Remaining work (backend, non-frontend)
1. Execute build + migration gates in .NET-capable runner/workstation.
2. Run live-mode smoke (`SMOKE_MODE=live`) against a running API instance.
3. Validate strict IAP with real provider config (`EXPECT_IAP_STRICT_READY=true`).
4. Record Go/No-Go and deferred backend items after live gates pass.

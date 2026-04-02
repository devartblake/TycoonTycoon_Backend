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

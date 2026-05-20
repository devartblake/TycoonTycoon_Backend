# Alpha/Beta Status Board

Last updated: `<YYYY-MM-DD HH:mm TZ>`

## Overall release status

Status: `not-started`

## P0 Alpha blockers

| Area | Status | Owner/Agent | Notes |
|---|---:|---|---|
| Local Docker startup | not-started | devops-docker | |
| PostgreSQL migrations | not-started | efcore-migration | |
| Auth/session identity | not-started | dotnet-api | |
| `/users/me/wallet` authoritative read | not-started | wallet-economy | |
| Match submit idempotency | not-started | wallet-economy | |
| Reward claim authority | not-started | wallet-economy | |
| Store catalog fallback | not-started | backend-api | |
| Admin endpoint protection | not-started | security-kms | |
| Critical tests pass | not-started | test-quality | |

## P1 Alpha important

| Area | Status | Owner/Agent | Notes |
|---|---:|---|---|
| Feature flags for non-essential modules | not-started | repo-hygiene | |
| MinIO seed loading | not-started | devops-docker | |
| Health/readiness checks | not-started | observability | |
| Structured logs/correlation | not-started | observability | |
| Sidecar fallback behavior | not-started | personalization-sidecar | |
| CI build/test path | not-started | test-quality | |

## Post-Alpha deferred

| Area | Reason deferred | Revisit trigger |
|---|---|---|
| Advanced personalization tuning | Not required for Alpha stability | After core loop telemetry is stable |
| Full admin dashboard parity | Large surface area | After API contract freeze |
| Deep analytics dashboards | Not release-blocking | After event schema stabilizes |
| Multi-region deployment | Infrastructure maturity task | After Beta feedback |

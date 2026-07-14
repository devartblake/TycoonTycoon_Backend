# Local Dev Happy Path

**Goal:** cold clone → running API stack with health green, without reading the full docs tree.

**OS:** Windows, macOS, or Linux with Docker Desktop / Engine.  
**SDK:** match `global.json` (.NET 10).

---

## 0. Prerequisites

- Git  
- Docker with Compose v2  
- .NET SDK from `global.json`  
- Optional: Python 3.12 (Django operator dashboard), Node 20+ (React dashboard)

```bash
git clone <this-repo>
cd TycoonTycoon_Backend   # or your checkout path
dotnet --info             # confirm SDK
docker version
```

---

## 1–2. Preferred: one-command bootstrap

From repo root:

```bash
# Linux / macOS / Git Bash
make dev
# or: ./scripts/bootstrap-local.sh

# Windows PowerShell
make dev-win
# or: .\scripts\bootstrap-local.ps1
```

This runs Setup CLI secret/env generation and brings up the compose stack (setup → migration → API).  
See also [`../dev-secrets.md`](../dev-secrets.md). Ensure any `docker/.env` is **gitignored**.

### Manual alternative

```bash
dotnet run --project Synaptix.Setup -- init-local
docker compose -f docker/compose.yml up -d --build
```

Order of one-shot services is important and already encoded in compose:

1. **setup** — MinIO/seeds/admin bootstrap  
2. **migration** — EF migrate + seed readiness  
3. **backend-api** — depends on healthy deps / completed migration where configured  

First boot can take several minutes (image pulls + migrations).

Useful follow-ups:

```bash
docker compose -f docker/compose.yml ps
docker compose -f docker/compose.yml logs -f backend-api
```

---

## 3. Health checks

Adjust host/port to your compose mapping (Traefik or published API port). Typical probes:

```bash
# Liveness / ready (names may be /health, /healthz, /alive — check compose Traefik labels)
curl -sf http://localhost:<api-port>/healthz || curl -sf http://localhost:<api-port>/health
```

CI health script (when stack is up):

```bash
bash scripts/run-health-pass.sh
```

---

## 4. Run tests locally (optional but recommended)

```bash
# Main API tests (needs Redis — CI uses a dockerized Redis with known password)
dotnet test Synaptix.Backend.Api.Tests/Synaptix.Backend.Api.Tests.csproj -c Release

# KMS + Compliance unit tests
dotnet test Synaptix.Security.Kms.Tests/Synaptix.Security.Kms.Tests.csproj -c Release

# Route parity only
dotnet test Synaptix.Backend.Api.Tests/Synaptix.Backend.Api.Tests.csproj --filter "FullyQualifiedName~RouteParityContractTests"
```

---

## 5. KMS / secure channel (optional)

Minimal KMS + Vault style stack is documented in:

[`../security/SYNAPTIX_SECURITY_RUNNING_GUIDE.md`](../security/SYNAPTIX_SECURITY_RUNNING_GUIDE.md)

Hybrid post-quantum suite:

```json
"Kms": { "Suites": { "EnableHybridPq": false } }
```

Leave **false** for local Alpha unless you intentionally exercise Hybrid (platform needs X25519 + ML-KEM).

---

## 6. Operator dashboards

| Surface | Path | Notes |
|---------|------|--------|
| **React (canonical ops)** | `Synaptix.OperatorDashboard.React` | `npm install && npm run dev` — primary operator UI |
| Django (legacy / fallback) | `Synaptix.OperatorDashboard.Django` | No new features; optional until React cutover complete |
| Blazor / Vue / Web | Deprecated | Do not add new workflows |

Direction: [`../operator-dashboard/OPERATOR_DASHBOARD_DIRECTION_2026-07-13.md`](../operator-dashboard/OPERATOR_DASHBOARD_DIRECTION_2026-07-13.md).

---

## 7. When something breaks

| Symptom | Check |
|---------|--------|
| Compose refuses to start | Missing `${VAR:?…}` — re-run `Synaptix.Setup -- init-local` |
| API crash-loops | `docker compose logs backend-api migration setup` |
| Tests fail on Redis auth | Match Redis password to test host config / CI redis container |
| EF drift | `bash scripts/validate-ef-schema.sh` |
| Auth / 501 on OAuth buttons | Expected until providers configured; client gates external auth |

More: [`../operator-dashboard/OPERATOR_DASHBOARD_INCIDENT_RUNBOOK.md`](../operator-dashboard/OPERATOR_DASHBOARD_INCIDENT_RUNBOOK.md), root `DEPLOYMENT.md` / `Docker.md`.

---

## 8. Stop / reset

```bash
docker compose -f docker/compose.yml down
# nuclear (deletes volumes):
docker compose -f docker/compose.yml down -v
```

---

## Next reading (only if needed)

1. Program plan: [`../status/BCE_EXECUTION_PLAN.md`](../status/BCE_EXECUTION_PLAN.md)  
2. Alpha backlog: [`../alpha-beta/REMAINING_TASKS.md`](../alpha-beta/REMAINING_TASKS.md)  
3. Docs index: [`../README.md`](../README.md)  

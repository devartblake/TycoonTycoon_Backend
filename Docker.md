# Docker Setup – TycoonTycoon Backend

This document describes how to run **TycoonTycoon_Backend** locally using Docker for infrastructure services (PostgreSQL, MongoDB, Redis, Elasticsearch), with two supported workflows:

* **Option A (recommended for development):** Dockerized infrastructure + run .NET services on the host
* **Option B:** Fully Dockerized backend API + infrastructure

The setup is designed to support fast iteration, reliable local state, and clean separation between **infrastructure** and **application lifecycle**.

> **Current ownership:** `Synaptix.Setup` provisions infrastructure and uploads bundled seed files; `Synaptix.MigrationService` applies EF migrations and application-data seeds; `Synaptix.OperatorDashboard.Django` is the canonical operator dashboard. Legacy Tycoon/Blazor names elsewhere in this long-running guide are historical references.

---

## 1. Repository Layout (Docker-related)

All Docker assets live under the `docker/` directory.

```text
TycoonTycoon_Backend/
├─ Docker.md                 # ← this document
├─ docker/
│  ├─ compose.yml            # main docker-compose stack
│  ├─ compose.prod.yml       # production overrides (hides ports)
│  ├─ .env.example           # environment template
│  ├─ Makefile               # convenience commands
│  ├─ Dockerfile.api         # Backend API container
│  ├─ Dockerfile.migrate     # Migration runner (runs once)
│  ├─ Dockerfile.sidecar     # FastAPI Python sidecar
│  ├─ Dockerfile.dashboard   # Blazor Server operator dashboard
│  └─ init/
│     ├─ postgres/
│     │  └─ 01-init.sh
│     └─ mongo/
│        └─ 01-init.js
├─ Synaptix.Backend.Api/
├─ Synaptix.MigrationService/
├─ Tycoon.OperatorDashboard/
└─ Synaptix.Sidecar/
```

This keeps Docker concerns isolated and avoids polluting the solution root.

---

## 2. Services Provided by Docker

The Docker stack provides infrastructure and the fully Dockerized application lifecycle.

| Service                       | Port(s)       | Purpose                                         |
| ----------------------------- | ------------- | ----------------------------------------------- |
| PostgreSQL                    | 5432          | Primary relational database (EF Core, Hangfire) |
| MongoDB                       | 27017         | Eventing, analytics, document data              |
| Redis                         | 6379          | Caching, SignalR backplane                      |
| Elasticsearch                 | 9200          | Search, analytics                               |
| RabbitMQ                      | 5672 / 15672  | Message broker, background jobs                 |
| MinIO                         | 9000          | S3-compatible object storage                    |
| MinIO Console *(dev profile)* | 9001          | MinIO browser UI                                |
| Kibana *(dev profile)*        | 5601          | Elasticsearch UI                                |
| pgAdmin *(dev profile)*       | 5050          | PostgreSQL UI                                   |
| Mongo Express *(dev profile)* | 8081          | MongoDB UI                                      |
| Prometheus *(dev profile)*    | 9090          | Metrics scraper                                 |
| Grafana *(dev profile)*       | 3000          | Metrics dashboards                              |
| DBGate *(dev profile)*        | 3001          | Multi-database browser UI                       |

**Application services** (Option B — fully Dockerized):

| Service              | Port | Dockerfile              | Description                      |
| -------------------- | ---- | ----------------------- | -------------------------------- |
| `migration`          | —    | `Dockerfile.migrate`    | Runs migrations once then exits  |
| `setup`              | -    | `Dockerfile.setup`      | Provisions services and uploads bundled seeds, then exits |
| `backend-api`        | 5000 | `Dockerfile.api`        | Main REST API + SignalR          |
| `sidecar`            | 8100 | `Dockerfile.sidecar`    | FastAPI ML/analytics/webhooks    |
| `operator-dashboard` | 8200 | `Dockerfile.dashboard-django` | Canonical Django operator dashboard |
| `operator-dashboard-blazor` | 8201 | `Dockerfile.dashboard` | Legacy rollback/comparison dashboard |

> **Important:**
> Schema migrations and application-data seeding are **not** done by the API.
> `Synaptix.Setup` owns infrastructure provisioning and bundled seed upload.
> `Synaptix.MigrationService` owns EF migrations and application-data seeding.

This is enforced explicitly in `Program.cs`:

> “Do NOT migrate here anymore. Synaptix.MigrationService owns migrations + seeding now.”

---

## 3. Environment Configuration

### 3.1 Generate your local `.env`

```bash
dotnet run --project Synaptix.Setup -- init-local
dotnet run --project Synaptix.Setup -- validate --local
```

`docker/.env.example` is a placeholder template, not a runnable credential file. `init-local` generates strong local secrets and writes `docker/.env`. Generated local secrets and bootstrap files are ignored by Git.

### 3.2 Recommended local bootstrap

```powershell
./scripts/bootstrap-local.ps1
```

```bash
./scripts/bootstrap-local.sh
```

The wrapper performs the intended startup chain:

```text
init-local -> validate -> infrastructure -> setup -> migration -> backend API -> Django dashboard
```

For a fully Dockerized run after `docker/.env` exists:

```bash
docker compose --env-file docker/.env -f docker/compose.yml up -d --build
```

---

## 4. Common Commands

All commands assume execution from **repo root**.

### Bring infrastructure up

```bash
make -f docker/Makefile up
```

### Bring infrastructure + dev tools up

```bash
make -f docker/Makefile up-dev
```

### Stop everything

```bash
make -f docker/Makefile down
```

### View logs

```bash
make -f docker/Makefile logs
```

### Destroy volumes (⚠ data loss)

```bash
make -f docker/Makefile clean
```

---

## 5. Option A (Recommended): Docker Infra + Host .NET

This is the **primary development workflow**.

### 5.1 Start infrastructure

```bash
make -f docker/Makefile up
```

Ensure all services are healthy:

```bash
make -f docker/Makefile health
```

---

### 5.2 Run migrations

```bash
dotnet run --project Synaptix.MigrationService/Synaptix.MigrationService.csproj
```

This will:

* Apply EF Core migrations
* Create schemas/tables
* Seed reference data

---

### 5.3 Run the API on the host

```bash
dotnet run --project Synaptix.Backend.Api/Synaptix.Backend.Api.csproj
```

The API:

* Binds normally via `ASPNETCORE_URLS`
* Connects to Docker services via **localhost**
* Uses Redis automatically for SignalR if configured

Typical local connection strings:

```text
Postgres:
Host=localhost;Port=5432;Database=tycoon_db;Username=tycoon_user;Password=...

Mongo:
mongodb://tycoon_app_user:...@localhost:27017/tycoon_db?authSource=tycoon_db

Redis:
localhost:6379,password=...

Elasticsearch:
http://localhost:9200

MinIO (S3-compatible):
Endpoint=localhost:9000;AccessKey=tycoon_minio_user;SecretKey=...;UseSSL=false
```

---

## 6. Option B: Fully Dockerized Backend (Runblocks)

Use this when you want:

* CI/CD parity
* Full container isolation
* No local .NET SDK dependency

---

### 6.1 Build + run API container

Uncomment the `backend-api` service in `docker/compose.yml`, then:

```bash
docker compose -f docker/compose.yml up -d --build
```

This will:

* Build `docker/Dockerfile.api`
* Start API after all infra services are healthy
* Inject connection strings via environment variables

The API will be exposed on:

```text
http://localhost:5000
```

---

### 6.2 Migrations (still required)

Even in Option B, **migrations are not run automatically**.

Run once on the host **or** create a one-off container:

```bash
dotnet run --project Synaptix.MigrationService/Synaptix.MigrationService.csproj
```

> Intentional design choice: migrations are explicit and controlled.

---

## 7. Dev Tooling URLs (Option A or B)

| Tool                | URL                                                              |
| ------------------- | ---------------------------------------------------------------- |
| Swagger             | [http://localhost:5000/swagger](http://localhost:5000/swagger)   |
| Hangfire            | [http://localhost:5000/hangfire](http://localhost:5000/hangfire) |
| Operator Dashboard  | [http://localhost:8200](http://localhost:8200)                   |
| FastAPI Sidecar     | [http://localhost:8100/docs](http://localhost:8100/docs)         |
| MinIO Console       | [http://localhost:9001](http://localhost:9001)                   |
| pgAdmin             | [http://localhost:5050](http://localhost:5050)                   |
| Mongo Express       | [http://localhost:8081](http://localhost:8081)                   |
| Kibana              | [http://localhost:5601](http://localhost:5601)                   |
| Prometheus          | [http://localhost:9090](http://localhost:9090)                   |
| Grafana             | [http://localhost:3000](http://localhost:3000)                   |
| DBGate              | [http://localhost:3001](http://localhost:3001)                   |

---

## 8. Notes on Aspire / AppHost

If you later choose to use `Tycoon.AppHost` (Aspire-style orchestration):

* This Docker setup remains valid
* Aspire can replace Docker Compose **incrementally**
* Redis / Postgres naming already matches Aspire conventions (`cache`, `tycoon-db`)

---

## 9. Troubleshooting

### Common Issues and Solutions

#### Issue: Containers fail to start

**Symptoms:**
- `docker compose up` exits with errors
- Services show "unhealthy" status
- Containers restart continuously

**Solutions:**

1. **Check Docker resources**
   ```bash
   docker system df
   docker system prune  # Clean up unused resources
   ```

2. **Verify Docker is running**
   ```bash
   docker info
   ```

3. **Check for port conflicts**
   ```bash
   # View all running containers
   docker ps -a
   
   # Check specific port usage (example for PostgreSQL)
   lsof -i :5432  # macOS/Linux
   netstat -ano | findstr :5432  # Windows
   ```

4. **Review service logs**
   ```bash
   docker compose -f docker/compose.yml logs [service-name]
   # Examples:
   docker compose -f docker/compose.yml logs postgres
   docker compose -f docker/compose.yml logs mongodb
   ```

#### Issue: Database connection errors

**Symptoms:**
- API throws "connection refused" errors
- Migration service fails with timeout

**Solutions:**

1. **Verify services are healthy**
   ```bash
   make -f docker/MakeFile health
   ```

2. **Wait for services to fully initialize**
   - PostgreSQL: 10-15 seconds
   - MongoDB: 15-20 seconds
   - Elasticsearch: 30-60 seconds

3. **Check connection strings**
   - Ensure `appsettings.json` matches `docker/.env`
   - Database name: `tycoon_db`
   - Username: `tycoon_user`
   - Password: `tycoon_password_123`

4. **Test database connectivity directly**
   ```bash
   # PostgreSQL
   docker compose -f docker/compose.yml exec postgres \
     psql -U tycoon_user -d tycoon_db -c "SELECT version();"
   
   # MongoDB
   docker compose -f docker/compose.yml exec mongodb \
     mongosh -u tycoon_admin -p tycoon_mongo_password_123 \
     --eval "db.adminCommand('ping')"
   
   # Redis
   docker compose -f docker/compose.yml exec redis \
     redis-cli -a tycoon_redis_password_123 ping
   ```

#### Issue: Elasticsearch yellow/red health status

**Symptoms:**
- Elasticsearch health check fails
- Slow response times
- Index operations fail

**Solutions:**

1. **Check cluster health**
   ```bash
   curl -u elastic:tycoon_elastic_password_123 \
     http://localhost:9200/_cluster/health?pretty
   ```

2. **Increase JVM heap size (if needed)**
   - Edit `docker/.env`:
     ```bash
     ES_JAVA_OPTS=-Xms1g -Xmx1g  # Increase to 1GB
     ```
   - Restart Elasticsearch:
     ```bash
     docker compose -f docker/compose.yml restart elasticsearch
     ```

3. **Wait for initialization**
   - Elasticsearch can take 30-60 seconds to fully start
   - Yellow status is normal for single-node development

4. **View Elasticsearch logs**
   ```bash
   docker compose -f docker/compose.yml logs elasticsearch | tail -100
   ```

#### Issue: Migration service fails

**Symptoms:**
- Migrations don't complete
- "Migration failed" errors
- Database schema not created

**Solutions:**

1. **Ensure dependencies are healthy**
   ```bash
   make -f docker/MakeFile health
   ```

2. **Run migrations manually**
   ```bash
   dotnet run --project Synaptix.MigrationService/Synaptix.MigrationService.csproj
   ```

3. **Check migration logs**
   ```bash
   make -f docker/MakeFile migration-logs
   ```

4. **Reset database (⚠️ data loss)**
   ```bash
   make -f docker/MakeFile clean
   make -f docker/MakeFile up
   make -f docker/MakeFile migrate
   ```

#### Issue: Performance problems

**Symptoms:**
- Slow API responses
- High CPU/memory usage
- Containers crashing with OOM errors

**Solutions:**

1. **Allocate more resources to Docker**
   - Docker Desktop → Settings → Resources
   - Recommended: 4 CPUs, 8GB RAM minimum

2. **Limit Elasticsearch memory**
   - Already configured via `ES_JAVA_OPTS` in `.env`
   - Default: 512MB, can increase if needed

3. **Disable dev profile services**
   ```bash
   # Start only essential infrastructure
   make -f docker/MakeFile up
   ```

4. **Monitor resource usage**
   ```bash
   docker stats
   ```

---

## 10. Database Migration Workflow

### Standard Migration Process

1. **Start infrastructure**
   ```bash
   make -f docker/MakeFile up
   ```

2. **Wait for services to be healthy**
   ```bash
   make -f docker/MakeFile health
   ```

3. **Run migrations**
   ```bash
   make -f docker/MakeFile migrate
   # OR manually:
   dotnet run --project Synaptix.MigrationService/Synaptix.MigrationService.csproj
   ```

### Creating New Migrations

When you change entity models:

1. **Navigate to Infrastructure project**
   ```bash
   cd Tycoon.Backend.Infrastructure
   ```

2. **Create migration**
   ```bash
   dotnet ef migrations add YourMigrationName \
     --startup-project ../Synaptix.Backend.Api
   ```

3. **Review generated migration**
   - Check `Migrations/` folder
   - Verify Up() and Down() methods

4. **Apply migration**
   ```bash
   # Via MigrationService (recommended)
   dotnet run --project ../Synaptix.MigrationService/Synaptix.MigrationService.csproj
   
   # OR directly via EF tools
   dotnet ef database update --startup-project ../Synaptix.Backend.Api
   ```

### Migration Modes

Configure via environment variable or appsettings:

**MigrateAndSeed (default)**
```bash
MIGRATION_MODE=MigrateAndSeed
```
- Applies EF Core migrations
- Seeds reference data
- Default mode for development

**RebuildElastic**
```bash
MIGRATION_MODE=RebuildElastic
REBUILD_ELASTIC=true
REBUILD_ELASTIC_FROM_DATE=2025-01-01
REBUILD_ELASTIC_TO_DATE=2025-12-31
```
- Rebuilds Elasticsearch indices
- Useful after index mapping changes

**MigrateSeedAndRebuildElastic**
```bash
MIGRATION_MODE=MigrateSeedAndRebuildElastic
```
- Runs migrations
- Seeds data
- Rebuilds Elasticsearch indices

---

## 11. Health Check Verification

### Automated Health Checks

```bash
make -f docker/MakeFile health
```

### Manual Health Verification

**PostgreSQL**
```bash
docker compose -f docker/compose.yml exec postgres \
  pg_isready -U tycoon_user -d tycoon_db
```

**MongoDB**
```bash
docker compose -f docker/compose.yml exec mongodb \
  mongosh -u tycoon_admin -p tycoon_mongo_password_123 \
  --eval "db.adminCommand('ping')"
```

Mongo setup creates databases, users, collections, and indexes only. Empty analytics collections are expected until gameplay or analytics smoke writes valid events. A local write-path smoke can be run with a valid `question_answered` payload:

```bash
curl -X POST http://localhost:5000/analytics/track \
  -H "Content-Type: application/json" \
  -d '{"eventName":"question_answered","timestamp":"2026-06-04T00:00:00Z","properties":{"id":"local-smoke-question-answered","playerId":"11111111-1111-1111-1111-111111111111","matchId":"22222222-2222-2222-2222-222222222222","questionId":"smoke-question","mode":"smoke","category":"general","difficulty":1,"isCorrect":true,"answerTimeMs":750,"pointsAwarded":10}}'

docker compose --env-file docker/.env -f docker/compose.yml exec mongodb \
  mongosh --quiet --eval "const u=process.env.MONGO_INITDB_ROOT_USERNAME; const p=process.env.MONGO_INITDB_ROOT_PASSWORD; const conn=new Mongo('127.0.0.1:27017'); const admin=conn.getDB('admin'); admin.auth(u,p); const db=conn.getDB('synaptix_analytics'); printjson({events:db.question_answered_events.countDocuments(), daily:db.qa_daily_rollups.countDocuments(), playerDaily:db.qa_player_daily_rollups.countDocuments()});"
```

**Redis**
```bash
docker compose -f docker/compose.yml exec redis \
  redis-cli -a tycoon_redis_password_123 ping
```

**Elasticsearch**
```bash
curl -u elastic:tycoon_elastic_password_123 \
  http://localhost:9200/_cluster/health?pretty
```

**RabbitMQ**
```bash
docker compose -f docker/compose.yml exec rabbitmq \
  rabbitmq-diagnostics ping
```

**MinIO**
```bash
curl -f http://localhost:9000/minio/health/live
```

**Backend API** (when running)
```bash
curl http://localhost:5000/healthz
```

### Expected Health Check Outputs

- PostgreSQL: "accepting connections"
- MongoDB: "ok: 1"
- MongoDB analytics collections: empty before smoke/gameplay; counts increase after a valid `question_answered` event
- Redis: "PONG"
- Elasticsearch: status "yellow" or "green"
- RabbitMQ: "pong"
- MinIO: HTTP 200 OK
- API: HTTP 200 OK

---

## 12. Log Viewing Commands

### View All Logs

```bash
make -f docker/MakeFile logs
# OR:
docker compose -f docker/compose.yml logs -f
```

### View Specific Service Logs

```bash
# API logs
make -f docker/MakeFile api-logs
# OR:
docker compose -f docker/compose.yml logs -f backend-api

# Migration logs
make -f docker/MakeFile migration-logs
# OR:
docker compose -f docker/compose.yml logs migration

# Infrastructure services
docker compose -f docker/compose.yml logs -f postgres
docker compose -f docker/compose.yml logs -f mongodb
docker compose -f docker/compose.yml logs -f redis
docker compose -f docker/compose.yml logs -f elasticsearch
```

### Filter and Search Logs

```bash
# Last 100 lines
docker compose -f docker/compose.yml logs --tail=100 backend-api

# Since specific time
docker compose -f docker/compose.yml logs --since 2025-01-01T12:00:00

# Filter with grep
docker compose -f docker/compose.yml logs backend-api | grep ERROR

# Multiple services
docker compose -f docker/compose.yml logs -f postgres mongodb redis
```

### Export Logs

```bash
# Export to file
docker compose -f docker/compose.yml logs backend-api > api-logs.txt

# Export all logs
docker compose -f docker/compose.yml logs > all-logs.txt
```

---

## 13. Container Cleanup Procedures

### Stop Services (Keep Data)

```bash
make -f docker/MakeFile down
# OR:
docker compose -f docker/compose.yml down
```

### Remove Volumes (⚠️ DELETES DATA)

```bash
make -f docker/MakeFile clean
# OR:
docker compose -f docker/compose.yml down -v --remove-orphans
```

### Remove Everything and Rebuild

```bash
# Complete cleanup
docker compose -f docker/compose.yml down -v --remove-orphans
docker system prune -a --volumes

# Rebuild from scratch
make -f docker/MakeFile up
make -f docker/MakeFile migrate
```

### Clean Up Unused Resources

```bash
# Remove unused images, containers, networks
docker system prune -a

# Remove unused volumes
docker volume prune

# Remove dangling images
docker image prune
```

### Selective Cleanup

```bash
# Remove specific service
docker compose -f docker/compose.yml rm -s -v postgres

# Remove specific volume
docker volume rm tycoon_postgres_data

# Stop and remove specific container
docker stop tycoon_postgres
docker rm tycoon_postgres
```

---

## 10. Design Principles (Intentional)

* Infrastructure is **ephemeral**, data is persisted via volumes
* Migrations are **explicit**, never implicit
* Docker is **supporting**, not controlling, application lifecycle
* Local dev ≠ production, but parity is preserved

---

**This file is the single source of truth for Docker usage in this repository.**

---

## 10. On-Prem + Cloudflare Deployment

If you want to run this stack on your own Linux servers (Ubuntu/Fedora) and expose it through Cloudflare, use:

- `docker/compose.yml`
- `docker/compose.prod.yml`
- optionally `docker/compose.cloudflare-tunnel.yml` (Cloudflare Tunnel mode)

See the full runbook here:

- `docs/ON_PREM_CLOUDFLARE_DEPLOYMENT.md`

---

## 11. Synaptix Security Stack (`compose.security.yml`)

The security stack is a **separate, independently runnable** Docker Compose file that provides:

| Service | Container | Port | Purpose |
|---|---|---|---|
| HashiCorp Vault (dev) | `synaptix_vault` | `8210:8200` | Transit key wrapping |
| Vault Init (one-shot) | `synaptix_vault_init` | — | Provisions Transit keys at startup |
| KMS API | `synaptix_kms_api` | `5060:5050` | Session, payload, and key endpoints |

### Standalone (no main stack)

```bash
docker compose -f docker/compose.security.yml up --build
```

The KMS API falls back to in-memory cache when no Redis connection string is set. This is the default for local development.

### Alongside the main stack

```bash
# Start main infrastructure first
docker compose -f docker/compose.yml up -d

# Then bring up the security stack
docker compose -f docker/compose.security.yml up -d
```

To connect KMS to the main Redis instance, set `KMS_REDIS_URL` in your environment before running:

```bash
export KMS_REDIS_URL="redis:6379,password=<redis-password>"
docker compose -f docker/compose.security.yml up -d
```

### Environment variables

| Variable | Default | Description |
|---|---|---|
| `VAULT_PORT` | `8210` | Host-side Vault port |
| `KMS_API_PORT` | `5060` | Host-side KMS API port |
| `VAULT_DEV_TOKEN` | `dev-root-token-change-me` | Vault root token (dev only) |
| `KMS_SERVICE_TOKEN` | `kms-internal-service-token-change-me` | Service-to-service header value |
| `JWT_SECRET_KEY` | `your-super-secret-jwt-key-change-me-in-production-minimum-32-characters-long` | Must match backend JWT secret |
| `VAULT_REQUIRED` | `false` | If true, KMS API refuses to start when Vault is unreachable |
| `KMS_REDIS_URL` | _(empty)_ | Redis connection string; empty = in-memory fallback |

### Transit keys provisioned by Vault Init

| Key name | Algorithm |
|---|---|
| `synaptix-session-wrap` | aes256-gcm96 |
| `synaptix-payload-wrap` | aes256-gcm96 |
| `synaptix-refresh-token-wrap` | aes256-gcm96 |
| `synaptix-data-protection-wrap` | aes256-gcm96 |

### Health check

```bash
curl http://localhost:5060/health
# → {"status":"Healthy"}
```

### Full run instructions

See [`docs/security/SYNAPTIX_SECURITY_RUNNING_GUIDE.md`](docs/security/SYNAPTIX_SECURITY_RUNNING_GUIDE.md) for a complete step-by-step guide including curl examples for every endpoint.

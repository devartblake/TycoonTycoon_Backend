# Docker Setup – TycoonTycoon Backend

This document describes how to run **TycoonTycoon_Backend** locally using Docker for infrastructure services (PostgreSQL, MongoDB, Redis, Elasticsearch), with two supported workflows:

* **Option A (recommended for development):** Dockerized infrastructure + run .NET services on the host
* **Option B:** Fully Dockerized backend API + infrastructure

The setup is designed to support fast iteration, reliable local state, and clean separation between **infrastructure** and **application lifecycle**.

---

## 1. Repository Layout (Docker-related)

All Docker assets live under the `docker/` directory.

```text
TycoonTycoon_Backend/
├─ Docker.md                # ← this document
├─ docker/
│  ├─ compose.yml           # main docker-compose stack
│  ├─ compose.dev.yml       # dev-only overrides
│  ├─ .env.example          # environment template
│  ├─ Makefile              # convenience commands
│  ├─ Dockerfile.api        # API container (Option B)
│  └─ init/
│     ├─ postgres/
│     │  └─ 01-init.sh
│     └─ mongo/
│        └─ 01-init.js
└─ Tycoon.Backend.Api/
└─ Tycoon.MigrationService/
```

This keeps Docker concerns isolated and avoids polluting the solution root.

---

## 2. Services Provided by Docker

The Docker stack provides **infrastructure only**:

| Service                       | Purpose                                         |
| ----------------------------- | ----------------------------------------------- |
| PostgreSQL                    | Primary relational database (EF Core, Hangfire) |
| MongoDB                       | Eventing, analytics, document data              |
| Redis                         | Caching, SignalR backplane                      |
| Elasticsearch                 | Search, analytics                               |
| Kibana *(dev profile)*        | Elasticsearch UI                                |
| pgAdmin *(dev profile)*       | PostgreSQL UI                                   |
| Mongo Express *(dev profile)* | MongoDB UI                                      |

> **Important:**
> Schema creation, migrations, and seeding are **NOT** done by the API.
> `Tycoon.MigrationService` is the **only owner** of migrations and data bootstrapping.

This is enforced explicitly in `Program.cs`:

> “Do NOT migrate here anymore. Tycoon.MigrationService owns migrations + seeding now.” 

---

## 3. Environment Configuration

### 3.1 Create your `.env`

```bash
cp docker/.env.example docker/.env
```

Edit values as needed (passwords, ports, memory limits).

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
dotnet run --project Tycoon.MigrationService/Tycoon.MigrationService.csproj
```

This will:

* Apply EF Core migrations
* Create schemas/tables
* Seed reference data

---

### 5.3 Run the API on the host

```bash
dotnet run --project Tycoon.Backend.Api/Tycoon.Backend.Api.csproj
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
dotnet run --project Tycoon.MigrationService/Tycoon.MigrationService.csproj
```

> Intentional design choice: migrations are explicit and controlled.

---

## 7. Dev Tooling URLs (Option A or B)

| Tool          | URL                                                              |
| ------------- | ---------------------------------------------------------------- |
| Swagger       | [http://localhost:5000/swagger](http://localhost:5000/swagger)   |
| pgAdmin       | [http://localhost:5050](http://localhost:5050)                   |
| Mongo Express | [http://localhost:8081](http://localhost:8081)                   |
| Kibana        | [http://localhost:5601](http://localhost:5601)                   |
| Hangfire      | [http://localhost:5000/hangfire](http://localhost:5000/hangfire) |

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
   dotnet run --project Tycoon.MigrationService/Tycoon.MigrationService.csproj
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
   dotnet run --project Tycoon.MigrationService/Tycoon.MigrationService.csproj
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
     --startup-project ../Tycoon.Backend.Api
   ```

3. **Review generated migration**
   - Check `Migrations/` folder
   - Verify Up() and Down() methods

4. **Apply migration**
   ```bash
   # Via MigrationService (recommended)
   dotnet run --project ../Tycoon.MigrationService/Tycoon.MigrationService.csproj
   
   # OR directly via EF tools
   dotnet ef database update --startup-project ../Tycoon.Backend.Api
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

**Backend API** (when running)
```bash
curl http://localhost:5000/healthz
```

### Expected Health Check Outputs

- PostgreSQL: "accepting connections"
- MongoDB: "ok: 1"
- Redis: "PONG"
- Elasticsearch: status "yellow" or "green"
- RabbitMQ: "pong"
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

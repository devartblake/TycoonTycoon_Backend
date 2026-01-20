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

### API starts but cannot connect to DB

* Ensure Docker services are healthy
* Verify you ran `Tycoon.MigrationService`
* Confirm you are using **localhost**, not service names, in Option A

### Hangfire throws missing connection error

* Ensure `ConnectionStrings:tycoon-db` or `db` is set
* Hangfire is enabled only when configured (see `Program.cs`) 

---

## 10. Design Principles (Intentional)

* Infrastructure is **ephemeral**, data is persisted via volumes
* Migrations are **explicit**, never implicit
* Docker is **supporting**, not controlling, application lifecycle
* Local dev ≠ production, but parity is preserved

---

**This file is the single source of truth for Docker usage in this repository.**

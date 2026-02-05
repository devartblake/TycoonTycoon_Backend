# TycoonTycoon Backend

A modern, cloud-native backend API built with .NET 9, designed for scalable multiplayer tycoon game infrastructure with real-time analytics, robust data persistence, and comprehensive observability.

---

## 📋 Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Prerequisites](#prerequisites)
- [Quick Start](#quick-start)
- [Development Workflows](#development-workflows)
  - [Local Development](#local-development-option-a-recommended)
  - [Docker Development](#docker-development-option-b)
- [Configuration](#configuration)
- [Database Migrations](#database-migrations)
- [Available Services](#available-services)
- [Testing](#testing)
- [Troubleshooting](#troubleshooting)
- [Documentation](#documentation)
- [Contributing](#contributing)

---

## 🎯 Overview

TycoonTycoon Backend provides a complete game backend infrastructure featuring:

- **🎮 Game State Management**: PostgreSQL-based persistent game state with EF Core
- **📊 Real-time Analytics**: MongoDB for event tracking and player analytics
- **⚡ High-Performance Caching**: Redis for session management and SignalR backplane
- **🔍 Full-Text Search**: Elasticsearch for advanced search and log aggregation
- **🔄 Background Jobs**: Hangfire for scheduled tasks and job processing
- **📡 Message Queue**: RabbitMQ for event-driven architecture
- **🔐 JWT Authentication**: Secure API access with role-based authorization
- **📈 Observability**: Built-in OpenTelemetry support and comprehensive logging

---

## 🏗️ Architecture

The solution follows Clean Architecture principles with the following structure:

```
TycoonTycoon_Backend/
├── Tycoon.Backend.Api/              # Web API project (Controllers, SignalR Hubs)
├── Tycoon.Backend.Application/      # Application logic (Services, Use Cases)
├── Tycoon.Backend.Domain/           # Domain entities and business logic
├── Tycoon.Backend.Infrastructure/   # Data access, external services
├── Tycoon.Backend.Migrations/       # EF Core migrations
├── Tycoon.MigrationService/         # Database migration runner service
├── Tycoon.Shared/                   # Shared utilities and helpers
├── Tycoon.ServiceDefaults/          # Common service configurations
├── docker/                          # Docker infrastructure configuration
└── scripts/                         # Development automation scripts
```

### Technology Stack

- **Runtime**: .NET 9
- **Web Framework**: ASP.NET Core 9.0
- **ORM**: Entity Framework Core 9.0
- **Databases**: PostgreSQL 16, MongoDB 7.0
- **Cache**: Redis 7
- **Search**: Elasticsearch 8.11
- **Message Queue**: RabbitMQ 3.13
- **Background Jobs**: Hangfire
- **Real-time Communication**: SignalR

---

## ✅ Prerequisites

Before you begin, ensure you have the following installed:

### Required Tools

1. **[.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)** (9.0 or later)
   ```bash
   dotnet --version  # Should show 9.0.x
   ```

2. **[Docker Desktop](https://www.docker.com/get-started)** (includes Docker Compose V2)
   ```bash
   docker --version
   docker compose version
   ```

### Optional but Recommended

- **[Make](https://www.gnu.org/software/make/)** - For convenient command shortcuts
  - macOS: `brew install make`
  - Linux: Usually pre-installed, or `apt-get install build-essential`
  - Windows: Included with Git Bash or install via Chocolatey

- **[Git](https://git-scm.com/)** - For version control

---

## 🚀 Quick Start

### Automated Setup (Recommended)

We provide setup scripts for all platforms that automate the environment configuration:

**Linux/macOS:**
```bash
chmod +x scripts/setup-dev.sh
./scripts/setup-dev.sh
```

**Windows (PowerShell):**
```powershell
.\scripts\setup-dev.ps1
```

These scripts will:
- ✅ Check for required tools (.NET SDK, Docker)
- ✅ Create/validate `.env` configuration file
- ✅ Validate appsettings.json files
- ✅ Optionally start Docker infrastructure
- ✅ Provide next steps for running migrations and the API

### Manual Quick Start

If you prefer to set up manually:

1. **Clone the repository**
   ```bash
   git clone https://github.com/devartblake/TycoonTycoon_Backend.git
   cd TycoonTycoon_Backend
   ```

2. **Configure environment**
   ```bash
   # Ensure docker/.env exists (should be committed with defaults)
   # Review and customize if needed
   ```

3. **Start infrastructure**
   ```bash
   make -f docker/MakeFile up
   # OR without make:
   docker compose -f docker/compose.yml up -d
   ```

4. **Run migrations**
   ```bash
   dotnet run --project Tycoon.MigrationService/Tycoon.MigrationService.csproj
   ```

5. **Start the API**
   ```bash
   dotnet run --project Tycoon.Backend.Api/Tycoon.Backend.Api.csproj
   ```

6. **Access the application**
   - API: http://localhost:5000
   - Swagger UI: http://localhost:5000/swagger
   - Hangfire Dashboard: http://localhost:5000/hangfire

---

## 💻 Development Workflows

### Local Development (Option A - Recommended)

This is the **primary development workflow**. It runs infrastructure in Docker while running .NET services on your host machine for fast iteration.

#### 1. Start Infrastructure

```bash
make -f docker/MakeFile up
```

This starts:
- PostgreSQL (port 5432)
- MongoDB (port 27017)
- Redis (port 6379)
- Elasticsearch (port 9200)
- RabbitMQ (ports 5672, 15672)

#### 2. Verify Health

```bash
make -f docker/MakeFile health
```

Wait until all services report as healthy (typically 10-30 seconds).

#### 3. Run Database Migrations

```bash
dotnet run --project Tycoon.MigrationService/Tycoon.MigrationService.csproj
```

This will:
- Apply all EF Core migrations
- Create database schemas and tables
- Seed reference data
- Configure Elasticsearch indices (if enabled)

#### 4. Start the API

```bash
dotnet run --project Tycoon.Backend.Api/Tycoon.Backend.Api.csproj
```

The API will:
- Start on http://localhost:5000
- Connect to all Docker infrastructure services via localhost
- Enable hot reload for rapid development

#### 5. Development Tools

With the `dev` profile, you get additional admin UIs:

```bash
make -f docker/MakeFile up-dev
```

Access:
- **pgAdmin**: http://localhost:5050 (PostgreSQL UI)
- **Mongo Express**: http://localhost:8081 (MongoDB UI)
- **Kibana**: http://localhost:5601 (Elasticsearch UI)
- **RabbitMQ Management**: http://localhost:15672
- **Prometheus**: http://localhost:9090
- **Grafana**: http://localhost:3000

#### Connection Strings for Local Development

The API automatically uses these connection strings from `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "db": "Host=localhost;Port=5432;Database=tycoon_db;Username=tycoon_user;Password=tycoon_password_123",
    "redis": "localhost:6379,password=tycoon_redis_password_123,defaultDatabase=0",
    "mongo": "mongodb://tycoon_admin:tycoon_mongo_password_123@localhost:27017/tycoon_db?authSource=admin",
    "elasticsearch": "http://localhost:9200"
  }
}
```

### Docker Development (Option B)

Run the entire stack (infrastructure + API) in Docker containers.

#### 1. Start Everything

```bash
docker compose -f docker/compose.yml up -d --build
```

This builds and starts:
- All infrastructure services
- Migration service (runs once and exits)
- Backend API (containerized)

#### 2. Access the Application

- API: http://localhost:5000
- All other services on their respective ports

#### 3. View Logs

```bash
# All services
make -f docker/MakeFile logs

# Specific service
make -f docker/MakeFile api-logs
make -f docker/MakeFile migration-logs
```

#### 4. Stop Everything

```bash
make -f docker/MakeFile down
```

---

## ⚙️ Configuration

### Environment Variables

Configuration is managed through `docker/.env` which contains defaults for local development. All values can be overridden:

**Key Configuration Values:**

| Service | Variable | Default | Description |
|---------|----------|---------|-------------|
| PostgreSQL | `POSTGRES_DB` | `tycoon_db` | Database name |
| | `POSTGRES_USER` | `tycoon_user` | Database user |
| | `POSTGRES_PASSWORD` | `tycoon_password_123` | Database password |
| MongoDB | `MONGO_INITDB_ROOT_USERNAME` | `tycoon_admin` | Admin username |
| | `MONGO_INITDB_ROOT_PASSWORD` | `tycoon_mongo_password_123` | Admin password |
| Redis | `REDIS_PASSWORD` | `tycoon_redis_password_123` | Redis password |
| Elasticsearch | `ELASTIC_PASSWORD` | `tycoon_elastic_password_123` | Elasticsearch password |
| RabbitMQ | `RABBITMQ_USER` | `tycoon_user` | RabbitMQ username |
| | `RABBITMQ_PASSWORD` | `tycoon_rabbitmq_password_123` | RabbitMQ password |

**⚠️ Security Note**: Change all passwords before deploying to production!

### Application Settings

Application configuration is in `appsettings.json` files:

- `appsettings.json` - Base configuration (matches Docker defaults)
- `appsettings.Development.json` - Development overrides
- `appsettings.Production.json` - Production configuration

---

## 🗄️ Database Migrations

### Overview

- **Tycoon.MigrationService** is the **only owner** of database migrations and seeding
- The API **does not** run migrations automatically (by design)
- Migrations are explicit and controlled

### Running Migrations

**Standalone (recommended):**
```bash
dotnet run --project Tycoon.MigrationService/Tycoon.MigrationService.csproj
```

**Via Docker:**
```bash
make -f docker/MakeFile migrate
```

### Creating New Migrations

```bash
cd Tycoon.Backend.Infrastructure
dotnet ef migrations add YourMigrationName --startup-project ../Tycoon.Backend.Api
```

### Migration Modes

Configure via `MigrationService__Mode` in appsettings or environment variable:

- `MigrateAndSeed` (default) - Run migrations and seed data
- `RebuildElastic` - Rebuild Elasticsearch indices only
- `MigrateSeedAndRebuildElastic` - Do everything

---

## 🛠️ Available Services

### Infrastructure Services (Always Running)

| Service | Port | Purpose |
|---------|------|---------|
| PostgreSQL | 5432 | Primary database |
| MongoDB | 27017 | Analytics & events |
| Redis | 6379 | Caching & SignalR |
| Elasticsearch | 9200 | Search & logs |
| RabbitMQ | 5672 | Message queue |
| RabbitMQ Management | 15672 | Admin UI |

### Application Services

| Service | Port | Purpose |
|---------|------|---------|
| Backend API | 5000 | Main REST API |
| Swagger UI | 5000/swagger | API documentation |
| Hangfire Dashboard | 5000/hangfire | Job monitoring |

### Development Tools (dev profile only)

| Service | Port | Credentials |
|---------|------|-------------|
| pgAdmin | 5050 | admin@tycoon.local / admin_password_123 |
| Mongo Express | 8081 | admin / admin_password_123 |
| Kibana | 5601 | elastic / tycoon_elastic_password_123 |
| Prometheus | 9090 | (no auth) |
| Grafana | 3000 | admin / admin_password_123 |

---

## 🧪 Testing

### Running Tests

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test Tycoon.Backend.Api.Tests/

# Run with coverage
dotnet test /p:CollectCoverage=true /p:CoverageReportsDirectory=../coverage
```

### Test Projects

- `Tycoon.Backend.Api.Tests` - API integration tests
- `Tycoon.Backend.Application.Tests` - Application logic tests
- `Tycoon.Backend.Infrastructure.Tests` - Data access tests

---

## 🔧 Troubleshooting

### Services Won't Start

**Problem**: Docker containers fail to start or crash immediately.

**Solutions**:
1. Check Docker is running: `docker info`
2. Check for port conflicts: `docker compose ps`
3. View logs: `docker compose -f docker/compose.yml logs [service-name]`
4. Try clean restart:
   ```bash
   make -f docker/MakeFile down
   make -f docker/MakeFile up
   ```

### API Can't Connect to Database

**Problem**: API throws database connection errors.

**Solutions**:
1. Verify infrastructure is healthy: `make -f docker/MakeFile health`
2. Check connection strings in `appsettings.Development.json` match `docker/.env`
3. Ensure migrations have run: `dotnet run --project Tycoon.MigrationService/...`
4. Verify PostgreSQL is accessible:
   ```bash
   docker compose -f docker/compose.yml exec postgres psql -U tycoon_user -d tycoon_db
   ```

### Migrations Fail

**Problem**: Migration service throws errors or doesn't complete.

**Solutions**:
1. Ensure PostgreSQL is healthy and accessible
2. Check database credentials match `.env` file
3. Drop and recreate database (⚠️ data loss):
   ```bash
   make -f docker/MakeFile clean  # Removes all data
   make -f docker/MakeFile up
   make -f docker/MakeFile migrate
   ```

### Elasticsearch Not Responding

**Problem**: Elasticsearch health check fails or shows yellow/red status.

**Solutions**:
1. Increase memory (if needed): Edit `ES_JAVA_OPTS` in `.env`
2. Wait longer - ES can take 30-60 seconds to fully start
3. Check ES logs: `docker compose -f docker/compose.yml logs elasticsearch`
4. Verify credentials:
   ```bash
   curl -u elastic:tycoon_elastic_password_123 http://localhost:9200/_cluster/health?pretty
   ```

### Redis Connection Timeout

**Problem**: API logs show Redis connection errors.

**Solutions**:
1. Verify Redis is running: `docker compose ps redis`
2. Test connection: `docker compose exec redis redis-cli -a tycoon_redis_password_123 ping`
3. Check password matches in both `appsettings.json` and `docker/.env`

### Port Already in Use

**Problem**: Docker fails to start because port is already allocated.

**Solutions**:
1. Identify the conflicting process:
   ```bash
   # Linux/Mac
   lsof -i :[port]
   # Windows
   netstat -ano | findstr :[port]
   ```
2. Stop the conflicting service or change port in `docker/.env`
3. Restart Docker stack

### Performance Issues

**Problem**: Slow response times or high resource usage.

**Solutions**:
1. Allocate more resources to Docker Desktop (Settings → Resources)
2. Recommended minimums:
   - CPU: 4 cores
   - Memory: 8 GB
   - Disk: 20 GB
3. Disable dev profile services if not needed:
   ```bash
   make -f docker/MakeFile up  # without -dev
   ```

### Clean Slate Reset

If all else fails, perform a complete reset:

```bash
# Stop and remove everything (⚠️ DELETES ALL DATA)
make -f docker/MakeFile clean

# Start fresh
make -f docker/MakeFile up
make -f docker/MakeFile migrate
dotnet run --project Tycoon.Backend.Api/Tycoon.Backend.Api.csproj
```

---

## 📚 Documentation

- **[Docker.md](Docker.md)** - Detailed Docker setup and infrastructure guide
- **[API Documentation](http://localhost:5000/swagger)** - Interactive API documentation (when API is running)
- **Hangfire Dashboard** - Background job monitoring at http://localhost:5000/hangfire

---

## 🤝 Contributing

### Getting Started

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/my-feature`
3. Make your changes following the coding standards
4. Run tests: `dotnet test`
5. Commit your changes: `git commit -m "Add my feature"`
6. Push to your fork: `git push origin feature/my-feature`
7. Open a Pull Request

### Coding Standards

- Follow C# coding conventions
- Write unit tests for new features
- Update documentation for API changes
- Use meaningful commit messages

### Pull Request Process

1. Ensure all tests pass
2. Update relevant documentation
3. Add a clear description of changes
4. Request review from maintainers

---

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

---

## 🌟 Support

For issues, questions, or contributions:
- **GitHub Issues**: https://github.com/devartblake/TycoonTycoon_Backend/issues
- **Discussions**: https://github.com/devartblake/TycoonTycoon_Backend/discussions

---

## 🙏 Acknowledgments

Built with:
- [.NET](https://dotnet.microsoft.com/) - Cross-platform framework
- [PostgreSQL](https://www.postgresql.org/) - Robust relational database
- [MongoDB](https://www.mongodb.com/) - Flexible document database
- [Redis](https://redis.io/) - Lightning-fast cache
- [Elasticsearch](https://www.elastic.co/) - Powerful search engine
- [RabbitMQ](https://www.rabbitmq.com/) - Reliable message broker
- [Hangfire](https://www.hangfire.io/) - Background job processor

---

**Happy coding! 🚀**

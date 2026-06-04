# Synaptix.Setup UI + CLI Architecture Handoff
### Full Platform Bootstrap, Provisioning, Administration & Operations Design

**Project:** TycoonTycoon_Backend / Synaptix Platform  
**Version:** 1.0  
**Target:** Alpha/Beta Release + Long-Term Platform Architecture

---

# 1. Executive Summary

The Synaptix platform is growing beyond a simple backend API and is evolving into a multi-service ecosystem consisting of:

- Tycoon Backend API
- Operator Dashboard
- Migration Service
- Security/KMS Services
- Redis
- MongoDB
- PostgreSQL
- RabbitMQ
- MinIO
- Elasticsearch
- Analytics Services
- Future Personalization Services
- Future AI Services

The recommended solution is to introduce:

**Synaptix.Setup**

This becomes the authoritative setup, bootstrap, provisioning, validation, and operational orchestration engine.

The Operator Dashboard becomes the visual management layer.

The Backend API becomes the secure orchestration layer.

---

# 2. Design Goals

## Objective 1
Simple local developer onboarding.

```bash
dotnet run --project Synaptix.Setup -- init-local
```

## Objective 2
Automated environment provisioning.

## Objective 3
Secure secret handling.

## Objective 4
Administrative visibility.

## Objective 5
Long-term platform scalability.

---

# 3. High-Level Architecture

```text
OperatorDashboard
        ↓
Backend.Api
        ↓
Synaptix.Setup
        ↓
Postgres / Mongo / Redis / RabbitMQ / MinIO / Elastic / KMS
```

---

# 4. Synaptix.Setup Responsibilities

- Configuration generation and validation
- Secret generation and rotation
- KMS integration
- PostgreSQL setup
- MongoDB setup
- Redis setup
- RabbitMQ setup
- MinIO setup
- Elasticsearch setup
- Seed management
- Super administrator creation
- Setup status reporting

---

# 5. CLI-Only Operations

The following operations should remain CLI-only:

- Database reset
- Environment destruction
- JWT secret rotation
- KMS master key rotation
- Disaster recovery
- Infrastructure recreation

---

# 6. Operator Dashboard Features

## Setup Overview
Route:

```text
/settings/setup
```

Displays:

- Environment readiness
- Service health
- Seed health
- Migration status
- KMS status

## Validation Dashboard

```text
/settings/setup/validation
```

Displays:

- Missing configuration
- Invalid secrets
- Seed failures
- Migration failures

## Service Health Dashboard

```text
/settings/setup/services
```

Displays:

- PostgreSQL
- MongoDB
- Redis
- RabbitMQ
- MinIO
- Elasticsearch
- KMS

## Seed Management

```text
/settings/setup/seeds
```

Features:

- Upload
- Validate
- Synchronize
- Version tracking

## Super Administrator Management

```text
/settings/setup/admin
```

Features:

- Create Super Admin
- Reset Password
- Permission Review
- MFA Status

## Setup Logs

```text
/settings/setup/logs
```

Features:

- Search
- Filter
- Export
- Audit Review

---

# 7. Backend API Layer

Suggested route group:

```text
/api/admin/setup
```

Endpoints:

```http
GET  /api/admin/setup/status
GET  /api/admin/setup/readiness
GET  /api/admin/setup/services
GET  /api/admin/setup/seeds
GET  /api/admin/setup/admin

POST /api/admin/setup/validate
POST /api/admin/setup/seeds/sync
POST /api/admin/setup/admin/create
POST /api/admin/setup/admin/reset-password
```

---

# 8. Integration with Synaptix.Security.Kms

## KMS Owns

- Encryption
- Decryption
- Key wrapping
- Vault Transit
- Key rotation

## Setup Owns

- Bootstrap secrets
- Environment generation
- Service provisioning
- Admin provisioning

## Bridge Layer

```text
Synaptix.Setup.Security
```

Components:

- ISetupSecretProtector
- SetupSecretManifest
- SetupSecretValidator
- KmsSetupSecretProtector

---

# 9. Secret Management Strategy

Supported modes:

- PlaintextLocal
- KmsPreferred
- KmsRequired
- ExternalOnly

Recommended:

| Environment | Mode |
|------------|------|
| Local | KmsPreferred |
| Staging | KmsRequired |
| Production | ExternalOnly / KmsRequired |

---

# 10. Bootstrap Workflow

## Local

```bash
dotnet run --project Synaptix.Setup -- init-local
docker compose up -d
dotnet run --project Synaptix.MigrationService
```

## Staging

```bash
dotnet Synaptix.Setup.dll validate
dotnet Synaptix.Setup.dll provision-services
dotnet Synaptix.MigrationService.dll
```

## Production

```bash
dotnet Synaptix.Setup.dll validate --strict
dotnet Synaptix.Setup.dll provision-services --strict
dotnet Synaptix.MigrationService.dll
```

---

# 11. Seed Architecture

Create:

```text
config/seeds/seed-manifest.json
```

Defines:

- Required seeds
- Optional seeds
- Versioning
- Validation rules

---

# 12. Service Provisioners

Recommended tasks:

- PostgresSetupTask
- MongoSetupTask
- RedisSetupTask
- RabbitMqSetupTask
- MinioSetupTask
- ElasticSetupTask
- SuperAdminSetupTask

Requirements:

- Idempotent
- Testable
- Environment-aware

---

# 13. Operator Permissions

Standard:

- Setup.View
- Setup.Validate
- Setup.SeedManage
- Setup.AdminManage
- Setup.LogsView

Restricted:

- Setup.SecretRotate
- Setup.DatabaseReset
- Setup.EnvironmentDestroy
- Setup.KmsAdministration

Require Super Administrator approval.

---

# 14. Alpha/Beta Implementation Plan

## Phase 1 (P0)

- Synaptix.Setup CLI
- Setup status endpoint
- Readiness endpoint
- Dashboard overview

## Phase 2 (P1)

- Validation dashboard
- Service dashboard
- Seed dashboard

## Phase 3 (P1)

- Admin management
- Audit logs
- Bootstrap reports

## Phase 4 (P2)

- Secret rotation
- KMS dashboards
- Advanced provisioning

---

# 15. Final Recommendation

```text
Synaptix.Setup
    = Platform Bootstrap Engine

Synaptix.Backend.Api
    = Secure Orchestration Layer

Synaptix.OperatorDashboard
    = Administrative Control Center

Synaptix.Security.Kms
    = Secret Protection Platform
```

Benefits:

- Developer onboarding
- Automated provisioning
- Safe administration
- Secret management
- Operational visibility
- Alpha/Beta readiness
- Enterprise scalability
- Multi-game support
- Future cloud readiness

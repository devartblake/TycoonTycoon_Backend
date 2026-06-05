# Synaptix.Setup UI + CLI Architecture Handoff

**Status:** Canonical setup-management architecture
**Updated:** 2026-06-04
**Baseline:** `main` at `688e35b0`
**Target:** Alpha/Beta-safe operations and long-term platform administration

## 1. Decision

The architecture is viable with a strict responsibility boundary:

```text
Synaptix.Setup
  = offline CLI and one-shot bootstrap/provisioning engine

Synaptix.Backend.Api
  = authenticated, sanitized setup status/readiness API

Synaptix.OperatorDashboard.Django
  = canonical setup-management UI and BFF

Synaptix.Security.Kms
  = secret-protection platform; setup integration remains deferred
```

The Backend API must not shell out to or directly execute `Synaptix.Setup`. The Django dashboard must not connect directly to infrastructure services or read setup secrets. Initial UI/API delivery is read-only.

## 2. Current Implemented State

### Synaptix.Setup CLI

The standalone `.NET 10` CLI is implemented and registered in the solution. It currently supports:

```text
init-local
validate
provision-services
provision-minio
upload-seeds
validate-seeds
create-super-admin
rotate-super-admin-password
status
```

Implemented provisioning tasks:

- PostgreSQL connection and migration-history validation.
- MongoDB database, collection, and index provisioning.
- MongoDB app-user create/update in `MONGO_AUTH_DB`.
- MongoDB app-user authentication validation.
- Legacy same-named app-user detection in `admin`, with opt-in cleanup through `SETUP_MONGO_REMOVE_LEGACY_ADMIN_APP_USER=true`.
- Redis password and logical-database read/write validation.
- RabbitMQ vhost and permission provisioning.
- MinIO bucket provisioning and bundled seed upload.
- Optional Elasticsearch validation.

The Compose `setup` service runs as a one-shot container before `migration`. MongoDB and Redis use container-internal ports (`27017` and `6379`).

### Setup Security

The following Phase 1 abstractions exist under `Synaptix.Setup.Security`:

- `ISetupSecretProtector`
- `ProtectedSetupSecret`
- `SetupSecretManifest`
- `SetupSecretOptions`
- `SetupSecretValidator`
- `PlaintextLocalSetupSecretProtector`

`PlaintextLocal` is the active implementation. `KmsSetupSecretProtector` is not implemented.

### Tests And Verification

`Synaptix.Setup.Tests` covers MongoDB admin connection construction, auth-database resolution, password escaping, and Redis structured/raw configuration parsing.

Phase 1 read-only setup visibility is implemented:

- Backend API endpoints under `/admin/setup/*` aggregate sanitized live diagnostics.
- Backend API writes sanitized diagnostic snapshots to the durable `setup_reports` table when the report store is available.
- `setup:read` is assigned to admin and super-admin permission profiles and enforced by the Backend API and Django.
- Django BFF endpoints under `/api/operator/setup/*` proxy only the Backend API.
- Django pages under `/settings/setup/*` display read-only setup diagnostics.
- Backend contract tests and Django client/view permission tests cover the surface.

Local Docker verification completed:

- repeated `provision-services` runs: `7` tasks succeeded, `0` errors;
- MongoDB app-user authentication through `synaptix_analytics`;
- MongoDB analytics/crypto collections and indexes exist; analytics documents are not seeded by setup;
- legacy `admin` user warning behavior;
- opt-in legacy-user removal;
- Redis validation across five logical databases.

This local evidence does not replace staging readiness or release-gate evidence.

### Remaining Not Implemented

The following surfaces do not currently exist:

- setup-write permissions;
- KMS-backed setup-secret protection;
- setup mutation through the dashboard or Backend API.

## 3. Runtime And Trust Boundaries

### Current Bootstrap Flow

```text
Synaptix.Setup init-local
        |
        v
docker/.env + local bootstrap files
        |
        v
Docker infrastructure
        |
        v
one-shot setup container: provision-services
        |
        v
one-shot migration container: migrations + application seeds + readiness
        |
        v
Backend API
        |
        v
Synaptix.OperatorDashboard.Django
```

### Implemented Read-Only Management Flow

```text
Operator
  |
  v
Synaptix.OperatorDashboard.Django
  |
  v
Django BFF: /api/operator/setup/*
  |
  v
Backend API: /admin/setup/*
  |
  v
sanitized live diagnostics and readiness sources
```

The Backend API endpoints aggregate safe live diagnostics. They do not invoke the CLI, expose environment values, return connection strings, or disclose secret-validation details that reveal secret material.

## 4. Bootstrap Status Limitation

`Synaptix.Setup` currently writes:

```text
.local/bootstrap/bootstrap-status.json
```

When `provision-services` runs in the one-shot setup container, that file is container-local and is not a durable Backend API data source. Phase 1 setup endpoints therefore aggregate live diagnostics already available to the Backend API, such as health checks, MongoDB status, storage diagnostics, migration/readiness signals, and sanitized configuration presence.

The implemented durable report history is Backend-generated, not CLI-file-backed: each live diagnostic snapshot can be stored as sanitized JSON in PostgreSQL `setup_reports`. Documentation and UI must not claim that the Backend API reads the CLI status file.

## 5. Phase 1: Read-Only Setup Visibility

Phase 1 is intentionally read-only and should not block the Alpha/Beta release.

### Implemented Permission

Add:

```text
setup:read
```

This matches the existing lowercase colon-delimited permission convention and is assigned to admin and super-admin profiles.

No `setup:write` permission is introduced in Phase 1.

### Implemented Backend API

Mount beneath the existing admin route group:

```http
GET /admin/setup/status
GET /admin/setup/readiness
GET /admin/setup/services
GET /admin/setup/seeds
GET /admin/setup/validation
GET /admin/setup/history
GET /admin/setup/history/latest
```

Responses must be sanitized and read-only:

- `status`: overall setup/runtime summary and report-source metadata;
- `readiness`: dependency and migration/readiness state;
- `services`: PostgreSQL, MongoDB, Redis, RabbitMQ, MinIO, Elasticsearch, and KMS availability;
- `seeds`: required seed presence/readiness, without returning seed contents;
- `validation`: missing/invalid configuration categories without values or secrets.
- `history`: latest sanitized durable setup-report summaries from `setup_reports`;
- `history/latest`: latest sanitized durable setup-report detail.

### Implemented Django BFF And UI

Django remains the canonical Operator Dashboard.

BFF routes:

```text
/api/operator/setup/status
/api/operator/setup/readiness
/api/operator/setup/services
/api/operator/setup/seeds
/api/operator/setup/validation
/api/operator/setup/history
```

UI routes:

```text
/settings/setup
/settings/setup/readiness
/settings/setup/services
/settings/setup/seeds
/settings/setup/validation
/settings/setup/history
```

The UI should display timestamps, source, state, warnings, and remediation guidance. It must not display credentials, connection strings, plaintext validation values, or secret-manifest contents.

## 6. CLI-Only Operations

These operations remain CLI-only until a separately approved, audited design exists:

- provisioning or reprovisioning infrastructure services;
- seed upload, synchronization, or mutation;
- super-admin creation or password rotation;
- secret generation or rotation;
- legacy MongoDB app-user removal;
- database reset or destructive migration actions;
- environment destruction;
- KMS master-key administration;
- disaster recovery;
- infrastructure recreation.

The initial Django UI may show remediation commands, but it must not execute them.

## 7. Secret Management Direction

Supported configuration modes are defined:

```text
PlaintextLocal
KmsPreferred
KmsRequired
ExternalOnly
```

Current implementation status:

| Mode | Status |
|---|---|
| `PlaintextLocal` | Implemented and active for local Alpha/Beta bootstrap |
| `KmsPreferred` | Validation behavior exists; KMS-backed protection is not implemented |
| `KmsRequired` | Validation behavior exists; KMS-backed protection is not implemented |
| `ExternalOnly` | Defined as policy direction; provider workflow is not implemented |

Future KMS integration must use `Synaptix.Security.Kms.Client`, keep setup ownership outside KMS, and never expose secret operations through the read-only Phase 1 UI.

## 8. Delivery Roadmap

### Completed

- Synaptix.Setup CLI and one-shot Compose setup service.
- Local secret generation and validation.
- Service provisioning and bundled seed upload.
- Durable MongoDB/Redis configuration hardening.
- Focused setup tests.
- Phase 1 sanitized setup status/readiness response contracts.
- Protected Backend API `/admin/setup/*` GET endpoints.
- Durable Backend-generated setup report/history store.
- Admin/super-admin `setup:read` permission.
- Django BFF client, `/api/operator/setup/*` routes, and `/settings/setup/*` pages.
- Backend secret-leak contract tests and Django permission/view tests.

### Deferred

- CLI-authored setup-run event/audit history.
- KMS-backed `KmsSetupSecretProtector`.
- Any API/UI mutation.
- Secret rotation and destructive operations.

## 9. Acceptance Criteria

The read-only setup UI architecture is implemented. Acceptance status:

- every endpoint has a defined sanitized response contract;
- the Backend API gathers status without invoking `Synaptix.Setup`;
- Django accesses setup data only through authenticated Backend API calls;
- `setup:read` is enforced in both Backend API and Django;
- no response or log exposes credentials, secret values, or connection strings;
- API contract tests and Django permission/view tests pass;
- durable setup history uses sanitized Backend-generated reports, not the CLI container-local status file;
- local Docker smoke against the running stack remains to be captured;
- documentation continues to distinguish local evidence from staging readiness.

## 10. Final Recommendation

Proceed with the hybrid design, beginning with read-only visibility:

```text
Synaptix.Setup = authoritative offline setup engine
Synaptix.Backend.Api = sanitized read-only setup API
Synaptix.OperatorDashboard.Django = canonical setup UI/BFF
Synaptix.Security.Kms = future setup-secret protection dependency
```

This preserves the current working bootstrap path, respects the Django dashboard decision, and avoids turning a privileged setup executable into a network-reachable control plane.

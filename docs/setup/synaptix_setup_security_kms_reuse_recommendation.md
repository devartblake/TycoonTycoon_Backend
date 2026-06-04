# Synaptix.Setup + Synaptix.Security/KMS Integration Recommendation

Repository: `devartblake/TycoonTycoon_Backend`  
Focus: determining whether the existing `Synaptix.Security` / `Synaptix.Security.Kms` projects can support the proposed `Synaptix.Setup` bootstrap system.

---

## Implementation Status (2026-06-04)

The recommended responsibility boundary remains valid, and Phase 1 setup-security abstractions are implemented under `Synaptix.Setup.Security`:

- `ISetupSecretProtector`
- `ProtectedSetupSecret`
- `SetupSecretManifest`
- `SetupSecretOptions`
- `SetupSecretValidator`
- `PlaintextLocalSetupSecretProtector`

`InitLocalCommand` writes the protected-secret envelope using `PlaintextLocalSetupSecretProtector`, and `ValidateCommand` applies the configured protection-mode validation policy.

Important limitation: setup secrets are not currently protected by KMS. `KmsSetupSecretProtector`, typed `Synaptix.Security.Kms.Client` integration, production external-secret providers, and general secret-rotation commands remain deferred. `KmsPreferred`, `KmsRequired`, and `ExternalOnly` describe policy direction and validation behavior; they do not mean KMS-backed setup-secret storage is complete.

For the setup UI/API direction, see [`Synaptix_Setup_UI_CLI_Architecture_Handoff.md`](Synaptix_Setup_UI_CLI_Architecture_Handoff.md). The initial proposed UI is read-only and must not expose setup secrets.

---

## 1. Executive Summary

Yes, the existing `Synaptix.Security.Kms` stack can and should be used by `Synaptix.Setup` in a future KMS-backed protection phase.

However, `Synaptix.Security.Kms` should **not** become responsible for Docker, PostgreSQL, MongoDB, Redis, RabbitMQ, MinIO, Elasticsearch, seed execution, or super admin provisioning. Those responsibilities belong in `Synaptix.Setup`.

The correct approach is:

```text
Synaptix.Security.Kms
  owns encryption, key wrapping, Vault Transit, key rotation, KMS API, and KMS client contracts

Synaptix.Setup
  owns bootstrap workflow, generated env files, local secrets, service provisioning, seed validation, and super admin setup

Synaptix.Setup.Security
  bridges setup secrets to Synaptix.Security.Kms
```

This keeps the security project generic and reusable while giving the setup system access to secure secret protection.

---

## 2. Current Security/KMS Assets in the Repository

The repository already includes a complete security/KMS folder structure inside the solution:

```text
/Services/Synaptix.Security.Kms/
  Synaptix.Security.Kms.Api
  Synaptix.Security.Kms.Application
  Synaptix.Security.Kms.Contracts
  Synaptix.Security.Kms.Infrastructure

Synaptix.Security.Kms.Client
Synaptix.Security.Kms.Tests
```

The existing security handoff documentation describes the KMS/security system as a reusable `.NET 10` subsystem for secure session negotiation, payload encryption/decryption, replay protection, Vault Transit integration, key rotation, and service-to-service reuse.

This is directly useful for `Synaptix.Setup`.

---

## 3. What Synaptix.Setup Can Reuse Immediately

### 3.1 KMS-backed secret wrapping

`Synaptix.Setup` can use the security/KMS stack to protect setup-generated secrets.

Secrets that should be protected:

```text
POSTGRES_PASSWORD
MONGO_INITDB_ROOT_PASSWORD
MONGO_APP_PASSWORD
REDIS_PASSWORD
RABBITMQ_PASSWORD
MINIO_ROOT_PASSWORD
ELASTIC_PASSWORD
GRAFANA_PASSWORD
PGADMIN_DEFAULT_PASSWORD
MONGO_EXPRESS_PASSWORD
ADMIN_OPS_KEY
JWT_SECRET_KEY
KMS_SERVICE_TOKEN
SUPER_ADMIN_PASSWORD
```

The setup flow can generate these locally, write them to `docker/.env` for local-only usage, or encrypt/wrap them through KMS for more secure storage.

### 3.2 Vault Transit integration

The repository already includes `VaultTransitClient`, which uses Vault Transit through direct REST calls.

Useful current capabilities:

```csharp
GenerateDataKeyAsync(string keyName, CancellationToken ct)
DecryptDataKeyAsync(string keyName, string ciphertextKey, CancellationToken ct)
GetLatestKeyVersionAsync(string keyName, CancellationToken ct)
```

This is useful for wrapping bootstrap secrets, generating encrypted setup manifests, validating KMS availability, and supporting future production-grade secret rotation.

### 3.3 KMS client project

The existing `Synaptix.Security.Kms.Client` is intended as a typed HTTP client package for other backend projects.

`Synaptix.Setup` should reference the KMS client rather than calling the KMS API through raw `HttpClient`.

Recommended dependency direction:

```text
Synaptix.Setup
  -> Synaptix.Security.Kms.Client
  -> Synaptix.Security.Kms.Contracts
```

Avoid:

```text
Synaptix.Setup
  -> Synaptix.Security.Kms.Api
```

The setup project should call the KMS service as a client, not embed the API.

### 3.4 Security startup validation patterns

The security documentation already recommends fail-fast behavior for staging/production when JWT secrets are missing/default, key length is below policy, or Vault is required but unreachable.

`Synaptix.Setup` should adopt this same policy style for all setup secrets.

Examples:

```text
Fail outside local if ADMIN_OPS_KEY=CHANGE_ME_IN_PRODUCTION
Fail outside local if SUPER_ADMIN_PASSWORD=ChangeMe123!
Fail outside local if JWT_SECRET_KEY contains "change-me"
Fail outside local if Redis/RabbitMQ/Postgres passwords use generated dev defaults
Fail outside local if KMS is required but unavailable
```

---

## 4. What Should Be Extended

The KMS stack is useful, but `Synaptix.Setup` needs a setup-focused wrapper layer.

Recommended new namespace/project area:

```text
Synaptix.Setup.Security
  SetupSecretProtector.cs
  SetupSecretManifest.cs
  SetupSecretValidator.cs
  SetupSecretRotationService.cs
  SetupSecretOptions.cs
```

This layer should live under `Synaptix.Setup`, not inside `Synaptix.Security.Kms`.

---

## 5. Recommended New Abstractions

### 5.1 `ISetupSecretProtector`

```csharp
public interface ISetupSecretProtector
{
    Task<ProtectedSetupSecret> ProtectAsync(
        string name,
        string plaintext,
        CancellationToken ct);

    Task<string> UnprotectAsync(
        ProtectedSetupSecret secret,
        CancellationToken ct);
}
```

Purpose:

- bridge setup-generated secrets to KMS,
- hide KMS transport details from setup tasks,
- support local plaintext fallback only in development.

### 5.2 `ProtectedSetupSecret`

```csharp
public sealed record ProtectedSetupSecret(
    string Name,
    string Ciphertext,
    string Provider,
    string KeyName,
    string KeyVersion,
    DateTimeOffset ProtectedAtUtc);
```

Purpose:

- store encrypted/wrapped setup secrets in a structured manifest,
- support key version tracking,
- allow future rotation.

### 5.3 `SetupSecretManifest`

```csharp
public sealed record SetupSecretManifest(
    int Version,
    string Environment,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyDictionary<string, ProtectedSetupSecret> Secrets);
```

Example file:

```text
.local/bootstrap/bootstrap.secrets.enc.json
```

This file can exist locally for encrypted local secret storage, but production should use a real secret manager or KMS-backed provider.

### 5.4 `SetupSecretValidator`

```csharp
public interface ISetupSecretValidator
{
    Task ValidateAsync(
        SetupSecretValidationContext context,
        CancellationToken ct);
}
```

Validation rules:

- no default passwords outside local,
- required secrets exist,
- KMS service reachable when required,
- JWT secret length meets policy,
- admin ops key is not placeholder,
- super admin password is not default,
- migration destructive flags are disabled outside local.

---

## 6. Recommended Responsibility Boundary

### 6.1 Synaptix.Security.Kms should own

```text
Encryption
Decryption
Key wrapping
Data key generation
Vault Transit client
Key version discovery
Key rotation APIs
KMS contracts
KMS client
Replay/security session primitives
Payload protection
Service-to-service KMS access
```

### 6.2 Synaptix.Setup should own

```text
Local bootstrap command
Docker .env generation
Service provisioning
Seed manifest validation
Super admin creation flow
Generated local credential output
Redis logical DB setup
Mongo database/user/index provisioning
RabbitMQ vhost/user/permission provisioning
MinIO bucket/seed upload
PostgreSQL connection/migration validation
Elasticsearch index/template validation
Setup status report
```

### 6.3 Synaptix.Setup.Security should own

```text
Protecting generated setup secrets
Unprotecting local encrypted setup manifests
Validating setup secrets against security policy
Calling Synaptix.Security.Kms.Client
Deciding whether plaintext local fallback is allowed
```

---

## 7. Recommended Setup Flow With KMS

### 7.1 Local development flow

```bash
dotnet run --project Synaptix.Setup -- init-local
```

Steps:

1. Generate local secrets.
2. Write `docker/.env`.
3. Optionally protect secrets through KMS if KMS is available.
4. Write local admin credential file.
5. Write bootstrap status report.

Local outputs:

```text
docker/.env
.local/bootstrap/super-admin.local.txt
.local/bootstrap/bootstrap-status.json
.local/bootstrap/bootstrap.secrets.enc.json
```

All `.local/` files must be ignored by Git.

### 7.2 Secure local flow

```bash
dotnet run --project Synaptix.Setup -- init-secure-local
```

Steps:

1. Generate local secrets.
2. Start or validate KMS.
3. Call KMS to wrap setup secrets.
4. Write encrypted setup manifest.
5. Write `docker/.env` only when needed for Docker runtime.
6. Record key version metadata.

### 7.3 Staging/production flow

```bash
dotnet run --project Synaptix.Setup -- validate --environment production --strict
dotnet run --project Synaptix.Setup -- provision-services --environment production --strict
dotnet run --project Synaptix.MigrationService
```

Rules:

- do not generate production secrets automatically,
- require all secrets from external secret provider,
- require KMS if configured as required,
- reject placeholder/default values,
- reject destructive migration flags,
- strict seed readiness must pass.

---

## 8. Example Encrypted Bootstrap Manifest

```json
{
  "version": 1,
  "environment": "local",
  "createdAtUtc": "2026-06-03T12:00:00Z",
  "secrets": {
    "POSTGRES_PASSWORD": {
      "name": "POSTGRES_PASSWORD",
      "ciphertext": "vault:v1:...",
      "provider": "SynaptixKms",
      "keyName": "synaptix-setup-bootstrap",
      "keyVersion": "v1",
      "protectedAtUtc": "2026-06-03T12:00:00Z"
    },
    "JWT_SECRET_KEY": {
      "name": "JWT_SECRET_KEY",
      "ciphertext": "vault:v1:...",
      "provider": "SynaptixKms",
      "keyName": "synaptix-setup-bootstrap",
      "keyVersion": "v1",
      "protectedAtUtc": "2026-06-03T12:00:00Z"
    }
  }
}
```

---

## 9. Recommended Key Names

Create explicit KMS key names for setup operations:

```text
synaptix-setup-bootstrap
synaptix-setup-super-admin
synaptix-setup-service-credentials
synaptix-jwt-signing-material
synaptix-admin-ops-key
```

Do not reuse payload/session keys for bootstrap secrets.

Existing runtime keys such as:

```text
synaptix-session-wrap
synaptix-payload-wrap
```

should remain focused on runtime security.

---

## 10. Required Security Rules

### 10.1 Never commit

```text
docker/.env
.local/
*.local.secret
*.generated.secret
bootstrap.secrets.enc.json if it contains environment-specific local secrets
super-admin.local.txt
```

### 10.2 Must fail outside local

`Synaptix.Setup validate --strict` must fail if:

```text
SUPER_ADMIN_PASSWORD=ChangeMe123!
ADMIN_OPS_KEY=CHANGE_ME_IN_PRODUCTION
JWT_SECRET_KEY contains "change-me"
MIGRATION_RESET_DATABASE=true
MIGRATION_ALLOW_ENSURE_CREATED=true
Vault/KMS is required but unreachable
Any required DB/service password is blank
Any generated dev password is reused in staging/prod
```

### 10.3 Should warn locally

Local setup may warn but continue for:

```text
KMS unavailable when local plaintext fallback is enabled
Optional analytics services disabled
Elasticsearch disabled
RabbitMQ disabled if background jobs are feature-flagged off
MinIO seed fallback using bundled seed files
```

---

## 11. Recommended Project References

`Synaptix.Setup.csproj` should reference:

```xml
<ItemGroup>
  <ProjectReference Include="..\Synaptix.Security.Kms.Client\Synaptix.Security.Kms.Client.csproj" />
  <ProjectReference Include="..\Synaptix.Security.Kms.Contracts\Synaptix.Security.Kms.Contracts.csproj" />
  <ProjectReference Include="..\Synaptix.Shared\Synaptix.Shared.csproj" />
</ItemGroup>
```

Only reference infrastructure projects if absolutely necessary.

Avoid referencing:

```text
Synaptix.Security.Kms.Api
Synaptix.Backend.Api
```

The setup tool should not embed API hosts.

---

## 12. Recommended Implementation Phases

### Phase 1 — Alpha/Beta Practical Use (implemented)

Priority: P0/P1

Implement inside `Synaptix.Setup`:

```text
SecretGenerator
SetupSecretValidator
docker/.env generator
super admin credential generator
strict environment validation
.gitignore updates
setup status report
```

KMS integration can be optional at first:

```text
SetupSecrets:ProtectionMode=PlaintextLocal
```

Valid modes:

```text
PlaintextLocal
KmsRequired
KmsPreferred
ExternalOnly
```

Recommended Alpha default:

```text
local: KmsPreferred
staging: KmsRequired
production: KmsRequired
```

### Phase 2 — KMS-backed setup secrets (deferred)

Priority: P1

Add:

```text
ISetupSecretProtector
KmsSetupSecretProtector
ProtectedSetupSecret
SetupSecretManifest
```

Use `Synaptix.Security.Kms.Client` for wrapping/unwrapping.

### Phase 3 — Secret rotation

Priority: P2

Add commands:

```bash
dotnet run --project Synaptix.Setup -- rotate-secret POSTGRES_PASSWORD
dotnet run --project Synaptix.Setup -- rotate-super-admin-password
dotnet run --project Synaptix.Setup -- rotate-admin-ops-key
dotnet run --project Synaptix.Setup -- rotate-jwt-secret
```

Rotation should:

1. generate replacement secret,
2. protect through KMS,
3. update target service/config,
4. record status,
5. optionally invalidate sessions or require restart.

### Phase 4 — Production secret provider support

Priority: P2/P3

Add providers:

```text
EnvironmentVariableSecretProvider
DockerSecretProvider
KmsProtectedManifestSecretProvider
CloudSecretProvider
```

Future cloud providers:

```text
AWS Secrets Manager
Azure Key Vault
GCP Secret Manager
HashiCorp Vault KV
```

---

## 13. Final Recommendation

Use the existing `Synaptix.Security.Kms` system for:

```text
secret wrapping
data key generation
Vault Transit
KMS API/client access
key version tracking
production secret validation patterns
future secret rotation
```

Do **not** make KMS responsible for setup.

Extend the repo with:

```text
Synaptix.Setup.Security
```

or a `Security/` folder inside `Synaptix.Setup`.

This gives the cleanest separation:

```text
Synaptix.Security.Kms = protects secrets
Synaptix.Setup = creates and provisions setup state
Synaptix.Setup.Security = connects setup secrets to KMS
```

That design is clean, testable, Alpha/Beta-friendly, and strong enough for long-term platform architecture.

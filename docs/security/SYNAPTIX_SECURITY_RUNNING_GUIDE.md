# Synaptix Security Stack — Running Guide

How to run the Synaptix KMS service locally, in Docker, and alongside the full Tycoon backend.

---

## Contents

1. [Architecture overview](#1-architecture-overview)
2. [Prerequisites](#2-prerequisites)
3. [Option A — Security stack alone (fastest)](#3-option-a--security-stack-alone-fastest)
4. [Option B — Security stack + main Tycoon stack](#4-option-b--security-stack--main-tycoon-stack)
5. [Option C — `dotnet run` locally (no Docker)](#5-option-c--dotnet-run-locally-no-docker)
6. [Environment variable reference](#6-environment-variable-reference)
7. [Port reference](#7-port-reference)
8. [Testing the endpoints](#8-testing-the-endpoints)
9. [Integrating the KMS Client into another service](#9-integrating-the-kms-client-into-another-service)
10. [Troubleshooting](#10-troubleshooting)

---

## 1. Architecture overview

```
┌──────────────────────────────────────────────────────────┐
│  docker/compose.security.yml  (name: tycoon-security)    │
│                                                          │
│  ┌─────────────┐   healthy   ┌──────────────────────┐   │
│  │  vault      │──────────►  │  vault-init (once)   │   │
│  │  :8200      │             │  provisions 4 transit │   │
│  └─────────────┘             │  keys, then exits     │   │
│         │                    └──────────────────────┘   │
│         │ vault-init done                                 │
│         ▼                                                │
│  ┌─────────────────────────────────────────────────┐    │
│  │  kms-api   :5050 (internal) / :5060 (host)      │    │
│  │  Synaptix.Security.Kms.Api                      │    │
│  │  ├── /security/sessions/*  (JWT auth)           │    │
│  │  ├── /security/payload/*   (JWT auth)           │    │
│  │  ├── /security/keys/*      (X-Service-Token)    │    │
│  │  ├── /internal/security/*  (X-Service-Token)    │    │
│  │  └── /health, /alive                            │    │
│  └─────────────────────────────────────────────────┘    │
└──────────────────────────────────────────────────────────┘

Other Tycoon services call the KMS API via Tycoon.Security.Kms.Client
(typed HTTP client, one-line DI registration: services.AddKmsClient(...))
```

**Key design decisions:**
- Vault runs in **dev mode** — in-memory only, resets on restart. For production use a properly initialised and unsealed Vault with persistent storage.
- When `ConnectionStrings:cache` is empty the KMS API uses an **in-memory distributed cache** (no Redis required for local development).
- When the `Vault` config section is absent or `Vault:Required = false`, the KMS API falls back to `NullKeyWrappingService` (generates random keys locally).

---

## 2. Prerequisites

| Requirement | Version | Check |
|---|---|---|
| Docker Desktop / Docker Engine | ≥ 24 | `docker --version` |
| Docker Compose | ≥ 2.20 (V2 plugin) | `docker compose version` |
| .NET SDK *(Option C only)* | 10.0.100 | `dotnet --version` |

No other tools are needed for Options A and B.

---

## 3. Option A — Security stack alone (fastest)

This runs Vault + KMS API with no other dependencies. Uses in-memory cache — no Redis required.

```bash
# From the repository root
docker compose -f docker/compose.security.yml up --build
```

**What happens:**
1. Vault container starts in dev mode and passes its healthcheck.
2. `vault-init` runs once and provisions 4 Transit encryption keys, then exits.
3. The KMS API builds from source, starts on `:5060`, and passes its curl healthcheck.

**Verify it's healthy:**

```bash
curl http://localhost:5060/health
# {"status":"Healthy"}

curl http://localhost:5060/
# {"service":"Synaptix.Security.Kms","version":"1.0"}
```

**Stop:**

```bash
docker compose -f docker/compose.security.yml down
```

**Stop and remove volumes:**

```bash
docker compose -f docker/compose.security.yml down -v
```

> Vault dev mode has no persistent volumes anyway, so `-v` only matters once you switch to production Vault.

---

## 4. Option B — Security stack + main Tycoon stack

Use this when you need the KMS API to use the shared Redis instance and be accessible from the other Tycoon services within Docker networking.

### Step 1 — Start main infrastructure

```bash
docker compose -f docker/compose.yml up -d
```

Wait until all services are healthy:

```bash
docker compose -f docker/compose.yml ps
```

### Step 2 — Connect KMS to shared Redis

Set the Redis URL to point at the Redis container by name (Docker's internal DNS):

```bash
export KMS_REDIS_URL="redis:6379,password=tycoon_redis_password_123"
```

Or add it to `docker/.env`:

```env
KMS_REDIS_URL=redis:6379,password=tycoon_redis_password_123
```

### Step 3 — Start the security stack

```bash
docker compose -f docker/compose.security.yml up -d
```

### Step 4 — Verify

```bash
# From host
curl http://localhost:5060/health

# From inside another container (uses Docker internal DNS)
# curl http://kms-api:5050/health
```

### Stopping

```bash
docker compose -f docker/compose.security.yml down
docker compose -f docker/compose.yml down
```

---

## 5. Option C — `dotnet run` locally (no Docker)

For fast iteration where you want to edit code and restart immediately.

### Step 1 — Start Vault (still needs Docker)

```bash
docker compose -f docker/compose.security.yml up vault vault-init -d
```

This starts only Vault and runs the key provisioner. Vault UI is at `http://localhost:8210`.

### Step 2 — Run the KMS API on the host

```bash
dotnet run --project Synaptix.Security.Kms.Api/Synaptix.Security.Kms.Api.csproj
```

The API binds to `http://localhost:5050` (from `launchSettings.json`).

`appsettings.Development.json` is pre-configured to connect to Vault at `http://localhost:8200` — note this is the Vault container's exposed port `8210` remapped; you may need to update this if you changed `VAULT_PORT`.

```bash
# Adjust if VAULT_PORT != 8210
# Edit appsettings.Development.json → Vault:Address → "http://localhost:8210"
```

### Step 3 — Verify

```bash
curl http://localhost:5050/health
curl http://localhost:5050/
```

### Without Vault at all

Leave the Vault section out of config or set `Vault:Required = false` (the default in `appsettings.Development.json`). The API starts with `NullKeyWrappingService` — it generates random keys locally rather than using Vault Transit. Sessions and payloads still work; keys are not persisted across restarts.

---

## 6. Environment variable reference

All environment variables for `compose.security.yml`. Set in your shell or in `docker/.env`.

| Variable | Default (dev) | Description |
|---|---|---|
| `VAULT_PORT` | `8210` | Host-side port for Vault UI / API |
| `KMS_API_PORT` | `5060` | Host-side port for the KMS API |
| `VAULT_DEV_TOKEN` | `dev-root-token-change-me` | Vault root token (dev mode only — change this!) |
| `KMS_SERVICE_TOKEN` | `kms-internal-service-token-change-me` | Shared secret sent in `X-Service-Token` header by internal callers |
| `JWT_SECRET_KEY` | `your-super-secret-jwt-key-change-me-in-production-minimum-32-characters-long` | Must match `JwtSettings:SecretKey` in the main Tycoon backend |
| `VAULT_REQUIRED` | `false` | Set to `true` to refuse startup if Vault is unreachable |
| `KMS_REDIS_URL` | *(empty)* | Redis connection string. Empty → in-memory cache |
| `ASPNETCORE_ENVIRONMENT` | `Development` | `Development` allows dev-mode JWT passthrough |
| `LOG_LEVEL` | `Information` | Serilog minimum level |
| `BUILD_CONFIGURATION` | `Release` | `Debug` for local development builds |

**Production checklist:**
- Replace all three `*-change-me` / `*-in-production` values.
- Set `VAULT_REQUIRED=true`.
- Set `ASPNETCORE_ENVIRONMENT=Production` — this enforces JWT validation and disables the dev passthrough.
- Use a properly initialised Vault with persistent storage and TLS.

---

## 7. Port reference

| Service | Internal port | Host port | URL |
|---|---|---|---|
| Vault | 8200 | 8210 | `http://localhost:8210` |
| KMS API | 5050 | 5060 | `http://localhost:5060` |
| KMS API (dotnet run) | 5050 | 5050 | `http://localhost:5050` |

The main Tycoon stack keeps port `8200` for the operator dashboard, which is why Vault is mapped to `8210`.

---

## 8. Testing the endpoints

All examples use the default dev tokens. Adjust ports for `dotnet run` (5050) vs Docker (5060).

### Health

```bash
curl -s http://localhost:5060/health | jq .
curl -s http://localhost:5060/alive  | jq .
```

### Service info

```bash
curl -s http://localhost:5060/ | jq .
```

### Session start (requires JWT)

In dev mode with `ASPNETCORE_ENVIRONMENT=Development` and no JWT secret configured, the API accepts any well-formed JWT without signature validation.

Generate a throwaway dev token (Python one-liner):

```bash
python3 -c "
import base64, json
h = base64.urlsafe_b64encode(json.dumps({'alg':'HS256','typ':'JWT'}).encode()).rstrip(b'=').decode()
p = base64.urlsafe_b64encode(json.dumps({'sub':'user-123','iat':9999999999}).encode()).rstrip(b'=').decode()
print(f'{h}.{p}.devonly')
"
```

```bash
DEV_TOKEN="<output from above>"

curl -s -X POST http://localhost:5060/security/sessions/start \
  -H "Authorization: Bearer $DEV_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "deviceId": "device-abc",
    "clientNonce": "dGVzdC1ub25jZQ==",
    "clientPublicKey": "dGVzdC1wdWJsaWMta2V5LTMyLWJ5dGVzPT09PT0=",
    "supportedSuites": ["X25519-HKDF-SHA256-AES256GCM"]
  }' | jq .
```

### Payload encrypt (requires JWT)

`aad` is optional on direct encrypt calls. Secure-channel responses use backend-derived AAD from the protected request context.

```bash
curl -s -X POST http://localhost:5060/security/payload/encrypt \
  -H "Authorization: Bearer $DEV_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "sessionId": "<session-id-from-start>",
    "plaintext": "SGVsbG8gV29ybGQ=",
    "contentType": "application/json",
    "aad": "syn-sec-v1|response|POST|/auth/refresh|<session-id-n>|1|user-123|2026-05-21T00:00:00.0000000Z"
  }' | jq .
```

### Payload decrypt (requires JWT)

Decrypt calls require replay metadata and request-context AAD. The backend secure-channel middleware derives this AAD automatically for protected API routes.

```bash
curl -s -X POST http://localhost:5060/security/payload/decrypt \
  -H "Authorization: Bearer $DEV_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "sessionId": "<session-id-from-start>",
    "ciphertext": "<base64url-ciphertext>",
    "nonce": "<base64url-aes-gcm-nonce>",
    "mac": "<base64url-aes-gcm-tag>",
    "contentType": "application/json",
    "encryptedAtUtc": "2026-05-21T00:00:00.0000000Z",
    "sequenceNumber": 1,
    "replayNonce": "client-random-replay-nonce",
    "aad": "syn-sec-v1|request|POST|/auth/refresh|<session-id-n>|1|user-123|2026-05-21T00:00:00.0000000Z",
    "subjectId": "user-123"
  }' | jq .
```

### Internal datakey generation (requires X-Service-Token)

```bash
curl -s -X POST http://localhost:5060/internal/security/datakey \
  -H "X-Service-Token: kms-internal-service-token-change-me" \
  -H "Content-Type: application/json" \
  -d '{
    "keyContext": "my-service",
    "keyBits": 256
  }' | jq .
```

### Key rotation (requires X-Service-Token)

```bash
curl -s -X POST http://localhost:5060/security/keys/rotate \
  -H "X-Service-Token: kms-internal-service-token-change-me" \
  -H "Content-Type: application/json" \
  -d '{"keyName": "synaptix-session-wrap"}' | jq .
```

### OpenAPI (Development only)

```bash
# JSON schema
curl -s http://localhost:5060/openapi/v1.json | jq .

# Or open in browser
open http://localhost:5060/openapi/v1.json
```

---

## 9. Integrating the KMS Client into another service

Any other Tycoon backend project can talk to the KMS API using the typed client library.

### 1. Add project reference

```xml
<!-- In YourService.csproj -->
<ItemGroup>
  <ProjectReference Include="..\Tycoon.Security.Kms.Client\Tycoon.Security.Kms.Client.csproj" />
</ItemGroup>
```

### 2. Register in DI

```csharp
// In Program.cs or DependencyInjection.cs
builder.Services.AddKmsClient(builder.Configuration);
```

### 3. Add config

```json
// appsettings.json
{
  "KmsClient": {
    "BaseUrl": "http://kms-api:5050",
    "ServiceToken": "kms-internal-service-token-change-me",
    "TimeoutSeconds": 10,
    "MaxRetryAttempts": 3
  }
}
```

For local development (`dotnet run` without Docker), change `BaseUrl` to `http://localhost:5060`.

### 4. Inject and use

```csharp
public class MyService(IKmsSessionClient sessions, IKmsPayloadClient payload)
{
    public async Task<string> StartSessionAsync(string deviceId)
    {
        var response = await sessions.StartSessionAsync(new StartSecureSessionRequest(
            DeviceId: deviceId,
            ClientNonce: Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            ClientPublicKey: /* your X25519 public key, base64 */,
            SupportedSuites: [SecureSuites.ClassicalV1]
        ));
        return response.SessionId.ToString();
    }
}
```

---

## 10. Troubleshooting

### Vault exits immediately with `unable to set CAP_SETFCAP`

Already fixed — `VAULT_DISABLE_MLOCK=true` is set in `compose.security.yml`. If you see this with a fresh pull, ensure you have the latest version of the file.

### KMS API container is unhealthy / curl fails

Check the KMS API logs:

```bash
docker compose -f docker/compose.security.yml logs kms-api
```

Common causes:
- **Vault not ready** — `vault-init` must complete before the KMS API starts. The `depends_on` condition handles this automatically, but on slow machines the 20-second `start_period` may need to be increased in `compose.security.yml`.
- **Port conflict** — Something else on your machine is using port `5060` or `8210`. Change `KMS_API_PORT` or `VAULT_PORT` in your environment.

### `vault-init` exits with `connection refused`

Vault's healthcheck hasn't passed yet. This is usually a timing issue on first run — re-running `docker compose -f docker/compose.security.yml up` is enough.

### JWT 401 Unauthorized in development

The dev JWT passthrough only activates when **all three** conditions are met:
1. `ASPNETCORE_ENVIRONMENT=Development`
2. `Jwt:Authority` is empty
3. `JwtSettings:SecretKey` is empty

If `JwtSettings:SecretKey` is set (e.g., via environment variable), the API validates signatures. Either clear it or sign your test tokens with that key.

### Session operations fail with `NullKeyWrappingService`

This is the expected dev fallback when Vault is not configured. Keys are ephemeral (lost on restart) but sessions and payloads still work end-to-end. To use Vault, ensure Vault is running and `Vault:Address` points to it in `appsettings.Development.json` (default: `http://localhost:8200` for `dotnet run`, `http://vault:8200` for Docker).

### In-memory cache lost after restart

This is by design in dev mode. Redis persistence is opt-in via `KMS_REDIS_URL`. For stateful testing, run the main stack and set `KMS_REDIS_URL`.

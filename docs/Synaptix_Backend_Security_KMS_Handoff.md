# Synaptix Backend Security + Vault/KMS Implementation Handoff

Repository: `devartblake/TycoonTycoon_Backend`

## Objective

Create a reusable `.NET 10` security subsystem that provides secure session negotiation, app-layer payload encryption/decryption, replay protection, Vault Transit integration, key rotation, and service-to-service reuse.

The backend security system should be callable by other backend systems through REST, gRPC, or package reference.

## Recommended solution name

Use one of:

- `Synaptix.SecurityGateway`
- `Tycoon.Security.Core`
- `Tycoon.SecurityService`

Recommended project structure:

```text
src/
  Synaptix.Security.Core/
    Abstractions/
    Models/
    Crypto/
    Sessions/
    Replay/

  Synaptix.Security.Api/
    Endpoints/
    Middleware/
    Program.cs

  Synaptix.Security.AspNetCore/
    EndpointFilters/
    Middleware/
    ServiceCollectionExtensions.cs

  Synaptix.Security.Vault/
    VaultTransitClient.cs
    VaultOptions.cs
    VaultEnvelopeEncryptionService.cs

  Synaptix.Security.Tests/
```

## Backend integration points

The existing backend already includes:

- JWT bearer auth.
- Rate limiting.
- ASP.NET Data Protection.
- SignalR and WebSocket endpoints.
- gRPC endpoints.
- Admin ops key middleware.
- Feature endpoint grouping.
- `Features.Crypto` namespace.

The new security layer should integrate without replacing the existing auth system.

## Critical hardening item

Remove production fallback behavior for JWT secrets.

Production/staging must fail if:

- `JwtSettings:SecretKey` is missing.
- JWT key is default dev value.
- JWT key length is below policy.
- Vault is required but unreachable.

Suggested policy:

```csharp
if (!builder.Environment.IsDevelopment() && IsDevJwtKey(jwtSettings.SecretKey))
{
    throw new InvalidOperationException("Refusing to start with development JWT key outside Development.");
}
```

## API endpoints

### `POST /security/sessions/start`

Creates a short-lived secure channel session.

Request:

```csharp
public sealed record StartSecureSessionRequest(
    string DeviceId,
    string ClientNonce,
    string ClientPublicKey,
    IReadOnlyList<string> SupportedSuites);
```

Response:

```csharp
public sealed record StartSecureSessionResponse(
    Guid SessionId,
    string ProtocolVersion,
    string SelectedSuite,
    string ServerPublicKey,
    string ServerNonce,
    DateTimeOffset ExpiresAtUtc,
    string ServerSignature);
```

### `POST /security/sessions/renew`

Renews or replaces an active secure session.

### `POST /security/payload/decrypt`

Internal-only endpoint for services that want centralized decryption.

### `POST /security/payload/encrypt`

Internal-only endpoint for services that want centralized encryption.

### `POST /security/keys/rotate`

Admin/internal endpoint for controlled key rotation.

## Core interfaces

```csharp
public interface ISecureSessionService
{
    Task<StartSecureSessionResponse> StartAsync(
        ClaimsPrincipal user,
        StartSecureSessionRequest request,
        CancellationToken ct);

    Task<SecureSession?> GetAsync(Guid sessionId, CancellationToken ct);
    Task RevokeAsync(Guid sessionId, string reason, CancellationToken ct);
}

public interface ISecurePayloadProtector
{
    Task<byte[]> DecryptRequestAsync(
        ClaimsPrincipal user,
        HttpRequest request,
        EncryptedPayload payload,
        CancellationToken ct);

    Task<EncryptedPayload> EncryptResponseAsync(
        ClaimsPrincipal user,
        HttpRequest request,
        byte[] plaintext,
        CancellationToken ct);
}

public interface IReplayProtectionStore
{
    Task<bool> TryAcceptAsync(
        Guid sessionId,
        long sequence,
        string nonce,
        TimeSpan ttl,
        CancellationToken ct);
}

public interface IKeyWrappingService
{
    Task<WrappedDataKey> GenerateDataKeyAsync(string keyName, CancellationToken ct);
    Task<byte[]> UnwrapDataKeyAsync(string encryptedDataKey, CancellationToken ct);
}
```

## Models

```csharp
public sealed record EncryptedPayload(
    string Ciphertext,
    string Nonce,
    string Mac,
    string ContentType,
    DateTimeOffset EncryptedAtUtc);

public sealed record SecureSession(
    Guid SessionId,
    string SubjectId,
    string DeviceId,
    string ProtocolVersion,
    string Suite,
    byte[] ClientToServerKey,
    byte[] ServerToClientKey,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    long LastSequence);

public sealed record WrappedDataKey(
    byte[] PlaintextKey,
    string EncryptedKey,
    string KeyVersion);
```

## Middleware / endpoint filter

Add an attribute or endpoint filter:

```csharp
public sealed class RequireEncryptedPayloadAttribute : Attribute;
```

Example:

```csharp
group.MapPost("/economy/claim", ClaimRewardAsync)
     .RequireAuthorization()
     .AddEndpointFilter<EncryptedPayloadEndpointFilter>();
```

## Replay protection

Use Redis for sequence and nonce tracking:

```text
synsec:session:{sessionId}:seq:{sequence} = 1 TTL 30m
synsec:session:{sessionId}:nonce:{nonce} = 1 TTL 30m
```

Reject requests if:

- Sequence is reused.
- Nonce is reused.
- Timestamp is outside allowed skew.
- Session subject/device does not match JWT.

## Vault Transit integration

### Docker Compose dev service

```yaml
vault:
  image: hashicorp/vault:latest
  container_name: synaptix-vault
  ports:
    - "8200:8200"
  environment:
    VAULT_DEV_ROOT_TOKEN_ID: dev-root-token-change-me
    VAULT_DEV_LISTEN_ADDRESS: 0.0.0.0:8200
  cap_add:
    - IPC_LOCK
  command: server -dev
```

Production should use initialized/unsealed Vault with persistent storage, not dev mode.

### Vault options

```csharp
public sealed class VaultOptions
{
    public required string Address { get; init; }
    public required string Token { get; init; }
    public string TransitMount { get; init; } = "transit";
    public string SessionWrapKey { get; init; } = "synaptix-session-wrap";
    public string PayloadWrapKey { get; init; } = "synaptix-payload-wrap";
    public bool Required { get; init; } = true;
}
```

### Vault Transit key setup

```bash
vault secrets enable transit
vault write -f transit/keys/synaptix-session-wrap type=aes256-gcm96
vault write -f transit/keys/synaptix-payload-wrap type=aes256-gcm96
vault write -f transit/keys/synaptix-refresh-token-wrap type=aes256-gcm96
vault write -f transit/keys/synaptix-data-protection-wrap type=aes256-gcm96
```

## Envelope encryption service

Use Vault Transit for key wrapping, then encrypt locally for performance:

```csharp
public sealed class VaultEnvelopeEncryptionService : IKeyWrappingService
{
    private readonly VaultTransitClient _vault;

    public VaultEnvelopeEncryptionService(VaultTransitClient vault)
    {
        _vault = vault;
    }

    public Task<WrappedDataKey> GenerateDataKeyAsync(string keyName, CancellationToken ct)
        => _vault.GenerateDataKeyAsync(keyName, ct);

    public Task<byte[]> UnwrapDataKeyAsync(string encryptedDataKey, CancellationToken ct)
        => _vault.DecryptDataKeyAsync(encryptedDataKey, ct);
}
```

## Data Protection integration

Current backend persists ASP.NET Data Protection keys to filesystem. For containerized production, move to persistent external storage and protect keys with an external key provider or Vault-backed wrapping where available.

Minimum acceptable production improvement:

- Persist Data Protection keys to a mounted Docker volume.
- Restrict file permissions.
- Back up key ring securely.

Preferred improvement:

- Store/protect key ring with external KMS/Vault integration.

## Key rotation policy

| Key | Rotation |
|---|---|
| Secure session keys | Every session / 10–30 min TTL |
| Vault Transit wrapping keys | 30–90 days initially |
| JWT signing keys | 30–90 days with overlap/JWKS if asymmetric |
| Refresh token encryption key | 30–90 days |
| Admin ops key | 30 days or immediately after personnel/tooling changes |
| Data Protection keys | Follow ASP.NET Core policy, persist safely |

## Post-quantum readiness

Design protocol versioning now:

```csharp
public static class SecureSuites
{
    public const string ClassicalV1 = "X25519-HKDF-SHA256-AES256GCM";
    public const string HybridPqV1 = "X25519-MLKEM768-HKDF-SHA256-AES256GCM";
}
```

Initial implementation should select `ClassicalV1`.

Future implementation can add `HybridPqV1` behind a feature flag after library review.

## Service-to-service usage

Other backend systems should call security operations through:

### REST

```http
POST /internal/security/encrypt
POST /internal/security/decrypt
POST /internal/security/datakey
```

### gRPC

```proto
service SecurityGateway {
  rpc Encrypt(EncryptRequest) returns (EncryptResponse);
  rpc Decrypt(DecryptRequest) returns (DecryptResponse);
  rpc GenerateDataKey(GenerateDataKeyRequest) returns (GenerateDataKeyResponse);
}
```

### NuGet package

Expose reusable interfaces and middleware through:

```text
Synaptix.Security.Core
Synaptix.Security.AspNetCore
Synaptix.Security.Vault
```

## Implementation milestones

### Milestone 1 — Security hardening

- Enforce no dev JWT key outside development.
- Add Vault options.
- Add Vault container for dev.
- Add key naming policy.
- Add security health check.

### Milestone 2 — Secure session API

- Add `Synaptix.Security.Core`.
- Add session start/renew/revoke endpoints.
- Store secure sessions in Redis.
- Bind sessions to JWT subject + device ID.

### Milestone 3 — Payload protection

- Add AEAD encryption/decryption service.
- Add endpoint filter.
- Add replay protection store.
- Protect one low-risk endpoint in dev.

### Milestone 4 — Vault envelope encryption

- Add Vault Transit client.
- Add data key generation.
- Add local AEAD data encryption.
- Add key rotation command/job.

### Milestone 5 — Internal security gateway

- Add gRPC service.
- Add REST internal endpoints.
- Add service auth policy.
- Package reusable libraries.

### Milestone 6 — PQ/hybrid experiment

- Add capability negotiation field.
- Add ML-KEM library spike in backend only.
- Add hybrid suite behind feature flag.
- Run performance and compatibility tests.

## QA checklist

- Missing Vault in production fails startup if required.
- Default JWT key fails outside development.
- Encrypted request decrypts successfully.
- Tampered ciphertext fails.
- Tampered AAD fails.
- Reused nonce fails.
- Reused sequence fails.
- Expired session fails.
- Session bound to wrong JWT subject fails.
- Vault key rotation does not break active data reads.
- Load test confirms acceptable overhead.
- Audit logs record key operations.

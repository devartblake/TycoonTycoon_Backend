# Synaptix Frontend ↔ Backend Encryption & Key Management Recommendation

## Executive answer

You should **not create a custom encryption algorithm**. A claim that any algorithm “can’t be broken” by current or next-generation decryption protocols is not technically defensible. The right approach is to build a **cryptographic architecture** using standardized, reviewed primitives, key rotation, Vault-backed key custody, short-lived session keys, replay protection, and post-quantum migration readiness.

Recommended target architecture:

1. **Transport security:** TLS 1.3 everywhere, with certificate pinning for mobile builds where operationally acceptable.
2. **Application-layer secure envelope:** request/response payload encryption for sensitive operations using AEAD: `AES-256-GCM` or `XChaCha20-Poly1305`.
3. **Session key establishment:** server-issued ephemeral session keys initially, then hybrid ECDH + post-quantum KEM once stable libraries are selected.
4. **Vault/KMS:** HashiCorp Vault Transit as the key authority for root keys, key wrapping, rotation, audit logging, and envelope encryption.
5. **Reusable backend security project:** a standalone `.NET 10` service/library called `Synaptix.SecurityGateway` or `Tycoon.Security.Core` that other backend systems can call.
6. **No static encryption secrets in the Flutter app.** The client can hold only short-lived session keys and public keys.

## Repository-specific fit

### Frontend: `devartblake/trivia_tycoon`

The Flutter app already has a centralized `ServiceManager` that initializes `ApiService`, `SecureStorage`, `EncryptionService`, `FernetService`, `AuthTokenStore`, `AuthHttpClient`, SignalR hubs, and `SynaptixApiClient`. That makes it a good fit for adding a `SecureChannelService` without scattering crypto logic across screens.

Current relevant frontend files:

- `lib/core/manager/service_manager.dart`
- `lib/core/services/encryption/encryption_service.dart`
- `lib/core/services/encryption/fernet_service.dart`
- `lib/core/services/storage/secure_storage.dart`
- `lib/core/networking/http_client.dart`
- `lib/core/services/auth_http_client.dart`
- `lib/core/services/auth_token_store.dart`

The existing `EncryptionService` currently wraps AES, Fernet, file encryption, passphrase-derived encryption, and Fernet key clearing. Keep that service for local app encryption, but do **not** use it as the final client-server encryption contract. Add a new dedicated network security service.

### Backend: `devartblake/TycoonTycoon_Backend`

The backend already has JWT bearer auth, rate limiting, SignalR, gRPC, data protection key persistence, admin ops key middleware, feature endpoint grouping, and a `Tycoon.Backend.Api.Features.Crypto` namespace. That means the backend is ready for a formal security module rather than a scattered implementation.

Current relevant backend areas:

- `Tycoon.Backend.Api/Program.cs`
- `Tycoon.Backend.Api/Security/*`
- `Tycoon.Backend.Api/Features/Crypto/*`
- `Tycoon.Backend.Application/Auth/*`
- `Tycoon.Backend.Infrastructure/*`

Important current risk to fix: the backend has a development fallback JWT key path. Development fallback keys are acceptable only in local dev, but should be blocked in staging and production.

## What “unbreakable” should mean in practice

Replace “unbreakable” with these enforceable goals:

| Goal | Meaning |
|---|---|
| Confidentiality | Attackers cannot read sensitive payloads without the right session keys. |
| Integrity | Attackers cannot alter payloads undetected. |
| Forward secrecy | Compromise of a long-term key should not expose old sessions. |
| Replay resistance | Captured requests cannot be reused successfully. |
| Key rotation | Keys can be rotated without app reinstall or downtime. |
| Quantum readiness | Architecture can adopt ML-KEM hybrid key exchange without redesign. |
| Operational auditability | Key use is logged through Vault/KMS. |

## Recommended cryptographic stack

### Phase 1: Production-ready classical security

Use this first because it is stable and deployable:

- TLS 1.3 for all traffic.
- JWT access token + refresh token auth.
- App-layer AEAD for high-value payloads.
- `AES-256-GCM` for backend/server-side encryption.
- `XChaCha20-Poly1305` or `AES-GCM` on Flutter depending on library/platform support.
- HKDF-SHA256 for deriving directional keys.
- HMAC-SHA256 for request signing where encryption is not needed.
- Nonce + timestamp + sequence number for replay protection.
- Vault Transit for root wrapping keys and data encryption key generation.

### Phase 2: Hybrid post-quantum readiness

Add a versioned handshake that can support:

- Classical ECDH: X25519.
- Post-quantum KEM: ML-KEM-768 as the preferred security/performance balance.
- Hybrid shared secret: `HKDF(X25519_shared_secret || ML-KEM_shared_secret)`.

Do not force PQ crypto directly into the Flutter app until the library choice is stable across Android, iOS, web, and desktop. Instead, design the protocol fields now:

```json
{
  "protocolVersion": "syn-sec-v1",
  "kemSuite": "X25519+ML-KEM-768",
  "clientEphemeralPublicKey": "base64url",
  "clientKemCiphertext": "base64url-or-null",
  "clientNonce": "base64url",
  "deviceId": "string",
  "authTokenBinding": "sha256-of-access-token"
}
```

### Phase 3: Full reusable security service

Create a standalone backend security service:

```text
src/
  Synaptix.Security.Core/
  Synaptix.Security.Api/
  Synaptix.Security.Vault/
  Synaptix.Security.AspNetCore/
  Synaptix.Security.Tests/
```

Other backend systems can call it via:

- REST for general apps.
- gRPC for internal high-performance services.
- NuGet package reference for in-process `.NET` usage.

## Protocol design

### 1. Client starts secure session

`POST /security/sessions/start`

Request:

```json
{
  "deviceId": "ios:device-id",
  "clientNonce": "base64url-24-bytes",
  "clientPublicKey": "base64url-x25519-public-key",
  "supportedSuites": [
    "X25519-HKDF-SHA256-AES256GCM",
    "X25519-MLKEM768-HKDF-SHA256-AES256GCM"
  ]
}
```

Response:

```json
{
  "sessionId": "uuid",
  "protocolVersion": "syn-sec-v1",
  "selectedSuite": "X25519-HKDF-SHA256-AES256GCM",
  "serverPublicKey": "base64url-x25519-public-key",
  "serverNonce": "base64url-24-bytes",
  "expiresAtUtc": "2026-05-02T18:30:00Z",
  "serverSignature": "base64url-signature"
}
```

### 2. Client derives keys

Derive two directional keys:

```text
masterSecret = X25519(clientPrivate, serverPublic)
handshakeSalt = SHA256(clientNonce || serverNonce || sessionId)
clientToServerKey = HKDF(masterSecret, salt=handshakeSalt, info="synaptix:c2s:v1")
serverToClientKey = HKDF(masterSecret, salt=handshakeSalt, info="synaptix:s2c:v1")
```

### 3. Client sends encrypted request

Headers:

```http
Authorization: Bearer <jwt>
X-Syn-Sec-Session: <sessionId>
X-Syn-Sec-Seq: 12
X-Syn-Sec-Nonce: <base64url nonce>
X-Syn-Sec-Key-Version: v1
X-Syn-Sec-AAD: <base64url aad hash>
```

Body:

```json
{
  "ciphertext": "base64url",
  "tag": "base64url",
  "contentType": "application/json",
  "encryptedAtUtc": "2026-05-02T18:01:00Z"
}
```

AAD should bind:

```text
method || path || sessionId || sequence || jwtSubject || deviceId || timestamp
```

### 4. Server verifies and decrypts

Server checks:

1. JWT valid.
2. Session active and bound to subject/device.
3. Sequence number not reused.
4. Nonce not reused for the same key.
5. Timestamp inside allowed skew.
6. AEAD authentication tag valid.
7. Endpoint allows encrypted payload.

## Vault/KMS architecture

### Recommended Vault layout

```text
vault/
  transit/
    keys/
      synaptix-session-wrap
      synaptix-payload-wrap
      synaptix-refresh-token-wrap
      synaptix-admin-ops-wrap
      synaptix-data-protection-wrap
  kv/
    synaptix/dev/*
    synaptix/staging/*
    synaptix/prod/*
  auth/
    kubernetes/
    approle/
    jwt/
```

### Vault responsibilities

Vault should store or control:

- JWT signing keys or key references.
- Data Protection key wrapping key.
- Envelope encryption root keys.
- Admin ops key material.
- Payment provider secrets.
- Database credentials if dynamic secrets are later enabled.
- Audit logs of key operations.

Vault should **not** receive every high-frequency gameplay payload if performance is critical. Use Vault to wrap/unwrap data keys, then cache short-lived derived keys in backend memory with strict TTL.

### Envelope encryption model

For persisted sensitive data:

1. Backend asks Vault Transit for a data key.
2. Backend encrypts data locally with AES-256-GCM.
3. Backend stores ciphertext + encrypted data key + key version.
4. Backend discards plaintext data key.
5. On read, backend asks Vault to unwrap the encrypted data key.
6. Backend decrypts locally.

## Performance plan

### Do not encrypt everything twice

TLS already protects the network. App-layer encryption should be used for:

- Login credentials.
- Refresh tokens.
- Player identity payloads.
- Payments metadata.
- Admin actions.
- Anti-cheat evidence.
- Economy ledger adjustments.
- Private messages.
- Guardian/child account information.

It is not necessary for public catalog endpoints, public leaderboard reads, static assets, or non-sensitive telemetry.

### Suggested defaults

| Setting | Recommendation |
|---|---|
| Secure session TTL | 10–30 minutes |
| Session idle timeout | 5–10 minutes |
| Nonce length | 96-bit for AES-GCM, 192-bit for XChaCha20 |
| Sequence storage | Redis, keyed by session ID |
| Backend key cache TTL | 1–5 minutes |
| Vault root key rotation | 30–90 days initially |
| Emergency revocation | Immediate min-decryption-version bump where safe |

## Recommended implementation phases

### Phase A — Hardening first

- Enforce production failure when JWT secret is default or missing.
- Move all secrets to environment variables or Vault.
- Add Vault dev container to Docker Compose.
- Add `VaultOptions` to backend config.
- Protect ASP.NET Data Protection keys with Vault or at minimum persistent external storage.
- Remove placeholder API keys from frontend services.

### Phase B — Secure session service

- Add `Synaptix.Security.Core` project.
- Add `ISecureSessionService`.
- Add `ISecurePayloadProtector`.
- Add Redis-backed session replay store.
- Add `/security/sessions/start` and `/security/sessions/renew`.
- Add encrypted payload middleware or endpoint filter.

### Phase C — Flutter secure channel

- Add `SecureChannelService`.
- Add `SecureSessionStore` using `SecureStorage`.
- Add `EncryptedApiClient` wrapper around existing `HttpClient`.
- Use secure channel only on high-value endpoints first.
- Add automatic session renewal.

### Phase D — Vault Transit integration

- Add `Synaptix.Security.Vault` project.
- Add Vault Transit client.
- Add envelope encryption endpoints for backend use.
- Add key rotation job.
- Add audit dashboards.

### Phase E — Hybrid/PQ readiness

- Add protocol fields now.
- Keep `selectedSuite` versioned.
- Add server capability negotiation.
- Test ML-KEM in backend first.
- Use hybrid mode for internal service-to-service before enabling mobile clients.

## Final recommendation

Build a **versioned secure-channel architecture**, not a proprietary encryption algorithm. Start with TLS 1.3 + AES-256-GCM/XChaCha20-Poly1305 + Vault Transit + replay protection. Then evolve the handshake to hybrid X25519 + ML-KEM-768 when mobile library support is stable enough for your target platforms.

This gives Synaptix strong practical security today, a sane post-quantum migration path, and a reusable backend security layer that can protect other projects without locking you into fragile custom crypto.

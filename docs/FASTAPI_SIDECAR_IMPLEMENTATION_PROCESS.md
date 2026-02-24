# FASTAPI_SIDECAR_IMPLEMENTATION_PROCESS.md

## Purpose
This document provides a full, practical implementation process to stand up a **FastAPI sidecar** that integrates with your **existing .NET backend** while preserving the decisions in `BACKEND_DECISIONS.md`.

The sidecar’s role (recommended):
- Provide a Python-native surface for:
  - Admin tooling endpoints (if needed)
  - Event ingestion buffering/validation
  - Future AI/analytics workflows
- Delegate **authoritative authentication** and **token issuance** to .NET (source of truth)
- Keep contracts stable and explicitly aligned to your .NET auth model and roles

---

## 0) Guiding Principle: Source of Truth
**.NET remains the authority** for:
- Users, roles, statuses
- MFA enrollment + verification policy
- Token issuance (access + refresh) and refresh rotation rules
- Session revocation

FastAPI sidecar:
- Validates requests
- Applies light policy checks (optional)
- Forwards/mediates calls to .NET
- Performs async work (queueing, hashing, aggregation) when appropriate

---

## 1) Repo Layout (recommended)
Create a new folder (or repo) such as:
fastapi-sidecar/
app/
init.py
main.py
settings.py
http_client.py
auth_bridge.py
models/
init.py
contracts.py
routes/
init.py
auth.py
meta.py
events.py
config.py
tests/
pyproject.toml
README.md
.env.example


**Minimal start**: you can begin with a single file (`fastapi_contracts_stub.py`) and then split it into modules once behavior stabilizes.

---

## 2) Dependencies
### Baseline
- `fastapi`
- `uvicorn[standard]`
- `pydantic`
- `httpx` (for .NET calls)
- `python-dotenv` (optional)

### Add later
- `redis` (idempotency cache / dedupe window / rate limiting)
- `aiokafka` or `pika` (Kafka/Rabbit producers)
- `prometheus-client` or OpenTelemetry for observability

---

## 3) Environment Variables (.env)
Use a `.env` file for configuration:

APP_ENV=dev
DOTNET_BASE_URL=http://localhost:5000

DOTNET_TIMEOUT_SECONDS=10

Optional: if the sidecar needs to validate JWT signatures locally

JWT_ISSUER=...
JWT_AUDIENCE=...
JWT_JWKS_URL=...

Event ingest and dedupe

EVENT_MAX_BATCH=500
EVENT_DEDUPE_MODE=eventId


**Note:** If .NET issues JWTs and you want local verification in FastAPI, prefer JWKS validation (issuer publishes signing keys). Otherwise, treat JWT as opaque and forward to .NET for introspection on sensitive routes.

---

## 4) Aligning with .NET Backend (required alignment checklist)
Before writing logic beyond stubs, align these with your .NET backend:

### 4.1 Auth endpoints alignment
Decide whether .NET already has:
- `POST /auth/login` returning challenge vs tokens
- `POST /auth/mfa/verify`
- `POST /auth/refresh`
- `POST /auth/logout`

If yes:
- FastAPI sidecar should simply proxy these and enforce the **admin MFA always** rule (or let .NET enforce it fully).

If not:
- Implement these endpoints in .NET first (recommended), and keep FastAPI “thin”.

### 4.2 Token semantics alignment
Confirm .NET implements:
- Access TTL = 10 minutes
- Refresh TTL = 14 days
- Refresh rotates on every use
- Replay defense revokes session family

FastAPI should not invent semantics—just reflect them in DTOs and responses.

### 4.3 Canonical enums alignment
Ensure .NET returns exactly these strings:
- `AccountStatus`: ACTIVE, PENDING_VERIFICATION, SUSPENDED, BANNED, DELETED
- `UserRole`: USER, MODERATOR, ADMIN, SUPER_ADMIN
- `AgeGroup`: UNDER_13, AGE_13_17, AGE_18_24, AGE_25_34, AGE_35_44, AGE_45_PLUS, UNKNOWN

FastAPI may host `/meta/enums` from its own definitions (canonical copy), but the **source of truth** is still the .NET user payload.

---

## 5) Implementation Steps (recommended order)

### Step 1 — Bootstrap FastAPI with contract stubs
- Add `fastapi_contracts_stub.py` (from this chat) or split into modules.
- Run locally:
  - `uvicorn fastapi_contracts_stub:app --reload --port 8001`

Acceptance:
- Endpoints start and return schema responses
- OpenAPI docs reflect the contract shape

---

### Step 2 — Add a .NET bridge layer (httpx client)
Create `http_client.py`:
- A configured `httpx.AsyncClient`
- Base URL points to .NET backend

Create `auth_bridge.py`:
- `login(email, password, device_id)`
- `mfa_verify(challenge_id, otp_code)`
- `refresh(refresh_token, device_id)`
- `logout(session_id)`

Acceptance:
- FastAPI routes call bridge functions
- Bridge functions call .NET and pass through errors cleanly (status code + message)

---

### Step 3 — Replace stub token minting with .NET calls
In `/auth/login`:
- Call .NET to authenticate credentials and retrieve role and MFA requirement.
- If admin role:
  - Return `challenge_id` response
- Else:
  - Return tokens from .NET

In `/auth/mfa/verify`:
- Validate otp against .NET
- Return token pair from .NET

In `/auth/refresh`:
- Call .NET refresh
- Ensure rotation happens on .NET side

Acceptance:
- There is no “fake token” output in FastAPI

---

### Step 4 — Implement event ingestion pipeline
#### 4.1 Idempotency by eventId
Minimal viable approach:
- Use a durable store (Mongo or Postgres) with a **unique index on eventId**.
- Attempt insert:
  - success => enqueue for processing
  - duplicate => return `duplicate`

#### 4.2 Server hash (secondary)
Compute server-side `eventHash`:
- Normalize stable fields (recommended):
  - `user_id`, `eventType`, bucketed timestamp (optional), canonicalized payload subset
- `SHA-256` over canonical JSON

Store it for monitoring and abuse detection.

#### 4.3 Queue upload
- Push accepted events to your queue (Rabbit/Kafka/SQS)
- Consumers process events asynchronously and write aggregates/analytics

Acceptance:
- Retry of same batch does not duplicate queue inserts

---

### Step 5 — DDoS / abuse controls (event endpoint)
Add:
- Rate limiting by `user_id`, `device_id`, `ip` (Redis-based token bucket)
- Payload size + batch size guardrails
- Backpressure: return `429` with retry-after if queue lag is high

Acceptance:
- Flood attempts degrade gracefully without collapsing core services

---

### Step 6 — Hybrid config + preferences
Implement:
- `GET /config` returns:
  - `policy`: server authoritative (from .NET config service)
  - `featureFlags`: server authoritative
  - `notificationDefaults`: server defaults

Implement:
- `POST /user/preferences`:
  - save preferences server-side (prefer .NET user profile store)
  - apply policy overrides (server wins)

Acceptance:
- Client can cache and merge
- Policy cannot be overridden

---

## 6) Versioning and Compatibility
### Contract versioning
- Add a header or field:
  - `X-Contract-Version: 1`
- If you change response shapes, bump version.

### Config caching
- Use `etag` and `If-None-Match` for `/config`

---

## 7) Testing Strategy (minimal but effective)
- Unit tests:
  - Enum values exact-match
  - Request validation boundaries (batch max, otp length)
- Integration tests:
  - Spin .NET + FastAPI in docker-compose
  - Verify auth pass-through and event dedupe behavior

---

## 8) Operational / Deployment Notes
- Run sidecar behind the same gateway as .NET (Traefik/Nginx)
- Ensure consistent auth headers and CORS policy for admin portal
- Add structured logs (JSON) + trace ids that flow into .NET

---

## 9) “Definition of Done”
You are “up to date” when:
- FastAPI emits the final contract schema (OpenAPI) matching `BACKEND_DECISIONS.md`
- FastAPI proxies auth flows to .NET (no stub tokens)
- Refresh rotation and replay defense are enforced (in .NET)
- Events ingestion is idempotent by eventId and queues only newly accepted events
- `/config` and `/user/preferences` implement the hybrid precedence model

---

## Appendix: Next concrete files to add
If you want the next step fully scaffolded, generate these next:
- `app/settings.py` (env + typed settings)
- `app/http_client.py` (httpx client)
- `app/auth_bridge.py` (calls to .NET)
- `app/routes/*.py` (routes split)
- `docker-compose.yml` snippet to run sidecar + .NET + redis (optional)

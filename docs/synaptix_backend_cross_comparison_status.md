# Synaptix Backend Cross-Comparison Status
## Completed work, dependency map, and remaining work

**Purpose:** summarize the current backend status, split into work that is independent of the frontend vs. work that depends on frontend readiness or cross-stack verification.

---

## 1. Snapshot

The backend rebrand and operator-facing language alignment are far along, but the gameplay-support backend still has a major gap compared with the frontend product layer.

What is clearly completed:
- backend packet A
- backend packet B
- backend packet C
- backend packet D
- operator-facing Synaptix Command branding
- Synaptix API branding
- additive preference persistence
- analytics dimension expansion
- several operational/backend hardening items unrelated to the rebrand

What is still incomplete:
- alpha gameplay backend called out in the FastAPI backend plan
- full economy sync
- crypto economy layer
- build/migration verification in a .NET environment
- final frontend/backend vocabulary verification

---

## 2. Backend completed work that does **not** depend on frontend changes

These items are backend-owned and can be considered independent progress.

### 2.1 Packet A — backend brand surface reframe
Completed:
- Swagger/OpenAPI branding moved to Synaptix API
- operator dashboard titles updated
- backend product-facing docs updated
- backend surface inventory created
- risk register / deferred rename list established

### 2.2 Packet C — backend product-language alignment
Completed:
- Blazor operator dashboard aligned to Synaptix Command
- Vue operator dashboard aligned
- web/react dashboard aligned
- backend-facing docs aligned
- visible terminology updated for operator surfaces

### 2.3 Packet D — analytics/admin terminology alignment
Completed:
- analytics model dimensions extended
- event ingestion supports Synaptix-facing dimensions
- operator dashboards use updated display terminology
- visible old branding removed from operator-visible UI

### 2.4 Additional operational/backend work outside the main packet system
Completed from changelog:
- sidecar gRPC wiring improvements
- mobile leaderboard/match streaming improvements
- health pass automation script
- admin question query hardening
- durable sidecar inference storage path in compose
- MinIO object-storage abstraction and setup
- vote feature domain/API additions
- test coverage expansions in several backend domains

These are meaningful backend completions, even if they are not all directly part of the Synaptix rebrand.

---

## 3. Backend completed work that **does** depend on frontend needs and is already aligned

These backend items exist specifically because the frontend needed or expected them.

### 3.1 Preference persistence for mode/theme system
Completed:
- `PlayerPreferences` support
- `GET /users/me/preferences`
- `PUT /users/me/preferences`

This directly supports the frontend’s Synaptix mode and surface preference system.

### 3.2 Analytics dimensions required by frontend instrumentation
Completed:
- `synaptix_mode`
- `surface`
- `audience_segment`
- `entry_point`
- `brand_version`

This directly supports the frontend’s Packet D instrumentation plan.

### 3.3 Operator/backend terminology intended to match frontend surfaces
Completed:
- Arena / Labs / Pathways / Command-facing language in backend-visible docs and dashboards
- currency label alignment in backend-visible dashboards

This work only has full value once verified against the live frontend wording, but the backend side is already done.

---

## 4. Backend completed work that is **implemented**, but still needs runtime validation or cross-stack verification

### 4.1 Build and migration verification
Still needs real environment verification:
- `dotnet build`
- EF migration generation for `PlayerPreferences`
- migration run against a dev database
- namespace/build regression confirmation

### 4.2 Cross-layer terminology verification
Still needs final confirmation:
- frontend labels match backend dashboards/docs
- operator surfaces use the same vocabulary as the app

### 4.3 Dashboard and telemetry verification in full runtime
Docs say dashboards/branding are complete, but final release confidence still depends on:
- live dashboard startup
- telemetry continuity
- no hidden old-brand strings outside audited surfaces

---

## 5. Remaining backend work with **no direct frontend dependency**

These are backend-owned tasks that can proceed without waiting on frontend implementation.

### 5.1 Build environment verification
Still open:
- full solution build verification
- migration generation and application
- CI confirmation of no namespace/build regressions

### 5.2 Deferred Packet E technical cleanup
Still open and intentionally deferred:
- `Tycoon.Backend.*` -> `Synaptix.Backend.*`
- project/solution rename
- service/telemetry identifier rename
- docker / CI / issuer / audience naming cleanup

### 5.3 Ongoing backend platform hardening
Potential continued work:
- more tests
- more admin/operator coverage
- more sidecar / gRPC hardening
- production deployment polish

These are backend concerns regardless of frontend progress.

---

## 6. Remaining backend work that **does depend** on frontend product needs

This is the major unfinished area.

### 6.1 Alpha gameplay backend from the FastAPI backend plan
The backend plan still marks major gameplay-support APIs as needing implementation.

Critical remaining backend items include:
- auth implementation for the FastAPI alpha backend path
- profile sync with Synaptix-specific fields
- quiz question bank and quiz submit scoring
- leaderboard endpoints
- economy state endpoints
- achievements endpoints
- store endpoints

These directly support the frontend’s real player journey.

### 6.2 Authoritative economy synchronization
Still open:
- remote wallet state
- economy session start
- reward reconciliation
- purchase settlement

The frontend currently has local wallet persistence, but the backend needs to become authoritative for real production behavior.

### 6.3 Crypto economy layer
Still open:
- crypto ledger
- wallet linking
- crypto balance/history
- withdrawal flow
- prize pool system
- optional staking later

This is entirely backend-led and is a hard dependency before the frontend can expose a real crypto economy.

### 6.4 Full live feature APIs
Still open or post-alpha in the backend plan:
- multiplayer and matchmaking
- seasons and tier APIs
- skills/pathways backend API
- friends/social APIs
- live WebSocket coverage
- advanced admin/auth features
- guardians / territory / extended systems where needed

### 6.5 Final cross-stack release validation
Still open:
- verify frontend actually sends/reads preference fields correctly
- verify analytics payloads match ingestion expectations
- verify frontend labels and backend labels stay consistent under live runtime

---

## 7. Practical backend priority order from here

### Highest-value remaining backend items
1. build + migration verification in a real .NET environment
2. core alpha gameplay backend from the FastAPI plan:
   - auth
   - profile sync
   - quiz
   - leaderboard
   - economy state
   - store
3. authoritative wallet/economy sync
4. cross-stack terminology verification with the live frontend
5. crypto economy layer
6. multiplayer / seasons / skills / social deeper APIs
7. only then reconsider Packet E

---

## 8. Final backend assessment

### Completed and mostly independent
The backend rebrand, dashboard alignment, and analytics-dimension work are largely complete.

### Completed because frontend needed it
Preferences persistence and analytics-dimension support are already in place.

### Still open
The biggest unfinished backend work is not the Synaptix rename itself — it is the actual gameplay/economy service layer needed to support the frontend beyond local/demo behavior.

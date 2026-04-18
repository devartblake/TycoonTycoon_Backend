# Question Flow Compatibility Architecture Handoff

**Date:** 2026-04-15  
**Status Updated:** 2026-04-18  
**Audience:** Frontend Team, Backend/API Team, Platform Team  
**Purpose:** Define the supported architecture and contract boundary for question flows after the option 2 migration decision. The backend no longer exposes `/quiz/*`; gameplay uses `/questions/*`, guided training uses `/modules/*`, and future rehearsal work should use a dedicated Study surface rather than reviving legacy quiz endpoints.

> **Primary training handoff:** Use [LEARNING_MODULES_API_HANDOFF.md](/c:/Users/lmxbl/Documents/TycoonTycoon_Backend/docs/LEARNING_MODULES_API_HANDOFF.md) for guided study/training flows.
> **Primary Study handoff:** Use [study_frontend_backend_handoff_2026-04-18.md](/C:/Users/lmxbl/Documents/TycoonTycoon_Backend/docs/study_frontend_backend_handoff_2026-04-18.md) for flashcards, self-test, favorites, custom sets, and due-review flows.

---

## 1. Purpose and Contract Status

This document is the source of truth for how question-related responsibilities are split across the `.NET API`, `Tycoon.Sidecar`, and mobile gRPC surfaces after backend `/quiz/*` retirement.

Core decisions:

- **API remains the only public contract boundary for gameplay question retrieval and grading**
- **`Tycoon.Sidecar` remains an internal capability/orchestration service behind the API**
- **mobile gRPC remains limited to live match/session behavior**
- **`/quiz/*` is not a supported backend contract surface**

### Route Status Matrix

| Surface | Route / Capability | Status | Notes |
|---|---|---|---|
| REST/API | `GET /questions/set` | `canonical` | Supported gameplay question retrieval route |
| REST/API | `POST /questions/check` | `canonical` | Supported single-answer grading route |
| REST/API | `POST /questions/check-batch` | `canonical` | Supported batch grading route |
| REST/API | `GET /modules` | `canonical` | Supported learning module listing route |
| REST/API | `GET /modules/{id}` | `canonical` | Supported learning module detail route |
| REST/API | `GET /modules/{id}/lessons` | `canonical` | Supported ordered lesson content route |
| REST/API | `POST /modules/{id}/complete` | `canonical` | Supported module completion/reward route |
| REST/API | `/study-sets/*` | `canonical` | Supported rehearsal/study-set discovery, favorites, custom sets, and due-review routes |
| REST/API | `/study-sessions/*` | `canonical` | Supported resumable flashcard/self-test session routes |
| REST/API | `/quiz/*` backend routes | `retired` | Not mapped in the backend API; do not assume legacy compatibility exists |
| REST/API | question discovery/stats routes beyond `/questions/set` | `implemented` | `GET /questions/categories`, `GET /questions/metadata`, and `POST /questions/preview-set` are available |
| `Tycoon.Sidecar` | enrichment, inference, curation helpers | `planned` | Internal-only behind API if and when profiling justifies offload |
| mobile gRPC | live match/session streaming | `canonical` | Correct place for low-latency gameplay flows |
| mobile gRPC | repository/discovery/training flows | `unsupported` | Do not move question discovery or module browsing here in this phase |

---

## 2. Ownership Map

### API ownership

The `.NET API` owns:

- public gameplay question route surface
- public learning module route surface
- auth and authorization
- error envelopes
- request/response contract stability
- validation of any sidecar-produced data before it is returned to clients

The API does **not** currently own a `/quiz/*` compatibility family.

### `Tycoon.Sidecar` ownership

`Tycoon.Sidecar` owns internal support capabilities such as:

- difficulty estimation
- recommendation/inference helpers
- orchestration-heavy or compute-heavy support logic
- internal utility processing

`Tycoon.Sidecar` does **not** own:

- public question or learning contracts
- frontend-facing compatibility policy
- repository/discovery APIs

### Mobile gRPC ownership

Mobile gRPC owns:

- live match start/join/play behavior
- live answer submission in active sessions
- session-oriented streaming

Mobile gRPC does **not** own:

- general question repository retrieval
- category browsing
- dataset metadata
- learning module browsing

---

## 3. Supported Frontend Surfaces

### Canonical gameplay REST routes

#### `GET /questions/set`

**Status:** `canonical`

Use for:

- gameplay-safe question retrieval
- category/difficulty/count-based question loading for play flows

Frontend guidance:

- treat this as the default backend retrieval path for play-oriented flows
- do not expect learning fields such as correct-answer exposure here
- do not assume old quiz route compatibility envelopes

#### `POST /questions/check`

**Status:** `canonical`

Use for:

- single-answer grading in gameplay

Frontend guidance:

- use option-ID-based grading as canonical

#### `POST /questions/check-batch`

**Status:** `canonical`

Use for:

- end-of-round / end-of-session grading reconciliation

Frontend guidance:

- use this as the canonical backend batch grading surface for play

### Canonical learning REST routes

#### `GET /modules`
#### `GET /modules/{id}`
#### `GET /modules/{id}/lessons`
#### `POST /modules/{id}/complete`

**Status:** `canonical`

Use for:

- guided learning
- lesson progression
- educational explanation exposure
- module completion rewards

Frontend guidance:

- training-oriented user flows should use learning modules, not retired quiz endpoints

### Unsupported/retired backend routes

The following backend route family is retired:

- `/quiz/*`

That means:

- do not treat `/quiz/play` as a backend fallback
- do not treat `/quiz/daily` or `/quiz/mixed` as compatibility retrieval aliases
- do not treat `/quiz/categories`, `/quiz/stats`, or class/dataset quiz routes as implemented backend surfaces

If the frontend still has local fallback behavior or old route names, that behavior is frontend migration debt rather than backend contract support.

---

## 4. Migration Sequence

### Phase 1: Truth and freeze

1. Freeze the canonical backend contracts:
   - `GET /questions/set`
   - `POST /questions/check`
   - `POST /questions/check-batch`
   - `/modules/*` learning routes
2. Mark `/quiz/*` as retired from the backend API.
3. Keep training guidance anchored to learning modules.

### Phase 2: Frontend cleanup

1. Remove or deprecate direct frontend `/quiz/*` transport calls.
2. Remove fallback logic that assumes backend quiz endpoints exist.
3. Align route naming and product language toward Play / Learn / Study.

### Phase 3: Internal offload

1. Profile gameplay question flows.
2. Move only worthwhile enrichment/curation work behind the API into `Tycoon.Sidecar`.
3. Keep the API responsible for public envelopes and public contract stability.

### Phase 4: Future expansion

1. If broader discovery/stats are needed, introduce them through a deliberate canonical surface.
2. If rehearsal/flashcard/self-test work is needed, introduce a dedicated Study route family rather than reviving `/quiz/*`.

---

## 5. Frontend Guidance

### What the frontend can rely on immediately

- `GET /questions/set`
- `POST /questions/check`
- `POST /questions/check-batch`
- `/modules/*` for guided learning
- mobile gRPC only for live match/session flows

### What is no longer supported by backend

- backend `/quiz/*` retrieval
- backend `/quiz/*` stats/discovery
- backend `/quiz/*` daily/mixed aliases

### What should remain local fallback only for now

- any frontend logic that still depends on retired quiz routes
- any old category/class/dataset assumptions not backed by a current canonical API route

### Logging and QA guidance

Frontend should distinguish:

- `backendCanonical`
- `localFallback`

If a frontend path still uses a local fallback because a retired quiz route no longer exists, that should be visible in logs and QA traces rather than mistaken for a healthy backend-backed flow.

---

## 6. Repo Coordination Guidance

### Recommended model

Use a **monorepo with separate apps**:

- Flutter frontend remains separate
- `.NET API` remains separate
- `Tycoon.Sidecar` remains separate
- contract/tooling/docs stay coordinated in one repo

### Why not merge everything into one app

A merged application would not solve the real problem here, which is contract discipline and stale assumptions. It would instead:

- increase coupling
- blur ownership
- make route drift easier to hide

### Better coordination model

Invest in:

- shared handoff docs
- contract tests
- issue/status tracking
- explicit deprecation tracking

---

## Test Plan

The architecture direction should be backed by:

- contract tests for `GET /questions/set`, `POST /questions/check`, and `POST /questions/check-batch`
- contract tests for `/modules/*` learning routes
- negative tests proving representative retired `/quiz/*` routes are absent
- future integration tests for API-to-sidecar enrichment paths if introduced
- continued mobile gRPC tests focused on live session behavior only

---

## Assumptions and Defaults

- Default public gameplay edge: **REST/API via `/questions/*`**
- Default learning/training edge: **REST/API via `/modules/*`**
- Default sidecar role: **internal helper**
- Default mobile gRPC role: **live session streaming only**
- Default backend `/quiz/*` status: **retired**

---

## Immediate Next Actions

### Backend/API team

- keep `/questions/*` and `/modules/*` stable
- continue removing stale docs that imply `/quiz/*` is still live
- do not reintroduce backend quiz compatibility shims without a new explicit architecture decision

### Frontend team

- remove direct `/quiz/*` API assumptions
- migrate gameplay retrieval fully onto `/questions/*`
- keep guided training on `/modules/*`
- reserve future rehearsal work for a dedicated Study surface

### Platform team

- keep `Tycoon.Sidecar` behind the API
- keep mobile gRPC focused on live session performance paths
- prefer explicit contract tooling over implicit compatibility

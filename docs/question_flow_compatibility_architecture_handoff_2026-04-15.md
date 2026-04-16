# Question Flow Compatibility Architecture Handoff

**Date:** 2026-04-15  
**Audience:** Frontend Team, Backend/API Team, Platform Team  
**Purpose:** Define the supported architecture and contract boundary for question flows so the frontend can integrate against stable question APIs without depending on undocumented fallback behavior.

> **Status update:** Training-oriented frontend work is now pivoting to learning modules. Use [LEARNING_MODULES_API_HANDOFF.md](/c:/Users/lmxbl/Documents/TycoonTycoon_Backend/docs/LEARNING_MODULES_API_HANDOFF.md) as the primary frontend handoff for guided study/training flows. This document remains relevant only for competitive `/questions/*` gameplay endpoints.

---

## 1. Purpose and Contract Status

This document is the source of truth for how question flow responsibilities are split across the `.NET API`, `Tycoon.Sidecar`, and mobile gRPC surfaces.

The key architecture decision is:

- **API remains the only public contract boundary for question retrieval, grading, auth, and compatibility behavior**
- **`Tycoon.Sidecar` remains an internal inference/orchestration service behind the API**
- **mobile gRPC remains limited to live match/session behaviors, not general question discovery**

The recommended repository shape is:

- **monorepo with separate apps**

This means:

- keep Flutter frontend, `.NET API`, and `Tycoon.Sidecar` in one source-of-truth repo
- keep them as separate runtime/application boundaries
- do **not** merge frontend and backend into one combined application

Why this is the recommended model:

- it preserves shared visibility and contract coordination
- it avoids turning every frontend/backend change into a single tightly coupled release
- it keeps service ownership clear
- it allows shared contract tooling without collapsing architectural boundaries

### Route Status Matrix

| Surface | Route / Capability | Status | Notes |
|---|---|---|---|
| REST/API | `GET /questions/set` | `canonical` | Current supported question retrieval route |
| REST/API | `POST /questions/check` | `canonical` | Current supported single-answer grading route |
| REST/API | `POST /questions/check-batch` | `canonical` | Current supported batch grading route |
| REST/API | `/quiz/play` question route | `unsupported` | Not implemented today; do not assume legacy play fallback exists |
| REST/API | `GET /quiz/daily` | `compatibility` | Implemented as a compatibility retrieval alias with `items/questions/data + meta` |
| REST/API | `GET /quiz/mixed` | `compatibility` | Implemented as a compatibility retrieval alias with `items/questions/data + meta` |
| REST/API | `GET /questions/mixed` | `compatibility` | Implemented as a compatibility retrieval alias with `items/questions/data + meta` |
| REST/API | `GET /quiz/categories` | `compatibility` | Implemented as a compatibility discovery surface with `items/categories/data` |
| REST/API | `GET /quiz/stats` and `GET /questions/stats` | `compatibility` | Implemented as lightweight global question stats |
| REST/API | `GET /quiz/categories/{slug}/stats` and `GET /questions/categories/{slug}/stats` | `compatibility` | Implemented as lightweight category stats |
| REST/API | `GET /quiz/datasets/info` and `GET /questions/datasets/info` | `compatibility` | Implemented as lightweight dataset metadata |
| REST/API | `/quiz/classes/{classId}/stats`, `/questions/classes/{classId}/stats` | `unsupported` | Frontend must not assume class stats exist today |
| API compatibility | Alternate grading request/response shims for frontend stability | `compatibility` | Text answer aliases are accepted in grading while option-ID remains canonical |
| `Tycoon.Sidecar` | Mixed/daily curation helpers, normalization, inference | `planned` | Internal-only behind API if needed |
| mobile gRPC | Live match/session streaming | `canonical` | Already the correct place for low-latency gameplay streaming |
| mobile gRPC | Category browsing, daily discovery, dataset metadata | `unsupported` | Do not move repository/discovery flows here in this phase |

---

## 2. Ownership Map

### API ownership

The `.NET API` owns:

- public question route surface
- auth and authorization
- public error envelopes
- request/response contract stability
- compatibility shims for the frontend
- validation of any sidecar-produced data before it is returned to clients

This means the frontend should treat the API markdown contract as authoritative, not fallback parser behavior or internal DTO assumptions.

### `Tycoon.Sidecar` ownership

`Tycoon.Sidecar` owns internal-only support capabilities such as:

- difficulty estimation
- recommendation/inference helpers
- orchestration-heavy or compute-heavy question support
- utilities and analytics-driven automation

`Tycoon.Sidecar` does **not** own:

- public question contracts
- frontend-facing route stability
- mobile-facing discovery APIs
- direct public compatibility policy

### Mobile gRPC ownership

Mobile gRPC owns:

- match start/join/play flows
- live answer submission in active sessions
- leaderboard/watch streams

Mobile gRPC does **not** own:

- question repository-style retrieval
- category browsing
- class stats
- daily discovery
- dataset metadata

---

## 3. Supported Frontend Question Surfaces

This section defines what the frontend can rely on immediately.

### Canonical REST question routes

#### `GET /questions/set`

**Status:** `canonical`

**Purpose**

- serve gameplay-safe question sets
- return approved questions only
- withhold the correct answer from clients

**Current request shape**

- query: `category`
- query: `difficulty`
- query: `count`

**Current response shape**

```json
{
  "questions": [
    {
      "id": "guid",
      "text": "What is the speed of light?",
      "category": "Science",
      "difficulty": 2,
      "options": [
        { "id": "A", "text": "300,000 km/s" },
        { "id": "B", "text": "150,000 km/s" }
      ],
      "mediaKey": null
    }
  ],
  "count": 10
}
```

**Frontend guidance**

- use this route as the default backend question retrieval path
- do not treat top-level arrays or `items` envelopes as canonical for this route
- do not assume `correctAnswer`, `answers[].isCorrect`, or quiz-play route reconstruction behavior here

#### `POST /questions/check`

**Status:** `canonical`

**Purpose**

- grade a single answer server-side

**Current request shape**

```json
{
  "questionId": "guid",
  "selectedOptionId": "A"
}
```

**Current response shape**

```json
{
  "questionId": "guid",
  "selectedOptionId": "A",
  "correctOptionId": "A",
  "isCorrect": true
}
```

**Frontend guidance**

- treat option-ID-based grading as the canonical contract
- treat any text-based grading behavior as compatibility-only if introduced later

#### `POST /questions/check-batch`

**Status:** `canonical`

**Purpose**

- grade a full round or quiz batch server-side

**Current request shape**

```json
{
  "answers": [
    { "questionId": "guid-1", "selectedOptionId": "A" },
    { "questionId": "guid-2", "selectedOptionId": "C" }
  ]
}
```

**Current response shape**

```json
{
  "results": [
    {
      "questionId": "guid-1",
      "selectedOptionId": "A",
      "correctOptionId": "A",
      "isCorrect": true
    }
  ],
  "total": 2,
  "correct": 1
}
```

**Frontend guidance**

- use this route as the canonical end-of-quiz reconciliation path
- do not assume alternate collection keys unless the API explicitly adds them as temporary compatibility support

### Frontend-assumed routes that are not implemented today

The following are currently **unsupported** unless the API explicitly adds them later:

- `GET /quiz/play`
- `GET /quiz/classes/{classId}/stats`
- `GET /questions/classes/{classId}/stats`

Current disposition for these surfaces:

- **stay frontend fallback only** until explicitly implemented in API
- **do not** move them into sidecar as public endpoints
- **do not** move them into mobile gRPC as discovery substitutes

### Compatibility routes implemented in this phase

The following compatibility routes are now available in the API:

- `GET /quiz/daily`
- `GET /quiz/mixed`
- `GET /questions/mixed`
- `GET /quiz/categories`
- `GET /quiz/stats`
- `GET /questions/stats`
- `GET /quiz/categories/{slug}/stats`
- `GET /questions/categories/{slug}/stats`
- `GET /quiz/datasets/info`
- `GET /questions/datasets/info`

These routes return a compatibility collection envelope:

```json
{
  "items": [ ... ],
  "questions": [ ... ],
  "data": [ ... ],
  "meta": {
    "source": "backend",
    "count": 10
  }
}
```

These are compatibility surfaces, not the long-term canonical question contract.

For discovery/object-style compatibility routes, the API now returns lightweight frontend-friendly objects:

- `/quiz/categories` with `items`, `categories`, `data`
- `/quiz/stats` and `/questions/stats` with `totalQuestions`, `questionCount`, `total`, category counts, and `source`
- `/quiz/categories/{slug}/stats` and `/questions/categories/{slug}/stats` with `questionCount`, `totalQuestions`, `total`, `difficulty`, and `source`
- `/quiz/datasets/info` and `/questions/datasets/info` with `name`, `datasetName`, `version`, question totals, and `source`

### Planned compatibility-only additions

If compatibility shims are added, they should be:

- additive
- explicitly documented as temporary
- implemented in API only

Good examples:

- accepting alternate grading field names temporarily
- adding optional metadata fields without changing canonical fields
- adding a small alias route that maps to canonical question retrieval if that reduces immediate frontend churn

Implemented in this phase:

- `POST /questions/check` accepts `selectedOptionId` as canonical and also tolerates `selectedAnswer` / `answer`
- `POST /questions/check-batch` accepts the same per-answer compatibility fields
- grading responses now include `correctAnswer` and `source` in addition to canonical option-ID fields

Bad examples:

- reshaping internal domain models purely to match fallback parser habits
- exposing sidecar-native schemas directly to the frontend
- introducing broad duplicate route families without deprecation intent

---

## 4. Migration Sequence

### Phase 1: Truth and freeze

1. Freeze and document the current canonical question REST contract:
   - `GET /questions/set`
   - `POST /questions/check`
   - `POST /questions/check-batch`
2. Publish this architecture handoff as the frontend-facing source of truth.
3. Mark all unsupported `/quiz/*`, stats, mixed, daily, and dataset surfaces clearly as not implemented.
4. Keep the frontend backend-first, but make local fallback behavior visibly distinct from canonical backend integration.

### Phase 2: Safe compatibility

1. Update frontend code to prefer only the documented canonical routes.
2. Add only the smallest API-layer compatibility shims needed to reduce immediate breakage.
3. Keep those shims temporary and documented.
4. Do not move compatibility work into `Tycoon.Sidecar`.
5. Do not use mobile gRPC as a substitute for missing REST discovery routes.

### Phase 3: Internal offload

1. Profile the API question path.
2. If mixed/daily generation or enrichment is expensive, move only that logic behind the API into `Tycoon.Sidecar`.
3. Keep API responsible for:
   - response validation
   - public envelope shaping
   - auth and compatibility behavior
4. Keep sidecar outputs internal and non-authoritative until validated by the API.

### Phase 4: Simplification

1. Deprecate temporary compatibility aliases once frontend adoption is stable.
2. Remove frontend fallback branches tied to routes that remain unsupported.
3. Reassess whether any new mobile-specific question gRPC surface is justified.
4. Only introduce question-focused gRPC if there is a deliberate move to a live-session-first, typed mobile question protocol.

---

## 5. Frontend Guidance

### What the frontend can rely on immediately

- `GET /questions/set`
- `POST /questions/check`
- `POST /questions/check-batch`
- mobile gRPC for live match/session flows only

### What is temporary compatibility

Temporary compatibility means:

- non-canonical request aliases
- non-canonical response aliases
- short-lived API shims added only to support frontend migration

Temporary compatibility should:

- be explicitly labeled in docs
- include a removal/deprecation note when introduced
- never become the default contract silently

### What should remain local fallback for now

Until implemented in API, the frontend should continue to treat these as local-fallback-only concerns:

- class stats/category assumptions
- any parser assumptions based on legacy `/quiz/*` routes that do not exist in the backend

### Logging and banner guidance

Frontend should distinguish at least these states:

- `backendCanonical`
- `backendCompatibility`
- `localFallback`

Recommended UX/log behavior:

- `backendCanonical`: normal operation, no banner needed
- `backendCompatibility`: optional debug log or QA-visible marker
- `localFallback`: visible banner and explicit log marker

This avoids the situation where the app “still works” but is no longer exercising the intended backend contract.

---

## 6. Repo Coordination Guidance

### Recommended model

Use a **monorepo with separate apps**:

- Flutter frontend remains its own app/project
- `.NET API` remains its own app/project
- `Tycoon.Sidecar` remains its own app/project
- proto files, shared docs, and contract tooling stay in the same repository

### Why not merge everything into one project

A single merged application is **not recommended** right now because it would:

- increase coupling between frontend and backend release cycles
- make service ownership less clear
- make it easier for undocumented assumptions to leak across boundaries
- not solve the actual contract-discipline problem
- make future scaling or sidecar specialization harder

### Better alternative to a full merge

Instead of merging apps, invest in shared contract governance:

- shared ownership of REST/OpenAPI/proto contract files
- a compatibility matrix in `docs`
- contract tests in CI
- generated client artifacts where useful
- explicit deprecation tracking for temporary aliases

### Governance additions recommended

- maintain one frontend-facing handoff doc per major domain
- add route inventory checks in CI where practical
- add contract tests for canonical and compatibility routes
- keep sidecar and mobile gRPC docs explicit about what they do **not** own

---

## Test Plan

The following validation work should accompany this architecture direction:

- contract tests for `GET /questions/set`, `POST /questions/check`, and `POST /questions/check-batch`
- contract tests for any future compatibility alias routes added in API
- negative tests proving unsupported frontend-assumed routes are absent unless intentionally implemented
- integration tests for any future API-to-sidecar question enrichment paths
- regression tests proving sidecar-native schemas do not leak to the frontend directly
- continued gRPC tests for live mobile match/session behavior only

If route aliases are added later, CI should validate:

- the alias maps to the intended canonical behavior
- the alias is labeled as compatibility-only in docs
- the alias does not widen the public contract unintentionally

---

## Assumptions and Defaults

- Default architecture: **monorepo with separate apps**
- Default public edge: **REST/API**
- Default sidecar role: **internal inference/orchestration helper**
- Default mobile gRPC role: **live mobile gameplay/session streaming only**
- Default frontend guidance style: prefer explicit supported/unsupported route status over optimistic fallback assumptions
- Default grading direction: **option-ID-based validation** remains canonical unless a future answer-ID contract is introduced deliberately

---

## Immediate Next Actions

### Backend/API team

- keep the three canonical question routes stable
- document any new compatibility alias before exposing it
- avoid expanding sidecar or gRPC into public question discovery without an explicit contract decision

### Frontend team

- align repository-backed question flows to the canonical question REST routes first
- treat unsupported routes as fallback-only, not assumed backend capabilities
- use compatibility mode only when explicitly documented by API

### Platform team

- keep `Tycoon.Sidecar` behind the API for question-related inference/offload work
- keep mobile gRPC focused on live session performance paths
- prefer contract tooling and CI over application merging

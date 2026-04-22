# Question Flow Frontend/Backend Handoff

**Date:** 2026-04-15  
**Status Updated:** 2026-04-18  
**Audience:** Backend / Platform Team / Frontend Team  
**Purpose:** Align the current Flutter question flow with the backend contracts after the option 2 migration decision: `/quiz/*` is retired from the backend API, `/questions/*` is the gameplay contract, and learning/training should use learning modules instead of legacy quiz-style endpoints.

---

## Status Update

This handoff previously documented a transition period where the frontend still expected several `/quiz/*` backend endpoints. That is no longer the supported architecture.

Current backend contract reality:

- `/questions/*` is the supported gameplay question API
- `/modules/*` is the supported guided learning API
- `/quiz/*` is **not** mapped in the backend API

Use [LEARNING_MODULES_API_HANDOFF.md](/c:/Users/lmxbl/Documents/TycoonTycoon_Backend/docs/LEARNING_MODULES_API_HANDOFF.md) as the source of truth for guided learning/module flows.
Use [study_frontend_backend_handoff_2026-04-18.md](/C:/Users/lmxbl/Documents/TycoonTycoon_Backend/docs/study_frontend_backend_handoff_2026-04-18.md) as the source of truth for the dedicated Study surface.

---

## Executive Summary

The frontend question system is organized around:

- `QuestionRepository`
- `QuestionRepositoryImpl`
- `QuestionHubService`

This stack is used by:

- question/play hub behavior
- category/class/daily play entry flow
- gameplay question loading
- per-answer validation
- end-of-round batch reconciliation

The frontend may still support **local fallback**, but the backend team should not treat that fallback behavior as the public contract. The contract boundary for gameplay is now the canonical `/questions/*` surface only.

---

## Canonical Backend Surfaces

### Gameplay questions

The supported gameplay routes are:

- `GET /questions/set`
- `GET /questions/categories`
- `GET /questions/metadata`
- `POST /questions/preview-set`
- `POST /questions/check`
- `POST /questions/check-batch`

These routes are the public backend contract for play-oriented question retrieval and grading.

### Guided learning

Training/study modules are handled separately through:

- `GET /modules`
- `GET /modules/{id}`
- `GET /modules/{id}/lessons`
- `POST /modules/{id}/complete`

These routes intentionally support learning-specific behavior such as correct-answer exposure inside lesson content.

### Dedicated Study surface

Rehearsal and self-paced review are now handled separately through:

- `GET /study-sets`
- `GET /study-sets/{id}`
- `GET /study-sets/recommended`
- `POST /study-sets`
- `PATCH /study-sets/{id}`
- `POST /study-sets/favorites/{questionId}`
- `DELETE /study-sets/favorites/{questionId}`
- `POST /study-sessions`
- `POST /study-sessions/{id}/progress`
- `GET /study-sessions/{id}/summary`

These routes are the supported backend path for Study/Quizlet-like behavior. They should not be conflated with competitive gameplay or guided learning modules.

### Retired backend routes

The following backend route family is retired and should not be assumed by frontend code or docs:

- `/quiz/*`

That includes old assumptions such as:

- `GET /quiz/play`
- `GET /quiz/daily`
- `GET /quiz/mixed`
- `GET /quiz/categories`
- `GET /quiz/stats`
- `GET /quiz/categories/{slug}/stats`
- `GET /quiz/classes/{classId}/stats`
- `GET /quiz/datasets/info`

If any frontend code still references these routes, treat that as migration cleanup work, not as a missing backend implementation request.

---

## Frontend Flow Map

### 1. Play question loading

Used by:

- question/play hub
- category play flow
- class play flow
- daily play flow
- multiplayer warmup/prefetch where relevant

Backend dependency:

- `GET /questions/set`

Frontend should pass category, difficulty, and count through the canonical gameplay repository/service path rather than attempting legacy quiz route reconstruction.

### 2. Answer validation

Used by:

- gameplay question screen
- play session state/notifier logic

Backend dependencies:

- `POST /questions/check`
- `POST /questions/check-batch`

Canonical grading direction:

- option-ID-based validation for gameplay

### 3. Guided learning/training

Used by:

- learn hub
- module detail
- lesson flow
- module completion/reward flow

Backend dependencies:

- `GET /modules`
- `GET /modules/{id}`
- `GET /modules/{id}/lessons`
- `POST /modules/{id}/complete`

This is the supported backend path for training users based on questions.

---

## Current Frontend Expectations the Backend Team Should Know

### Gameplay retrieval expectations

For `GET /questions/set`, the frontend should align to the actual gameplay DTO contract instead of legacy quiz-parser assumptions.

Backend-supported gameplay fields are conceptually:

- `id`
- `text`
- `category`
- `difficulty`
- `options`
- optional `mediaKey`

Backend-supported gameplay discovery fields are conceptually:

- categories with counts via `GET /questions/categories`
- categories + supported difficulties via `GET /questions/metadata`
- answer-safe filtered previews via `POST /questions/preview-set`

The backend does **not** expose:

- `correctAnswer`
- `answers[].isCorrect`
- learning-style explanation data

for gameplay retrieval.

### Gameplay validation expectations

For grading routes, the frontend should align to canonical option-ID contracts:

- request uses `selectedOptionId`
- response uses `correctOptionId`
- response includes `isCorrect`

Text-answer validation should be treated as local fallback or legacy frontend debt unless explicitly reintroduced as a temporary compatibility shim.

### Learning expectations

For modules/lessons, the learning contract may expose:

- ordered lesson context
- `correctOptionId`
- `explanation`

That difference is intentional and should not be treated as gameplay DTO drift.

---

## Updated Route-to-API Alignment

### Play-oriented routes/screens

Frontend play flows should ultimately align to:

- question/play hub UI
- category play UI
- class play UI
- daily play UI
- multiplayer play warmup/session launch

Backend dependency for those flows:

- `GET /questions/set`
- `POST /questions/check`
- `POST /questions/check-batch`

### Learn-oriented routes/screens

Frontend learning flows should align to:

- `/learn-hub`
- module detail
- lesson screen
- module completion screen

Backend dependency for those flows:

- `/modules/*`

### Retired route dependency

Frontend should not depend on backend `/quiz/*` routes for:

- discovery
- daily sets
- mixed sets
- category lists
- stats
- dataset metadata
- gameplay launch

If any of those experiences still exist in the frontend, they should either:

- move to canonical `/questions/*`
- move to `/modules/*` where the flow is training-oriented
- remain local fallback temporarily until a new canonical contract exists

---

## Remaining Contract Gaps / Risks

### 1. Frontend stale quiz-route assumptions

The biggest remaining migration risk is frontend code or docs that still assume backend `/quiz/*` routes are live.

Required cleanup direction:

- remove direct `/quiz/*` transport calls
- remove fallback logic that assumes backend quiz routes exist
- stop documenting `/quiz/*` as if it were a valid backend surface

### 2. Discovery/stats expectations need re-scoping

Older frontend flows expected backend discovery/stats routes such as quiz categories, class stats, and dataset info.

Those are not part of the current supported gameplay backend contract in this repo state.

For now, frontend should treat those as one of:

- local fallback concerns
- future work under a new canonical `/questions/*` discovery surface
- future Study-specific API work

### 3. Play vs Learn payload separation must remain explicit

Gameplay question DTOs and learning lesson DTOs should not be conflated.

Keep these rules:

- gameplay retrieval does not expose the correct answer
- learning lessons may expose the correct answer and explanation

---

## Recommended Backend Contract Standard

### Gameplay retrieval

Keep `/questions/set` stable and simple:

```json
{
  "questions": [
    {
      "id": "guid",
      "text": "Question text",
      "category": "Science",
      "difficulty": 2,
      "options": [
        { "id": "A", "text": "Option A" },
        { "id": "B", "text": "Option B" }
      ],
      "mediaKey": null
    }
  ],
  "count": 10
}
```

Keep discovery surfaces answer-safe as well:

- `GET /questions/categories` should expose approved categories only
- `GET /questions/metadata` should expose filter metadata, not answers
- `POST /questions/preview-set` should return gameplay-safe question DTOs, not learning-style explanations or correct-answer fields

### Gameplay grading

Keep `/questions/check` and `/questions/check-batch` aligned to option IDs:

```json
{
  "questionId": "guid",
  "selectedOptionId": "A",
  "correctOptionId": "A",
  "isCorrect": true
}
```

### Learning

Keep module lessons learning-oriented and explicit:

```json
{
  "lessonId": "guid",
  "order": 1,
  "questionId": "guid",
  "questionText": "Question text",
  "questionCategory": "Science",
  "options": [
    { "id": "A", "text": "Option A" },
    { "id": "B", "text": "Option B" }
  ],
  "correctOptionId": "A",
  "explanation": "Why A is correct"
}
```

---

## Backend Validation Checklist

- Confirm `GET /questions/set` remains the preferred canonical gameplay retrieval route.
- Confirm `GET /questions/categories`, `GET /questions/metadata`, and `POST /questions/preview-set` remain answer-safe discovery surfaces.
- Confirm `POST /questions/check` and `POST /questions/check-batch` remain stable.
- Confirm learning/training flows use `/modules/*`, not retired `/quiz/*`.
- Confirm retired `/quiz/*` routes remain absent from the backend API.
- Confirm gameplay payloads do not expose correct answers.
- Confirm learning lesson payloads may expose correct answers and explanations intentionally.

Status in this backend repo:

- `GET /questions/set` is implemented as the canonical gameplay retrieval route.
- `GET /questions/categories`, `GET /questions/metadata`, and `POST /questions/preview-set` are implemented as canonical discovery/preview routes.
- `POST /questions/check` and `POST /questions/check-batch` are implemented as the canonical grading routes.
- Representative `/quiz/*` route contract tests assert `404 Not Found`.
- Gameplay contract tests verify the retrieval payload omits `correctOptionId`.

---

## Recommended Next Joint Step

The cleanest next step is to stabilize and communicate these surfaces clearly:

1. `GET /questions/set`
2. `POST /questions/check`
3. `POST /questions/check-batch`
4. `GET /modules`
5. `GET /modules/{id}/lessons`

Once frontend flows are aligned to those surfaces, the remaining quiz-route cleanup becomes a straightforward deprecation/removal exercise instead of a contract debate.

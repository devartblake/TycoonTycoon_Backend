# Study Frontend/Backend Handoff

**Date:** 2026-04-18  
**Audience:** Frontend Team, Backend/API Team, Platform Team  
**Status:** Backend Study API baseline plus deepening pass implemented; frontend Study hub/routes still pending  
**Purpose:** Give the frontend team a concrete integration handoff for the new Study surface, including study-set discovery, favorites, custom sets, flashcard/self-test sessions, and due-review recommendations.

---

## Summary

The backend now exposes a dedicated Study route family under:

- `/study-sets`
- `/study-sessions`

This surface is separate from:

- `/questions/*` for gameplay-safe question retrieval and grading
- `/modules/*` for guided learning modules and lesson completion

The backend Study contract is intended for:

- favorites review
- custom saved study sets
- category study sets
- weak-area review
- due-review recommendations
- flashcard sessions
- self-test sessions

The frontend should treat Study as its own product surface and should not route these flows through legacy `/quiz/*` semantics.

---

## Backend Status

### Implemented backend Study capabilities

- generated category study sets
- generated weak-area study sets
- generated favorites study set for authenticated users
- generated due-review study set from persisted spaced-review state
- custom saved study sets with create/update support
- resumable study sessions
- flashcard vs self-test session mode persistence
- per-card flashcard interaction state persistence inside sessions
- per-player study card state for review scheduling

### Not implemented yet

- frontend `/study` route scaffolding
- frontend Study hub UI
- full flashcard UI/animation/state machine on the client
- richer spaced-repetition tuning beyond the current lightweight server policy
- collaborative/shared/admin-curated custom study-set management beyond player-owned sets

---

## Route Inventory

### Study set discovery and detail

| Method | Route | Auth | Purpose |
|---|---|---|---|
| `GET` | `/study-sets` | Optional | Discover available study sets for the current or specified player |
| `GET` | `/study-sets/recommended` | Optional | Discover recommended sets, including weak-area and due-review sets when available |
| `GET` | `/study-sets/{id}` | Optional | Resolve a study set into ordered study questions |

### Favorites

| Method | Route | Auth | Purpose |
|---|---|---|---|
| `POST` | `/study-sets/favorites/{questionId}` | Required | Save an approved question into the authenticated player's favorites study set |
| `DELETE` | `/study-sets/favorites/{questionId}` | Required | Remove a question from the authenticated player's favorites study set |

### Custom study sets

| Method | Route | Auth | Purpose |
|---|---|---|---|
| `POST` | `/study-sets` | Required | Create a custom saved study set owned by the authenticated player |
| `PATCH` | `/study-sets/{id}` | Required | Update a custom saved study set owned by the authenticated player |

### Study sessions

| Method | Route | Auth | Purpose |
|---|---|---|---|
| `POST` | `/study-sessions` | Required | Create a study session snapshot from a study set |
| `POST` | `/study-sessions/{id}/progress` | Required | Persist self-test or flashcard interaction progress |
| `GET` | `/study-sessions/{id}/summary` | Required | Reload the latest session state for resume/recovery |

---

## Study Set IDs and Kinds

The frontend should treat `StudySetListItemDto.id` as an opaque identifier for routing and later detail fetches.

### Implemented kinds

| Kind | Example `id` | Meaning |
|---|---|---|
| `Category` | `category:Science` | Generated from approved questions in that category |
| `WeakArea` | `weak-area:History` | Generated from player weak-area analytics |
| `Favorites` | `favorites` | Generated from the authenticated player's favorited questions |
| `Custom` | `custom:<guid>` | Persisted custom study set owned by the authenticated player |
| `DueReview` | `due-review` | Generated from spaced-review state for the authenticated player |

### Frontend routing recommendation

- Use the `id` exactly as returned by discovery endpoints.
- Do not parse the `id` on the client unless needed for display heuristics.
- Route examples:
  - `/study/set/category:Science`
  - `/study/set/favorites`
  - `/study/set/custom:<guid>`
  - `/study/set/due-review`

---

## DTO Contracts

### `StudySetListItemDto`

Used by:

- `GET /study-sets`
- `GET /study-sets/recommended`

Shape:

```json
{
  "id": "favorites",
  "title": "Favorites Study Set",
  "description": "Review your saved favorite questions in one dedicated study set.",
  "kind": "Favorites",
  "category": "Favorites",
  "questionCount": 12
}
```

Notes:

- `kind` is the primary client-side mode hint for iconography and grouping.
- `category` is not always a literal gameplay category; for generated meta-sets it may be values like `Favorites`, `DueReview`, or `Custom`.

### `StudySetDetailDto`

Used by:

- `GET /study-sets/{id}`
- `POST /study-sets`
- `PATCH /study-sets/{id}`

Shape:

```json
{
  "id": "custom:6a6d1c90-8f44-4b55-8b77-7b80c2c9d8a1",
  "title": "My Saved Set",
  "description": "A saved custom set",
  "kind": "Custom",
  "category": "Custom",
  "questionCount": 2,
  "questions": [
    {
      "id": "f47ac10b-58cc-4372-a567-0e02b2c3d479",
      "text": "What is the powerhouse of the cell?",
      "category": "Biology",
      "difficulty": "Easy",
      "options": [
        { "id": "A", "text": "Nucleus" },
        { "id": "B", "text": "Mitochondria" }
      ],
      "correctOptionId": "B",
      "mediaKey": null
    }
  ]
}
```

Important difference from gameplay:

- Study set detail intentionally exposes `correctOptionId`.
- Study is a rehearsal surface, not a competitive gameplay surface.

### `CreateStudySetRequest`

Used by:

- `POST /study-sets`

Shape:

```json
{
  "title": "Cell Biology Review",
  "description": "Saved review set for exam prep",
  "questionIds": [
    "guid-1",
    "guid-2"
  ]
}
```

Validation notes:

- title is required
- at least one approved question is required
- non-approved or unknown question IDs are filtered out server-side

### `UpdateStudySetRequest`

Used by:

- `PATCH /study-sets/{id}`

Shape is the same as create.

Validation notes:

- only `custom:<guid>` study-set IDs are updateable
- only the owning authenticated player can update the set

### `CreateStudySessionRequest`

Used by:

- `POST /study-sessions`

Shape:

```json
{
  "studySetId": "favorites",
  "mode": "Flashcard",
  "count": 20
}
```

Fields:

- `studySetId`: required
- `mode`: optional, defaults to `SelfTest`
- `count`: optional requested session size

Supported session modes:

- `SelfTest`
- `Flashcard`

### `UpdateStudySessionProgressRequest`

Used by:

- `POST /study-sessions/{id}/progress`

Shape:

```json
{
  "questionId": "guid",
  "selectedOptionId": "A",
  "currentQuestionIndex": 3,
  "flashcardAction": "Again",
  "confidence": 1,
  "answerRevealed": true,
  "isCompleted": false
}
```

Usage rules:

- For self-test:
  - send `questionId`
  - send `selectedOptionId`
  - optionally send `currentQuestionIndex`
- For flashcards:
  - send `questionId`
  - send `flashcardAction`
  - optionally send `confidence`
  - optionally send `answerRevealed`
- At least one of these must be present when `questionId` is present:
  - `selectedOptionId`
  - `flashcardAction`
  - `confidence`
  - `answerRevealed`

Supported flashcard actions:

- `Again`
- `Hard`
- `Good`
- `Easy`

### `StudySessionDto`

Used by:

- `POST /study-sessions`
- `POST /study-sessions/{id}/progress`
- `GET /study-sessions/{id}/summary`

Shape:

```json
{
  "id": "guid",
  "studySetId": "due-review",
  "mode": "Flashcard",
  "title": "Due Review Study Set",
  "kind": "DueReview",
  "category": "DueReview",
  "questionCount": 10,
  "answeredCount": 0,
  "correctCount": 0,
  "currentQuestionIndex": 0,
  "isCompleted": false,
  "questionIds": ["guid-1", "guid-2"],
  "answeredQuestionIds": [],
  "interactions": [
    {
      "questionId": "guid-1",
      "flashcardAction": "Again",
      "confidence": 1,
      "answerRevealed": true,
      "lastInteractedAtUtc": "2026-04-18T15:00:00Z"
    }
  ],
  "createdAtUtc": "2026-04-18T14:55:00Z",
  "updatedAtUtc": "2026-04-18T15:00:00Z",
  "completedAtUtc": null
}
```

Notes:

- `answeredQuestionIds` is authoritative for server-tracked self-test answers.
- `interactions` is authoritative for flashcard state resumption.
- `questionIds` is the ordered session snapshot, so the frontend should not re-fetch and reshuffle questions while a session is active.

---

## Expected Frontend Flows

### 1. Study hub discovery

Recommended first-load calls:

- `GET /study-sets`
- optionally `GET /study-sets/recommended`

Recommended UI grouping:

- continue studying:
  - due review
  - active/resumable sessions if frontend stores recent session IDs
- review sets:
  - favorites
  - weak areas
  - categories
- saved sets:
  - custom sets

### 2. Favorites flow

Recommended flow:

1. User taps a save/favorite affordance on a question.
2. Frontend calls `POST /study-sets/favorites/{questionId}`.
3. Frontend updates favorite UI optimistically or after success.
4. Study hub can navigate directly to `/study/set/favorites`.

Recommended remove flow:

1. User removes a question from favorites.
2. Frontend calls `DELETE /study-sets/favorites/{questionId}`.
3. If the favorites set becomes empty, `GET /study-sets/favorites` will return `404 Not Found`.

### 3. Custom set flow

Recommended flow:

1. User selects questions into a custom set builder.
2. Frontend calls `POST /study-sets`.
3. Frontend stores returned `StudySetDetailDto.id`.
4. Later edits use `PATCH /study-sets/{id}`.

Recommended UX rule:

- do not attempt to patch non-custom set IDs

### 4. Self-test session flow

Recommended flow:

1. Frontend resolves the set via `GET /study-sets/{id}` if it needs pre-launch preview.
2. Frontend calls:
   - `POST /study-sessions`
   - `mode = "SelfTest"`
3. Frontend drives the quiz/test UI from the returned session snapshot.
4. For each answer, call `POST /study-sessions/{id}/progress` with `selectedOptionId`.
5. On resume, call `GET /study-sessions/{id}/summary`.

### 5. Flashcard session flow

Recommended flow:

1. Frontend calls:
   - `POST /study-sessions`
   - `mode = "Flashcard"`
2. Frontend flips/reveals locally for UI animation.
3. On user rating/review action, call `POST /study-sessions/{id}/progress` with:
   - `flashcardAction`
   - `confidence` where appropriate
   - `answerRevealed`
4. On resume, rebuild card state from `StudySessionDto.interactions`.

---

## Spaced Review Semantics

The current backend implementation uses lightweight server-side review scheduling.

### What the frontend should assume

- `due-review` is the server’s best current review queue for the authenticated player
- review timing is based on persisted `StudyCardState`
- self-test correctness and flashcard review actions both feed that review state

### What the frontend should not assume

- exact SM-2 parity
- stable review intervals as a public contract
- that `due-review` contains all previously reviewed questions forever

Treat `due-review` as a backend recommendation surface, not a client-side scheduling algorithm.

---

## Error Handling

Errors use the standard backend envelope:

```json
{
  "error": {
    "code": "NOT_FOUND",
    "message": "Study set not found.",
    "details": {}
  }
}
```

Common codes:

| HTTP | Code | Typical scenario |
|---|---|---|
| `400` | `VALIDATION_ERROR` | invalid custom set payload, invalid session progress payload, non-custom patch target |
| `401` | `UNAUTHORIZED` | favorites/custom-set/session mutation without auth |
| `404` | `NOT_FOUND` | unknown study set, unknown custom set, empty favorites set detail, unknown session |

---

## Frontend Implementation Priorities

### Highest-value next steps

1. Build `StudyHubScreen`
2. Add `/study`, `/study/set/:setId`, `/study/flashcards/:setId`, `/study/test/:setId`
3. Add favorites affordances on question cards/results
4. Build a custom-set composer using approved question selections
5. Build flashcard flow on top of `StudySessionDto.interactions`
6. Build self-test flow on top of `StudySessionDto.answeredQuestionIds`

### Suggested route map

- `/study`
- `/study/set/:setId`
- `/study/flashcards/:setId`
- `/study/test/:setId`
- `/study/favorites`
- `/study/review/due`
- `/study/custom/:setId/edit`

---

## Backend Verification Status

Focused backend study contract tests passed for:

- study-set discovery
- study-set detail
- favorites add/remove
- session creation
- session progress updates
- session mode persistence
- flashcard interaction persistence
- custom set create/update
- due-review recommendations

Current backend caveat:

- the migrations project still has a local CLI verification issue in this environment where `dotnet build Tycoon.Backend.Migrations\Tycoon.Backend.Migrations.csproj --no-restore` exits without surfacing compiler diagnostics
- the API and test projects build successfully, and the focused Study contract suite is green

---

## Recommended Frontend Defaults

- Treat Study as a first-class surface separate from Play and Learn.
- Use study-set IDs as opaque routing identifiers.
- Use server-returned session state as authoritative for resume.
- Do not try to infer gameplay DTO rules from Study DTOs.
- Do not route Study flows through `/questions/check` or legacy `/quiz/*` assumptions.


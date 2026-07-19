# Web Companion ↔ Backend API Contract Audit

**Date**: 2026-07-19
**Scope**: Every `apiClient` call in `web-companion/src` verified against the actual
routes registered under `/api/v1` in `Synaptix.Backend.Api` (this repo).
**Outcome**: All mismatches listed below were **fixed in the web client** as part of
this audit (branch `claude/web-companion-audit-demwsv`). The backend was not changed.

## Why this audit

Status docs claimed Phase 2 "API Integration ✅ COMPLETE (18 endpoints)". The URLs
existed in `client.ts`, but roughly half of them pointed at routes that do not exist
on this backend, or sent payloads the backend cannot bind. The quiz loop, friends,
store purchase, missions claim, and personal rank were all broken end-to-end.

## Findings and fixes

### Quiz loop (was completely broken against the live API)

| Call | Problem found | Fix applied |
|---|---|---|
| `POST /matches/start` | Body was `{mode}`; backend requires `{hostPlayerId, mode}` and 403s when `hostPlayerId` ≠ JWT sub. Every quiz start failed with 403. | Client now sends `hostPlayerId` from the auth store. |
| `GET /questions/set` | Endpoint exists, but the client stored raw `GameplayQuestionDto`s into the mock-era `Question` shape (`text`→`question`, `options` are `{id,text}` objects not strings, **no `correctAnswer`** — the server deliberately withholds answers). Questions rendered as `undefined` and grading always failed. | Client maps DTO → UI shape (`question`, `options[]` texts, parallel `optionIds[]`, `timeLimit: 30`). |
| Grading | Client graded locally via `correctAnswer`, which the API never sends. | New `checkAnswer()` → `POST /questions/check`; `QuizSessionScreen` now grades server-side and reveals the correct option from the response. |
| `POST /matches/submit` | Client sent `{matchId, answers, score}`; backend binds `SubmitMatchRequest` (`eventId`, `mode`, `category`, `questionCount`, timestamps, `status`, `participants[]`, `answers[]`). Also sent fabricated option ids (`option_${index}`). | Client builds the full `SubmitMatchRequest` (idempotent `eventId` via `crypto.randomUUID()`), with real option ids captured during play. |
| `GET /questions/categories` | Endpoint exists; response is `{categories: [{key, count}]}` but the lobby ran `Object.entries()` over the array, producing category ids `"0","1",...` — which then broke `GET /questions/set?category=0`. | Lobby maps the facet array properly. |

### Wrong URLs (404 on every call)

| Client called | Backend actually has | Fix applied |
|---|---|---|
| `GET /leaderboard/rank/{id}` | `GET /leaderboards/me/{id}` (`MyTierDto`) | Path fixed; response mapped to the page's entry shape; 404 (no entry yet) returns `null` instead of erroring the whole page. |
| `GET /social/friends` | `GET /users/me/friends` (paged `{items}` of `FriendDto`) | Path + response mapping fixed. |
| `POST /social/friends/add {friendId}` | `POST /users/me/friends/request {targetUserId: Guid}` | Client resolves the typed username via `GET /users/search?handle=` first, then sends the request. |
| `POST /social/friends/remove` | `DELETE /users/me/friends/{friendPlayerId}` | Fixed. |
| `GET /social/friend-requests` | `GET /users/me/friends/requests` (paged `{items}` of `FriendRequestDetailDto`) | Path + mapping fixed. |
| `POST /social/friend-requests/{id}/accept·decline` | `POST /users/me/friends/requests/{id}/accept·decline` | Fixed. |
| `GET /store/items` | `GET /store/catalog` (`{items: StoreItemDto[], count}`) | Path fixed; `StoreItemDto` (`sku`, `itemType`, `priceCoins`/`priceDiamonds`) mapped to the page's item shape. |
| `POST /store/purchase {itemId, currencyType}` | Same path, but body is `StorePurchaseRequest {playerId, sku, quantity, currency}` | Body fixed. |
| `POST /missions/{id}/claim-reward` | `POST /missions/{id}/claim?playerId=` | Path + required `playerId` query fixed. |
| `POST /missions/{id}/complete` | No such endpoint | Removed (was unused by any page). |

### Response-shape mapping added (endpoint existed, page shape didn't)

- **`GET /leaderboard`** (legacy alias) returns rows keyed `user_id`, `playerName`,
  `score`, `tier` (int) — the page expected `playerId`/`username`/`xp`/`tier` (string)
  and crashed on `tier.toLowerCase()`. Mapped in the client.
- **`GET /missions`** returns global `MissionDto {id, type, key, goal, rewardXp}` —
  no title/description/per-player progress. Mapped with sensible defaults
  (see gaps below).

### Pre-existing build breakage (fixed)

`npm run build` failed on `main` before any of this work — 4 TypeScript errors in
`ForgotPasswordPage.tsx`, including a real runtime bug: `response.resetToken`
instead of `response.data.resetToken`, meaning **the OTP → reset-password step could
never have worked** (the reset token was always empty). Fixed; build is green.

### Verified working (no change needed)

Auth (`login`, `signup`, `logout`, `refresh`, `change-password`, `forgot-password`,
`verify-otp`, `reset-password`), `GET /users/me`, `GET /users/me/wallet` (dashboard
already maps `credits`/`synapseShards`), `PATCH /users/me`, `GET /matches/{id}`,
`GET /seasons/active`, `GET /seasons/state/{id}`, `POST /questions/check-batch`.

## Known gaps that need product/backend decisions (not fixed here)

1. **Missions have no per-player progress read endpoint.** `GET /missions` returns
   global definitions only; `/missions/progress/*` are write-side. The missions page
   shows `0/goal` and claims will 409 until a "my missions state" endpoint exists.
2. **`FriendDto` has no level/xp**, and `MyTierDto` has no level — those UI fields
   show 0. Either extend the DTOs or drop the fields from the UI.
3. **Store `itemType` values** may not match the page's `power-up | cosmetic |
   skill-boost` filter tabs; the "All" tab always works. Align vocabularies.
4. **Dashboard TODOs remain** (level/xp/rank/streak/accuracy hardcoded) — candidates:
   `/users/{id}/career-summary`, `/leaderboards/me/{id}`, `/seasons/state/{id}`.
5. **Skill tree** — the plan's signature web feature — is still a 352-byte
   placeholder. Backend `/skills` endpoints exist; the web UI does not.
6. **Stripe**: backend already exposes `POST /store/payments/checkout/session` and
   subscription checkout/portal sessions. The web client has Stripe deps installed
   but no checkout flow — this is the shortest path to web revenue.

## Verification

- `npm run build` (tsc + vite): ✅ green (was broken before this branch)
- `npx oxlint src`: no errors (pre-existing `exhaustive-deps` warnings only)
- No dev server + live backend available in this environment; endpoint/DTO pairs
  were verified statically against the endpoint registrations and
  `Synaptix.Shared.Contracts` DTOs, which serialize camelCase with string enums
  (`JsonStringEnumConverter` is registered in `Program.cs`).

# Full Backend API Handoff — 2026-04-28

> **Audience:** Flutter frontend team
> **Date:** 2026-04-28
> **Base URL:** `http(s)://<host>:5000`
> **Auth header:** `Authorization: Bearer <jwt>` (all authenticated endpoints)
> **Admin header:** `X-Admin-Ops-Key: <key>` (all `/admin/*` endpoints)
> **OpenAPI docs:** `GET /swagger` (dev only)

---

## What Changed Since Last Handoff (2026-04-26)

| Surface | Change |
|---------|--------|
| Questions | **Performance fix** — `GET /questions/set` and `POST /questions/preview-set` no longer timeout. `ORDER BY RANDOM()` replaced with count+skip. Options JOIN removed from categories/metadata queries. `GET /questions/metadata` now runs 1 DB query instead of 2. |
| Learning Modules | **Performance fix** — `GET /modules/progress/{playerId}` no longer timeout. Replaced large `WHERE id IN (...)` parameter with a JOIN query. |
| Avatars | Avatar purchase and asset download endpoints live: `POST /store/avatars/{avatarId}/purchase`, `GET /v1/assets/avatars/{avatarId}`, `GET /store/catalog?category=avatar` |

---

## Quick Reference — All Surfaces

| Prefix | Auth | Description |
|--------|------|-------------|
| `/auth` | None (logout requires JWT) | Registration, login, token refresh |
| `/users` | JWT | Profile, wallet, transactions, avatar upload |
| `/questions` | None | Gameplay question sets, answer grading |
| `/store` | Mixed | Item catalog, purchases, subscriptions, rewards |
| `/store/avatars` | JWT | 3D avatar purchase flow |
| `/v1/assets/avatars` | JWT | Presigned avatar GLB archive download |
| `/crypto` | JWT | On-chain wallet link, balance, staking, withdrawals |
| `/modules` | None | Learning module catalog, lessons, progress, completion |
| `/study-sessions` | JWT | Study session create/progress/summary |
| `/study-sets` | JWT | User study sets, favorites |
| `/notifications` | JWT | Inbox, unread count, mark-read, delete |
| `/messages` | JWT | Direct message conversations and threads |
| `/missions` | None/JWT | Mission list, progress hooks, reward claim |
| `/leaderboards` | None | Ranked and tiered leaderboards |
| `/matches` | JWT | Match start, submit, lookup |
| `/matchmaking` | JWT | Queue enqueue/cancel/status |
| `/party` | JWT | Party create/invite/join/queue |
| `/friends` | JWT | Friend requests, list, remove |
| `/seasons` | JWT | Active season, player state, rewards |
| `/skills` | JWT | Skill tree, player state, unlock, respec |
| `/powerups` | JWT | Powerup state, use |
| `/guardians` | JWT | Guardian tier info and challenge |
| `/game-events` | JWT | Event entry, revive, upcoming list |
| `/referrals` | None/JWT | Referral code create, lookup, redeem |
| `/qr` | JWT | QR scan tracking and history |
| `/ml` | JWT | Churn risk and match quality signals |
| `/players` | None/JWT | Player CRUD (low-level) |
| `/mobile/*` | JWT | Flutter-specific mirror of matches, players, leaderboards, seasons, economy |
| `/ws` | JWT via `?access_token=` | Raw WebSocket presence protocol |
| `/ws/match` | JWT | SignalR MatchHub |
| `/ws/presence` | JWT | SignalR PresenceHub |
| `/ws/notify` | JWT | SignalR NotificationHub (push notifications) |

---

## Error Envelope

All error responses follow this shape:

```json
{ "error": { "code": "ERROR_CODE", "message": "Human-readable message.", "details": {} } }
```

---

## Auth — `/auth`

No JWT required unless noted.

| Method | Path | Auth | Notes |
|--------|------|------|-------|
| `POST` | `/auth/signup` | None | **Use this for mobile registration** |
| `POST` | `/auth/register` | None | Legacy alias for `/auth/signup` — prefer `/auth/signup` |
| `POST` | `/auth/login` | None | Returns `{ accessToken, refreshToken }` |
| `POST` | `/auth/refresh` | None | Body: `{ refreshToken }` |
| `POST` | `/auth/logout` | JWT | Invalidates refresh token |

---

## Users — `/users`

Group requires JWT except `/users/search` and `/users/{userId}/career-summary`.

| Method | Path | Notes |
|--------|------|-------|
| `GET` | `/users/search?q=` | Public |
| `GET` | `/users/me` | Player profile — does **not** include wallet balance |
| `PATCH` | `/users/me` | Update display name / avatar URL |
| `GET` | `/users/me/wallet` | **Wallet balances** — returns `credits`, `neuralXp`, `synapseShards` |
| `GET` | `/users/me/transactions` | Transaction history |
| `POST` | `/users/me/onboarding-reward` | One-time new-player reward claim |
| `POST` | `/users/me/avatar/upload-url` | Returns presigned PUT URL for MinIO avatar upload |
| `GET` | `/users/{userId}/career-summary` | Public career stats |

### Wallet response shape

```json
{
  "playerId": "<uuid>",
  "credits":       500,
  "neuralXp":      100,
  "synapseShards": 0,
  "updatedAtUtc":  "2026-04-28T09:00:00Z"
}
```

`GET /wallet/{playerId}` does **not** exist. `GET /users/me` does **not** include balance fields.

---

## Questions — `/questions`

**No auth required.** These endpoints were the source of frontend timeouts — both issues are now fixed.

| Method | Path | Notes |
|--------|------|-------|
| `GET` | `/questions/set?category=&difficulty=&count=` | Random gameplay set (max 50). Correct answers omitted. |
| `GET` | `/questions/categories` | Approved categories with counts |
| `GET` | `/questions/metadata` | Categories + difficulties + default/max counts — single query, fast |
| `POST` | `/questions/preview-set` | Body: `{ categories[], difficulties[], count }` |
| `POST` | `/questions/check` | Body: `{ questionId, selectedOptionId }` → `{ isCorrect, correctOptionId }` |
| `POST` | `/questions/check-batch` | Body: `{ answers: [{questionId, selectedOptionId}] }` — max 50 |

### Performance note

Previous queries used `ORDER BY RANDOM()` which caused a full-table sort on PostgreSQL. Now uses `COUNT + random OFFSET`, making response times proportional to index lookups rather than table size.

### Question set response shape

```json
{
  "questions": [
    {
      "id": "<uuid>",
      "text": "What is 2 + 2?",
      "category": "math",
      "difficulty": "Easy",
      "options": [
        { "optionId": "a", "text": "3" },
        { "optionId": "b", "text": "4" }
      ],
      "mediaKey": null
    }
  ],
  "count": 10
}
```

---

## Store — `/store`

| Method | Path | Auth | Notes |
|--------|------|------|-------|
| `GET` | `/store/catalog?itemType=` | None | Full item catalog, filter by itemType |
| `GET` | `/store/catalog?category=avatar` | Optional JWT | Avatar catalog — includes `owned: true/false` when JWT provided |
| `GET` | `/store/catalog/{sku}` | None | Single item lookup |
| `GET` | `/store/catalog/{playerId}` | JWT | Player-personalised catalog |
| `GET` | `/store/daily` | JWT | Daily rotating store items |
| `GET` | `/store/hub` | JWT | Store hub summary (featured + daily + offers) |
| `GET` | `/store/special-offers` | JWT | Active flash-sale discounted items |
| `GET` | `/store/inventory/{playerId}` | JWT | Owned items |
| `POST` | `/store/purchase` | JWT | Coin/diamond purchase |
| `GET` | `/store/rewards/{playerId}` | JWT | Claimable reward list |
| `POST` | `/store/rewards/{playerId}/claim/{rewardId}` | JWT | Claim a reward |
| `GET` | `/store/premium` | JWT | Premium subscription item list |
| `GET` | `/store/subscription/status/{playerId}` | JWT | Subscription state |
| `POST` | `/store/subscription/activate` | JWT | Activate subscription |
| `POST` | `/store/subscription/checkout/session` | JWT | Create Stripe checkout session |
| `POST` | `/store/subscription/portal/session` | JWT | Stripe billing portal session |
| `POST` | `/store/subscription/paypal/create` | JWT | Create PayPal subscription |
| `POST` | `/store/subscription/paypal/cancel` | JWT | Cancel PayPal subscription |
| `POST` | `/store/payments/checkout/session` | JWT | Stripe one-time checkout session |
| `POST` | `/store/payments/paypal/order` | JWT | Create PayPal order |
| `POST` | `/store/payments/paypal/capture` | JWT | Capture PayPal order |
| `POST` | `/store/payments/webhook` | None | Stripe webhook (server-to-server) |
| `POST` | `/store/payments/paypal/webhook` | None | PayPal webhook (server-to-server) |
| `POST` | `/store/iap/validate` | JWT | Validate Apple/Google IAP receipt |
| `GET` | `/store/system/status` | None | Store health/config status |

### Purchase request

```json
{ "playerId": "<uuid>", "sku": "powerup:hint", "quantity": 1 }
```

---

## Avatars — `/store/avatars` · `/v1/assets/avatars`

Full 3D avatar purchase loop. All require JWT.

| Method | Path | Notes |
|--------|------|-------|
| `GET` | `/store/catalog?category=avatar` | Browse catalog — `owned: true/false` per item |
| `POST` | `/store/avatars/{avatarId}/purchase` | Buy avatar with coins |
| `GET` | `/v1/assets/avatars/{avatarId}` | Get presigned GLB archive download URL (valid 15 min) |

### Avatar catalog item shape (within `{ "items": [...] }`)

```json
{
  "id": "<uuid>",
  "sku": "avatar:cartoon-hero",
  "name": "Cartoon Hero",
  "description": "...",
  "price": 500,
  "currency": "coins",
  "category": "avatar",
  "type": "cosmetic",
  "mediaKey": "avatars/cartoon-hero-v1",
  "thumbnailUrl": "https://...",
  "owned": false,
  "isFeatured": true,
  "version": "1.0.0"
}
```

### Purchase request / response

**Request** `POST /store/avatars/{avatarId}/purchase`
```json
{ "playerId": "<uuid>" }
```

**Response `200`**
```json
{ "success": true, "avatarId": "avatar:cartoon-hero", "coinsDeducted": 500, "newBalance": 1200 }
```

**Error codes**

| Code | HTTP | When |
|------|------|------|
| `avatar_not_found` | 404 | SKU not found or inactive |
| `already_owned` | 409 | Player already owns this avatar |
| `insufficient_funds` | 409 | Not enough coins — `details: { required, available }` |
| `forbidden` | 403 | JWT player ID ≠ request body `playerId` |

### Asset download response

**Response `200`** `GET /v1/assets/avatars/{avatarId}`
```json
{
  "presignedUrl": "https://storage.example.com/avatars/...",
  "thumbnailUrl": "https://...",
  "expiresAt": "2026-04-28T10:15:00Z",
  "contentType": "application/zip",
  "archiveFormat": "zip",
  "sha256": null
}
```

Archive contains `models/avatar.glb` for `flutter_3d_controller`.

---

## Crypto Economy — `/crypto`

All require JWT. See `docs/frontend_handoff_2026-04-26.md` for full request/response shapes.

| Method | Path | Notes |
|--------|------|-------|
| `POST` | `/crypto/link-wallet` | Link on-chain wallet to player account |
| `GET` | `/crypto/balance/{playerId}` | Available (non-staked) crypto units |
| `GET` | `/crypto/history/{playerId}` | Paginated transaction history — `?page=&pageSize=` |
| `POST` | `/crypto/withdraw` | Request withdrawal — starts as `Pending`, requires admin approval |
| `GET` | `/crypto/withdraw/pending` | List pending withdrawals (admin-only in practice) |
| `POST` | `/crypto/withdraw/{id}/approve` | Admin: approve withdrawal |
| `POST` | `/crypto/withdraw/{id}/reject` | Admin: reject withdrawal |
| `POST` | `/crypto/prize-pool/fund` | Fund a prize pool |
| `GET` | `/crypto/prize-pool/{poolId}` | Get prize pool state |
| `POST` | `/crypto/prize-pool/distribute` | Distribute prize pool to winners |
| `POST` | `/crypto/stake` | Lock units from withdrawal |
| `POST` | `/crypto/unstake` | Return staked units to available balance |
| `GET` | `/crypto/staking/{playerId}` | Current staking position |

---

## Learning Modules — `/modules`

**No auth required.** These endpoints were the source of frontend timeouts — `GET /modules/progress/{playerId}` is now fixed.

| Method | Path | Notes |
|--------|------|-------|
| `GET` | `/modules?category=&difficulty=` | List published modules with lesson counts and completion state |
| `GET` | `/modules/recommended?playerId=&count=` | Recommended incomplete modules |
| `GET` | `/modules/progress/{playerId}` | Completion summary — total, completed, rate **[timeout fixed]** |
| `GET` | `/modules/{id}` | Single module detail |
| `GET` | `/modules/{id}/lessons` | Ordered lesson list for a module |
| `POST` | `/modules/{id}/complete` | Mark module complete and grant XP/coins reward |

---

## Study Hub — `/study-sessions` · `/study-sets`

All require JWT. See `docs/study_frontend_backend_handoff_2026-04-18.md` for full contracts.

| Method | Path | Notes |
|--------|------|-------|
| `POST` | `/study-sessions` | Create a new study session |
| `POST` | `/study-sessions/{id}/progress` | Submit flashcard/self-test progress |
| `GET` | `/study-sessions/{id}/summary` | Session performance summary |
| `GET` | `/study-sets` | List player's study sets |
| `GET` | `/study-sets/recommended` | Recommended study sets |
| `POST` | `/study-sets/favorites/{questionId}` | Add question to favourites |
| `DELETE` | `/study-sets/favorites/{questionId}` | Remove from favourites |
| `POST` | `/study-sets` | Create custom study set |
| `PATCH` | `/study-sets/{id}` | Update study set |
| `GET` | `/study-sets/{id}` | Get study set with questions |

---

## Notifications — `/notifications`

All require JWT. See `docs/notifications_backend_handoff_2026-04-20.md` for WebSocket push contract.

| Method | Path | Notes |
|--------|------|-------|
| `GET` | `/notifications/inbox` | Paginated notification list |
| `GET` | `/notifications/unread-count` | `{ "count": 3 }` |
| `POST` | `/notifications/{id}/read` | Mark single notification read |
| `POST` | `/notifications/read-all` | Mark all read |
| `DELETE` | `/notifications/{id}` | Dismiss/delete notification |

**WebSocket push:** Connect to `/ws/notify` (SignalR). Refresh inbox on `NotificationsUpdated` event.

---

## Direct Messages — `/messages`

All require JWT. See `docs/messaging_backend_handoff_2026-04-20.md` for WebSocket contract.

| Method | Path | Notes |
|--------|------|-------|
| `GET` | `/messages/conversations` | Conversation list with last-message preview |
| `POST` | `/messages/conversations/direct` | Create or return existing DM thread |
| `GET` | `/messages/conversations/{id}/messages?page=&pageSize=` | Paginated message thread |
| `POST` | `/messages/conversations/{id}/messages` | Send message |
| `POST` | `/messages/conversations/{id}/read` | Mark conversation read |
| `GET` | `/messages/unread-count` | `{ "count": 2 }` |

**WebSocket push:** Connect to `/ws/presence` (SignalR). Refresh thread on `DirectMessagesUpdated` event.

---

## Missions — `/missions`

| Method | Path | Auth | Notes |
|--------|------|------|-------|
| `GET` | `/missions/?type=` | None | `type`: `daily`, `weekly`, `all` |
| `POST` | `/missions/progress/match-completed` | JWT | Call after every match to credit mission progress |
| `POST` | `/missions/progress/round-completed` | JWT | Call after every round |
| `POST` | `/missions/{id}/claim` | JWT | Claim reward for completed mission |

---

## Leaderboards — `/leaderboards`

| Method | Path | Auth | Notes |
|--------|------|------|-------|
| `GET` | `/leaderboards/me/{playerId}` | None | Player's personal rank and score |
| `GET` | `/leaderboards/tiers/{tierId}` | None | Top players in a tier |
| `GET` | `/leaderboards/ranked` | None | Ranked leaderboard list |
| `GET` | `/leaderboard` | None | Legacy alias — prefer `/leaderboards` |
| `GET` | `/api/v1/leaderboard` | None | Legacy alias |

---

## Matches — `/matches`

All require JWT.

| Method | Path | Notes |
|--------|------|-------|
| `POST` | `/matches/start` | Start a match — returns match ID and question set |
| `POST` | `/matches/submit` | Submit match results |
| `GET` | `/matches/{matchId}` | Match detail and outcome |

**WebSocket:** Connect to `/ws/match` (SignalR) for real-time match events (opponent answers, countdowns, results).

---

## Matchmaking — `/matchmaking`

All require JWT.

| Method | Path | Notes |
|--------|------|-------|
| `POST` | `/matchmaking/enqueue` | Join matchmaking queue |
| `POST` | `/matchmaking/cancel` | Leave queue |
| `GET` | `/matchmaking/status/{playerId}` | Queue state (queued / matched / idle) |

---

## Party — `/party`

All require JWT.

| Method | Path | Notes |
|--------|------|-------|
| `POST` | `/party` | Create party (caller becomes leader) |
| `GET` | `/party/{partyId}` | Party state and member list |
| `POST` | `/party/{partyId}/invite` | Invite player to party |
| `POST` | `/party/invites/{inviteId}/accept` | Accept party invite |
| `POST` | `/party/invites/{inviteId}/decline` | Decline party invite |
| `POST` | `/party/{partyId}/leave` | Leave party |
| `GET` | `/party/invites` | List pending party invites |
| `POST` | `/party/{partyId}/enqueue` | Enqueue entire party for matchmaking |
| `POST` | `/party/{partyId}/queue/cancel` | Cancel party queue |

---

## Friends — `/friends`

All require JWT.

| Method | Path | Notes |
|--------|------|-------|
| `POST` | `/friends/request` | Send friend request |
| `POST` | `/friends/request/{id}/accept` | Accept request |
| `POST` | `/friends/request/{id}/decline` | Decline request |
| `GET` | `/friends` | Friend list |
| `DELETE` | `/friends` | Remove friend |
| `GET` | `/friends/requests` | Pending incoming requests |

---

## Seasons — `/seasons`

| Method | Path | Auth | Notes |
|--------|------|------|-------|
| `GET` | `/seasons/active` | None | Current season metadata |
| `GET` | `/seasons/state/{playerId}` | JWT | Player's XP, tier, rank within season |
| `GET` | `/seasons/rewards/eligibility/{playerId}` | JWT | Which season rewards player can claim |
| `POST` | `/seasons/rewards/claim/{playerId}` | JWT | Claim season reward |
| `GET` | `/seasons/rewards/preview/{playerId}` | JWT | Preview upcoming rewards |

---

## Skills — `/skills`

All require JWT.

| Method | Path | Notes |
|--------|------|-------|
| `GET` | `/skills/tree` | Full skill tree definition |
| `GET` | `/skills/state/{playerId}` | Player's unlocked skills |
| `POST` | `/skills/unlock` | Unlock a skill node (costs coins) |
| `POST` | `/skills/respec` | Reset all skill unlocks (costs coins) |

---

## Powerups — `/powerups`

All require JWT.

| Method | Path | Notes |
|--------|------|-------|
| `GET` | `/powerups/state/{playerId}` | Current powerup inventory |
| `POST` | `/powerups/use` | Activate a powerup during match |

---

## Guardians — `/guardians`

All require JWT.

| Method | Path | Notes |
|--------|------|-------|
| `GET` | `/guardians/{tierNumber}` | Guardian info for a tier |
| `POST` | `/guardians/challenge` | Challenge a guardian to unlock next tier |

---

## Game Events — `/game-events`

All require JWT.

| Method | Path | Notes |
|--------|------|-------|
| `POST` | `/game-events/enter` | Enter a live game event |
| `POST` | `/game-events/revive` | Use a revive in an event |
| `GET` | `/game-events/{gameEventId}` | Event state |
| `GET` | `/game-events/upcoming` | Scheduled upcoming events |

---

## Referrals — `/referrals`

| Method | Path | Auth | Notes |
|--------|------|------|-------|
| `POST` | `/referrals/` | JWT | Create a referral code for current player |
| `GET` | `/referrals/{code}` | None | Look up referral code info |
| `POST` | `/referrals/{code}/redeem` | JWT | Redeem a code on signup |

---

## QR — `/qr`

All require JWT.

| Method | Path | Notes |
|--------|------|-------|
| `POST` | `/qr/track-scan` | Record a QR code scan event |
| `POST` | `/qr/sync` | Batch sync offline scan queue |
| `GET` | `/qr/history/{playerId}` | Player's QR scan history |

---

## ML Signals — `/ml`

All require JWT. Consume in `ml_signal_service.dart`.

| Method | Path | Notes |
|--------|------|-------|
| `POST` | `/ml/churn-risk` | Returns churn probability for a player |
| `POST` | `/ml/match-quality` | Returns predicted match quality score |

---

## Players — `/players`

Low-level player management. For most flows use `/users/me` instead.

| Method | Path | Auth | Notes |
|--------|------|------|-------|
| `POST` | `/players/` | None | Create player record |
| `GET` | `/players/{id}` | None | Get player by ID |
| `GET` | `/players/{id}/stats` | None | Career stats |

---

## Mobile — `/mobile/*`

Flutter-specific route mirror with mobile-optimised contracts. All require JWT.

| Method | Path | Notes |
|--------|------|-------|
| `POST` | `/mobile/matches/start` | Match start (mobile contract) |
| `POST` | `/mobile/matches/submit` | Match submit (mobile contract) |
| `POST` | `/mobile/players/` | Create player |
| `GET` | `/mobile/players/{id}` | Get player |
| `GET` | `/mobile/leaderboards/me/{playerId}` | Personal rank |
| `GET` | `/mobile/leaderboards/tiers/{tierId}` | Tier leaderboard |
| `GET` | `/mobile/seasons/active` | Active season |
| `GET` | `/mobile/seasons/state/{playerId}` | Player season state |
| `GET` | `/mobile/economy/state` | Game balance policy (coin rewards, costs, multipliers) |
| `POST` | `/mobile/economy/session/start` | Start economy session for a player |
| `POST` | `/mobile/economy/daily-jackpot-ticket/claim` | Claim daily jackpot ticket |
| `POST` | `/mobile/economy/revive/quote` | Get revive cost quote |
| `POST` | `/mobile/economy/pity/report-loss` | Report loss for pity system |
| `POST` | `/mobile/economy/pity/report-win` | Report win to reset pity counter |

---

## WebSockets

JWT is passed via query string: `ws://<host>/ws?access_token=<jwt>`

| Endpoint | Protocol | Purpose |
|----------|----------|---------|
| `/ws` | Raw WebSocket | Presence protocol — sends `hello` frame on connect, broadcasts friend online/offline events |
| `/ws/match` | SignalR | Real-time match events (opponent answers, round results, game-over) |
| `/ws/presence` | SignalR | Friend presence + `DirectMessagesUpdated` events |
| `/ws/notify` | SignalR | Push notifications — fires `NotificationsUpdated` on new notification |

---

## Pending Backend Items (do not wire yet)

| Item | Status | ETA |
|------|--------|-----|
| `GET /v1/assets/audio/{category}/{filename}` | Not started | TBD |
| Admin Dashboard Wave B — Questions, Events, Seasons | Not started | TBD |
| Admin Dashboard Wave C — Moderation, Notifications, Economy, Anti-cheat | Not started | TBD |
| Backend Packet E — namespace rename | Intentionally deferred | Post-stable |

---

## Pending Database Migrations (DevOps action required)

Run before the store stock, flash sale, and reward-limit endpoints will work:

```bash
dotnet ef database update \
  --project Tycoon.Backend.Migrations \
  --startup-project Tycoon.Backend.Api
```

Applies (in order):
1. `20260425120000_AddSeasonRewardRules`
2. `20260425130000_AddStoreStockSystem`
3. `20260425140000_AddFlashSale`
4. `20260426100000_AddRewardClaimRule`
5. `20260426110000_AddEffectiveMaxQuantity`

# Frontend Handoff — 2026-04-26

> **Audience:** Flutter frontend team
> **Date:** 2026-04-26
> **Base URL:** `http(s)://<host>:5000`
> **Auth header:** `Authorization: Bearer <jwt>` (all authenticated endpoints)
> **Admin header:** `X-Admin-Ops-Key: <key>` (all `/admin/*` endpoints)
> **OpenAPI docs:** `/swagger` (dev only)

---

## What's New Since Last Handoff (2026-04-25)

| Surface | Change |
|---------|--------|
| Admin store P2 | 12 new `/admin/store/*` endpoints live |
| Crypto economy | All endpoints live — no handoff doc existed previously |
| Wallet | Clarification: use `GET /users/me/wallet`, not `GET /wallet/{playerId}` |
| Swagger | Duplicate middleware removed — `/swagger` now loads cleanly |
| Auth | `/auth/signup` is the correct mobile endpoint (not `/auth/register`) |

---

## Part 1 — Admin Store P2 Endpoints

All endpoints require `X-Admin-Ops-Key: <key>` header. No JWT required (ops-key is the sole auth mechanism for `/admin/*`).

### Error envelope

```json
{ "error": { "code": "ERROR_CODE", "message": "...", "details": {} } }
```

---

### Stock Policies

#### `GET /admin/store/stock-policies`

List all stock policies.

**Query params:** `?activeOnly=true` · `?sku=powerup:skip`

**Response `200`**
```json
{
  "policies": [
    {
      "sku": "powerup:skip",
      "maxQuantityPerUser": 5,
      "resetInterval": "daily",
      "isActive": true,
      "createdAtUtc": "2026-04-25T00:00:00Z",
      "updatedAtUtc": "2026-04-25T00:00:00Z"
    }
  ]
}
```

---

#### `PUT /admin/store/stock-policies/{sku}`

Create or update the stock policy for a SKU.

**Request**
```json
{ "maxQuantityPerUser": 5, "resetInterval": "daily", "isActive": true }
```

`resetInterval` must be `"daily"`, `"weekly"`, or `"none"`. `maxQuantityPerUser: 0` = unlimited. `isActive` is optional on update.

**Response `200`** — same shape as a single policy object above.

---

#### `POST /admin/store/stock-policies/bulk-reset`

Resets `quantityUsed = 0` for **all players** on the given SKU list. Does not change `NextResetAtUtc` relative to the policy interval.

**Request**
```json
{ "skus": ["powerup:skip", "powerup:hint"], "reason": "Weekend event reset" }
```

**Response `200`**
```json
{
  "skusReset": ["powerup:skip", "powerup:hint"],
  "playersAffected": 214,
  "resetAt": "2026-04-26T10:00:00Z",
  "reason": "Weekend event reset"
}
```

---

### Player Stock

#### `GET /admin/store/player-stock/{playerId}`

View a player's current stock state across all SKUs.

**Response `200`**
```json
{
  "playerId": "3fa8...",
  "items": [
    {
      "sku": "powerup:skip",
      "quantityUsed": 3,
      "maxQuantity": 5,
      "remaining": 2,
      "effectiveMaxQuantity": null,
      "lastResetAtUtc": "2026-04-26T00:00:00Z",
      "nextResetAtUtc": "2026-04-27T00:00:00Z",
      "updatedAtUtc": "2026-04-26T09:30:00Z"
    }
  ]
}
```

`effectiveMaxQuantity` is non-null when an admin override is active. When present it takes precedence over the policy `maxQuantity`.

---

#### `POST /admin/store/player-stock/{playerId}/override`

Set a temporary per-player ceiling for a specific SKU. Pass `null` to clear the override and revert to policy default.

**Request**
```json
{ "sku": "powerup:skip", "effectiveMaxQuantity": 10, "reason": "Support ticket #4421" }
```

**Response `200`**
```json
{
  "playerId": "3fa8...",
  "sku": "powerup:skip",
  "effectiveMaxQuantity": 10,
  "updatedAt": "2026-04-26T10:05:00Z",
  "reason": "Support ticket #4421"
}
```

---

### Flash Sales

#### `GET /admin/store/flash-sales`

Returns active and scheduled flash sales only (excludes cancelled and expired).

**Response `200`**
```json
{
  "sales": [
    {
      "id": "9f3c...",
      "sku": "powerup:hint",
      "discountPercent": 30,
      "startsAtUtc": "2026-04-26T00:00:00Z",
      "endsAtUtc": "2026-04-27T23:59:59Z",
      "isActive": true,
      "reason": "Weekend promo",
      "createdAtUtc": "2026-04-25T12:00:00Z"
    }
  ]
}
```

---

#### `POST /admin/store/flash-sales`

Create a new flash sale. Validates: SKU must be an active catalog item; no overlap with an existing active sale for the same SKU.

**Request**
```json
{
  "sku": "powerup:hint",
  "discountPercent": 30,
  "startsAtUtc": "2026-04-26T00:00:00Z",
  "endsAtUtc": "2026-04-27T23:59:59Z",
  "reason": "Weekend promo"
}
```

**Response `201`** — same shape as a single sale object above.

**Error codes**

| Code | HTTP | When |
|------|------|------|
| `VALIDATION_ERROR` | 400 | Missing/invalid fields |
| `NOT_FOUND` | 404 | SKU not found or inactive |
| `SALE_OVERLAP` | 409 | Another active sale overlaps the window |

---

#### `DELETE /admin/store/flash-sales/{id}`

Soft-cancels a flash sale (`isActive = false`). The sale record is preserved for audit history. The player-facing `GET /store/special-offers` will stop returning it immediately.

**Response `204`** — no body.

**Error codes**

| Code | HTTP | When |
|------|------|------|
| `NOT_FOUND` | 404 | Sale ID not found |
| `ALREADY_CANCELLED` | 409 | Sale already inactive |

---

### Reward Claim Limits

#### `GET /admin/store/reward-limits/{rewardId}`

**Response `200`**
```json
{
  "rewardId": "daily-bonus",
  "maxClaimsPerInterval": 1,
  "resetInterval": "daily",
  "isActive": true,
  "updatedAtUtc": "2026-04-26T00:00:00Z"
}
```

Returns `404 NOT_FOUND` if no rule has been created for this `rewardId`.

---

#### `PUT /admin/store/reward-limits/{rewardId}`

Create or update the claim-frequency cap for a reward. `rewardId` is any string identifier (e.g. `"daily-bonus"`, `"weekly-pack"`).

**Request**
```json
{ "maxClaimsPerInterval": 1, "resetInterval": "daily", "isActive": true }
```

`resetInterval`: `"daily"` · `"weekly"` · `"none"`. `isActive` optional on update.

**Response `200`** — same shape as the GET response above.

---

### Analytics

#### `GET /admin/store/analytics/purchases`

Aggregate purchase statistics. All filters optional.

**Query params:** `?from=2026-04-01T00:00:00Z` · `?to=2026-04-26T23:59:59Z` · `?sku=powerup:skip`

**Response `200`**
```json
{
  "from": "2026-04-01T00:00:00Z",
  "to": "2026-04-26T23:59:59Z",
  "totalPurchases": 1842,
  "totalCoinsSpent": 94100,
  "topSkus": [
    { "sku": "powerup:skip",  "purchaseCount": 731 },
    { "sku": "powerup:hint",  "purchaseCount": 614 },
    { "sku": "powerup:extra-time", "purchaseCount": 497 }
  ]
}
```

---

#### `GET /admin/store/analytics/stock-resets`

Paginated history of player stock resets, derived from `PlayerStoreStockState.lastResetAtUtc`. Ordered most-recent first.

**Query params:** `?sku=powerup:skip` · `?page=1` · `?pageSize=50`

**Response `200`**
```json
{
  "items": [
    {
      "playerId": "3fa8...",
      "sku": "powerup:skip",
      "lastResetAt": "2026-04-26T00:00:00Z",
      "nextResetAt": "2026-04-27T00:00:00Z",
      "quantityUsed": 0
    }
  ],
  "page": 1,
  "pageSize": 50,
  "totalItems": 4280,
  "totalPages": 86
}
```

---

## Part 2 — Crypto Economy Endpoints

> These endpoints have been live for some time but had no Flutter handoff doc. Wire them now.

All routes are under `/crypto`. All require `Authorization: Bearer <jwt>`.

---

### `POST /crypto/link-wallet`

Links a player's in-game account to an on-chain wallet address.

**Request**
```json
{ "playerId": "<uuid>", "walletAddress": "7xKX...", "network": "solana" }
```

`network` defaults to `"solana"` if omitted.

**Response `200`**
```json
{
  "playerId": "<uuid>",
  "walletAddress": "7xkx...",
  "network": "solana",
  "transactionId": "<uuid>",
  "status": "Applied"
}
```

---

### `GET /crypto/balance/{playerId}`

Returns the player's available crypto units (not staked).

**Response `200`**
```json
{ "playerId": "<uuid>", "units": 2500, "unitType": "CRYPTO_UNITS" }
```

---

### `GET /crypto/history/{playerId}`

Paginated crypto transaction history.

**Query params:** `?page=1` · `?pageSize=20`

**Response `200`**
```json
{
  "page": 1,
  "pageSize": 20,
  "total": 47,
  "items": [
    {
      "transactionId": "<uuid>",
      "kind": "crypto-stake",
      "units": 500,
      "direction": "out",
      "status": "Applied",
      "createdAtUtc": "2026-04-20T14:00:00Z"
    }
  ]
}
```

---

### `POST /crypto/withdraw`

Request a withdrawal of crypto units to an on-chain wallet.

**Request**
```json
{ "playerId": "<uuid>", "units": 1000, "toWalletAddress": "7xKX...", "network": "solana" }
```

**Response `200`**
```json
{ "transactionId": "<uuid>", "status": "Pending", "units": 1000, "network": "solana" }
```

Withdrawals start as `Pending` and require admin approval before settling.

---

### `POST /crypto/stake`

Stake crypto units (locks them from withdrawal/spend until unstaked).

**Request**
```json
{ "playerId": "<uuid>", "units": 500 }
```

**Response `200`**
```json
{ "transactionId": "<uuid>", "playerId": "<uuid>", "units": 500, "currentStakedUnits": 500, "status": "Applied" }
```

---

### `POST /crypto/unstake`

Unstake previously staked units, returning them to available balance.

**Request** — same shape as stake.

**Response `200`** — same shape as stake response, `currentStakedUnits` reflects new total.

---

### `GET /crypto/staking/{playerId}`

Returns the player's current staking position.

**Response `200`**
```json
{
  "playerId": "<uuid>",
  "availableUnits": 2000,
  "stakedUnits": 500,
  "unitType": "CRYPTO_UNITS"
}
```

---

## Part 3 — Wallet Endpoint Clarification

The correct endpoint for the player's coin/diamond/XP wallet is:

```
GET /users/me/wallet
Authorization: Bearer <jwt>
```

**Response `200`**
```json
{
  "playerId": "<uuid>",
  "credits":       500,
  "neuralXp":      100,
  "synapseShards": 0,
  "updatedAtUtc":  "2026-04-26T09:00:00Z"
}
```

| JSON field | Meaning |
|-----------|---------|
| `credits` | Coins |
| `neuralXp` | XP |
| `synapseShards` | Diamonds |

`GET /wallet/{playerId}` does **not** exist. `GET /users/me` does **not** include balance fields.

---

## Part 4 — Outstanding Backend Items Blocked on Frontend

These backend endpoints are live and tested. The only remaining work is on the Flutter side:

| Surface | Backend | Flutter work needed |
|---------|---------|-------------------|
| Study hub | ✅ Live | `StudyHubScreen`, `/study` routes, flashcard + self-test flows. See `docs/study_frontend_backend_handoff_2026-04-18.md` |
| Direct messaging | ✅ Live | WebSocket `DirectMessagesUpdated` hookup, paginated thread UI. See `docs/messaging_backend_handoff_2026-04-20.md` |
| Notifications | ✅ Live | `/ws/notify` refresh, mark-read/dismiss UI. See `docs/notifications_backend_handoff_2026-04-20.md` |
| Crypto economy | ✅ Live | All 10 crypto endpoints (this doc, Part 2). Flutter service + providers not yet wired |
| Avatar upload | ✅ Live (`POST /users/me/avatar/upload-url`) | Upload service, progress UI, MinIO-backed URL persistence |
| ML signals | ✅ Live | `POST /ml/churn-risk`, `POST /ml/match-quality` consumption in `ml_signal_service.dart` |
| Admin security UI | ✅ Live | Error envelope normalization, dead-letter list, audit security page. See `docs/frontend_admin_security_rollout_plan.md` |
| Sprint 2 networking layer | N/A | Copy 4 files into `lib/core/networking/`, add `web_socket_channel` + `uuid` to pubspec, add 3 Riverpod providers |

---

## Part 5 — Items Still Pending on Backend

These are **not yet live** — do not wire real calls:

| Item | Status | ETA |
|------|--------|-----|
| `GET /v1/assets/audio/{category}/{filename}` | Not started | TBD |
| Premium DB-backed catalog (Phase 2 growth) | Deferred | Post-launch |
| Admin dashboard Wave B — Questions, Events, Seasons | Not started | TBD |
| Admin dashboard Wave C — Moderation, Notifications, Economy, Anti-cheat | Not started | TBD |
| Backend Packet E — namespace rename | Intentionally deferred | Post-stable |

---

## Pending Database Migrations (DevOps action required)

The following migration files exist in the repo but have **not been applied** to the running database. A developer with database access must run:

```bash
dotnet ef database update \
  --project Tycoon.Backend.Migrations \
  --startup-project Tycoon.Backend.Api
```

This will apply (in order):
1. `20260425120000_AddSeasonRewardRules`
2. `20260425130000_AddStoreStockSystem`
3. `20260425140000_AddFlashSale`
4. `20260426100000_AddRewardClaimRule`
5. `20260426110000_AddEffectiveMaxQuantity`

The store stock and admin store P2 endpoints will return errors or empty results until migrations 2–5 are applied.

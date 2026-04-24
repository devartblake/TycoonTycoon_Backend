# Store & Admin Store Backend API Handoff

> **Audience:** Backend team
> **Date:** 2026-04-23
> **Base URL:** `http(s)://<host>:5000`
> **OpenAPI docs:** `/swagger` (dev only)
> **Design reference:** `docs/store_stock_backend_implementation_and_schema.md`

---

## Overview

This document covers the **next phase of store backend work**: player-scoped stock, daily limits, personalization hooks, and admin tooling. The currently implemented store endpoints are listed below as a baseline. All new endpoints build on top of this foundation.

---

## Currently Implemented Endpoints (Baseline)

| Method | Route | Auth | Status |
|--------|-------|------|--------|
| `GET` | `/store/catalog` | No | Live |
| `GET` | `/store/catalog/{sku}` | No | Live |
| `GET` | `/store/premium` | Yes | Live |
| `GET` | `/store/rewards/{playerId}` | Yes | Live |
| `POST` | `/store/rewards/{playerId}/claim/{rewardId}` | Yes | Live |
| `GET` | `/store/system/status` | No | Live |
| `GET` | `/store/inventory/{playerId}` | Yes | Live |
| `GET` | `/store/subscription/status/{playerId}` | Yes | Live |
| `POST` | `/store/subscription/activate` | Yes | Live |
| `POST` | `/store/subscription/checkout/session` | Yes | Live |
| `POST` | `/store/subscription/portal/session` | Yes | Live |
| `POST` | `/store/subscription/paypal/create` | Yes | Live |
| `POST` | `/store/subscription/paypal/cancel` | Yes | Live |
| `POST` | `/store/purchase` | Yes | Live |
| `POST` | `/store/payments/checkout/session` | Yes | Live |
| `POST` | `/store/payments/paypal/order` | Yes | Live |
| `POST` | `/store/payments/paypal/capture` | Yes | Live |
| `POST` | `/store/payments/webhook` | No | Live |
| `POST` | `/store/payments/paypal/webhook` | No | Live |
| `POST` | `/store/iap/validate` | Yes | Live |
| `GET` | `/store/avatars/{avatarId}` | Yes | Live |
| `POST` | `/store/avatars/{avatarId}/purchase` | Yes | Live |
| `GET` | `/v1/assets/avatars/{avatarId}` | Yes | Live |

---

## Error Envelope

All error responses use the nested envelope format:

```json
{
  "error": {
    "code": "error_code_snake_case",
    "message": "Human-readable message.",
    "details": {}
  }
}
```

---

## P0 — Daily Store + Stock Enforcement

### Goal

Add a daily store surface and enforce stock limits in the existing `POST /store/purchase` flow.

### P0.1 — `GET /store/daily`

Returns the daily rotating store items for the authenticated player.

#### Auth
Required (`Authorization: Bearer <jwt>`)

#### Response `200`

```json
{
  "playerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "generatedAt": "2026-04-23T00:00:00Z",
  "resetsAt": "2026-04-24T00:00:00Z",
  "items": [
    {
      "sku": "powerup:skip",
      "name": "Question Skip",
      "description": "Skip any question once.",
      "itemType": "powerup",
      "priceCoins": 50,
      "priceDiamonds": 0,
      "remainingQuantity": 3,
      "maxQuantity": 5,
      "resetInterval": "daily",
      "soldOut": false,
      "discountPercent": 0
    }
  ]
}
```

#### Implementation notes

- Filter `StoreItem` where `IsActive && ItemType` is in the daily rotation set.
- Stock state from `PlayerStoreStockState` (create row on first access).
- Lazy reset: if `NextResetAtUtc <= UtcNow`, reset `QuantityUsed = 0` and recalculate `NextResetAtUtc`.
- Return `resetsAt` = next midnight UTC.

### P0.2 — Stock enforcement in `POST /store/purchase`

Extend the existing purchase handler to check and decrement `PlayerStoreStockState` before completing a transaction.

#### New error codes

| Code | HTTP | Condition |
|------|------|-----------|
| `store_item_out_of_stock` | 409 | `remainingQuantity < quantity` |
| `store_item_unavailable` | 409 | Outside availability window |

#### Request (unchanged)

```json
{
  "playerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "sku": "powerup:skip",
  "quantity": 1,
  "clientRequestId": "b64c5408-d769-4b4a-9209-801f36b0f61d"
}
```

#### Response `200` (augmented)

```json
{
  "success": true,
  "transactionId": "txn_001",
  "sku": "powerup:skip",
  "quantityPurchased": 1,
  "remainingQuantity": 2,
  "nextResetAt": "2026-04-24T00:00:00Z",
  "wallet": {
    "coins": 1200,
    "gems": 50
  }
}
```

---

## P1 — Player-Specific Catalog + Hub Surface

### P1.1 — `GET /store/catalog/{playerId}`

Returns the full store catalog resolved for a specific player, including stock state, availability, and optional personalization.

#### Auth
Required (`Authorization: Bearer <jwt>`); JWT `playerId` must match path `playerId`.

#### Query Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `itemType` | string | (all) | Filter by item type |
| `category` | string | (all) | Filter by category prefix (`avatar`, `powerup`, etc.) |

#### Response `200`

```json
{
  "playerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "generatedAt": "2026-04-23T14:00:00Z",
  "items": [
    {
      "sku": "powerup:skip",
      "name": "Question Skip",
      "description": "Skip any question once.",
      "itemType": "powerup",
      "priceCoins": 50,
      "priceDiamonds": 0,
      "isAvailable": true,
      "remainingQuantity": 3,
      "maxQuantity": 5,
      "resetInterval": "daily",
      "lastResetAt": "2026-04-23T00:00:00Z",
      "nextResetAt": "2026-04-24T00:00:00Z",
      "soldOut": false,
      "discountPercent": 0,
      "owned": false,
      "availabilityState": "available",
      "stockState": "in_stock"
    }
  ]
}
```

#### `availabilityState` values

| Value | Meaning |
|-------|---------|
| `available` | Item is purchasable |
| `sold_out` | Player has used all stock for this interval |
| `outside_window` | Item is not in its availability window |
| `premium_required` | Player does not have premium |
| `already_owned` | One-time item already owned |

#### `stockState` values

| Value | Meaning |
|-------|---------|
| `in_stock` | Quantity available |
| `low_stock` | 1 remaining |
| `out_of_stock` | 0 remaining |
| `unlimited` | No stock tracking |

### P1.2 — `GET /store/hub`

Returns the store hub content for the authenticated player: featured items, categories, and daily highlights.

#### Auth
Required

#### Response `200`

```json
{
  "featured": [...],
  "daily": [...],
  "categories": ["powerup", "avatar", "currency", "cosmetic"]
}
```

### P1.3 — `GET /store/special-offers`

Returns active flash sales and limited-time offers for the player.

#### Auth
Required

#### Response `200`

```json
{
  "offers": [
    {
      "sku": "coin_pack_large",
      "name": "Weekend Bundle",
      "originalPriceCoins": 500,
      "salePriceCoins": 350,
      "discountPercent": 30,
      "endsAt": "2026-04-25T23:59:59Z"
    }
  ]
}
```

---

## P2 — Admin Store Management

All admin routes require `RequireAuthorization("Admin")` or an appropriate admin role claim.

### P2.1 — Stock Policy Management

#### `GET /admin/store/stock-policies`

Returns all active stock policies.

#### `PUT /admin/store/stock-policies/{sku}`

Upserts the stock policy for a catalog item.

##### Request

```json
{
  "policyType": "per_user",
  "maxQuantity": 5,
  "resetInterval": "daily",
  "isResetEnabled": true,
  "allowDynamicOverride": true
}
```

#### `POST /admin/store/stock-policies/bulk-reset`

Force-resets stock state for all players for one or more SKUs.

##### Request

```json
{
  "skus": ["powerup:skip", "powerup:hint"],
  "reason": "Manual weekly reset"
}
```

### P2.2 — Per-Player Stock Management

#### `GET /admin/store/player-stock/{playerId}`

Returns all stock state rows for a player.

#### `POST /admin/store/player-stock/{playerId}/override`

Applies an admin quantity override for a specific SKU.

##### Request

```json
{
  "sku": "powerup:skip",
  "effectiveMaxQuantity": 10,
  "reason": "Support override for player complaint"
}
```

### P2.3 — Flash Sale Management

#### `GET /admin/store/flash-sales`

Returns all active and scheduled flash sales.

#### `POST /admin/store/flash-sales`

Creates a new flash sale.

##### Request

```json
{
  "sku": "coin_pack_large",
  "discountPercent": 30,
  "startsAt": "2026-04-25T00:00:00Z",
  "endsAt": "2026-04-25T23:59:59Z",
  "reason": "Weekend promo"
}
```

#### `DELETE /admin/store/flash-sales/{id}`

Cancels a flash sale early.

### P2.4 — Reward Limit Management

#### `GET /admin/store/reward-limits`

Returns all reward claim rules.

#### `PUT /admin/store/reward-limits/{rewardId}`

Updates the claim interval and max claims for a reward type.

##### Request

```json
{
  "maxClaimsPerInterval": 3,
  "resetInterval": "daily"
}
```

### P2.5 — Store Analytics

#### `GET /admin/store/analytics/purchases`

Returns aggregate purchase stats.

##### Query parameters

| Parameter | Type | Description |
|-----------|------|-------------|
| `from` | ISO date | Start of range |
| `to` | ISO date | End of range |
| `sku` | string | Filter by SKU |
| `playerId` | Guid | Filter by player |

##### Response `200`

```json
{
  "totalPurchases": 1423,
  "totalRevenue": { "coins": 128500, "gems": 3200 },
  "topSkus": [
    { "sku": "powerup:skip", "purchases": 487, "revenueCoins": 24350 }
  ]
}
```

#### `GET /admin/store/analytics/stock-resets`

Returns a history of stock resets (lazy and manual).

---

## Domain Entities Required

These entities need to be created as EF Core entities before P0 can be implemented. Full C# class definitions are in `docs/store_stock_backend_implementation_and_schema.md` §6.2.

| Entity | Table | Used in |
|--------|-------|---------|
| `StoreStockPolicy` | `store_stock_policies` | P0, P1, P2.1 |
| `PlayerStoreStockState` | `player_store_stock_states` | P0, P1, P2.2 |
| `PlayerStorePurchase` | `player_store_purchases` | P0 extended purchase |
| `PlayerStoreInventoryItem` | `player_store_inventory_items` | P0 extended purchase |
| `FlashSale` | `store_flash_sales` | P1.3, P2.3 |

---

## EF Migration Plan

```bash
# After adding all entities and EF configurations:
dotnet ef migrations add AddStoreStockSystem \
  --project Tycoon.Backend.Migrations \
  --startup-project Tycoon.Backend.Api

dotnet ef database update \
  --project Tycoon.Backend.Migrations \
  --startup-project Tycoon.Backend.Api
```

---

## Implementation Priority

| Priority | Endpoint | Blocking? |
|----------|----------|-----------|
| P0 | `GET /store/daily` | Flutter daily store screen |
| P0 | Stock enforcement in `POST /store/purchase` | Purchase correctness |
| P1 | `GET /store/catalog/{playerId}` | Flutter hub + catalog personalization |
| P1 | `GET /store/hub` | Flutter store hub screen |
| P1 | `GET /store/special-offers` | Flutter offers surface |
| P2 | Admin stock policies | Operator tuning |
| P2 | Admin flash sales | Marketing campaigns |
| P2 | Admin reward limits | Reward balancing |
| P2 | Admin analytics | Business intelligence |

---

## Open Questions

1. **Stock policy defaults** — Should new catalog items default to `unlimited` or `per_user: 1` per day? Recommendation: `unlimited` with opt-in stock policies.
2. **Flash sale storage** — Store flash sales in DB (recommended for operator tooling) or config only?
3. **Personalization integration** — FastAPI `POST /ml/store-personalization` is already designed (see `store_stock_backend_implementation_and_schema.md` §6.7). Enable now or leave as P3?
4. **`playerStoreStockState` initialization** — Create rows lazily on first read (recommended) or eagerly on catalog load?
5. **`SeasonRewardRule` EF migration** — The `season_reward_rules` table does not yet have an EF migration. Run `dotnet ef migrations add AddSeasonRewardRules` before the next migration adds stock entities.

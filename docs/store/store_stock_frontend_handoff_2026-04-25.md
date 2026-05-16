# Store Stock System — Frontend Handoff

> **Audience:** Flutter frontend team
> **Date:** 2026-04-25
> **Base URL:** `http(s)://<host>:5000`
> **OpenAPI docs:** `/swagger` (dev only)
> **Design reference:** `docs/store_stock_backend_implementation_and_schema.md`

---

## Overview

This document covers all **P0 and P1 store stock endpoints** now live on the backend. These endpoints
replace or supplement the existing `GET /store/catalog` (which remains unchanged for anonymous access)
with player-aware, stock-enforced surfaces.

### What changed in `POST /store/purchase`

The existing purchase endpoint now enforces stock limits. If a player has exhausted their daily quota
for an item, the response is `409 store_item_out_of_stock`. No other behaviour changed.

---

## Authentication

All endpoints in this document require a valid bearer token:

```
Authorization: Bearer <jwt>
```

Unauthenticated requests receive `401 UNAUTHORIZED`.

---

## Error Envelope

All error responses use the existing nested format:

```json
{
  "error": {
    "code": "error_code_snake_case",
    "message": "Human-readable message."
  }
}
```

---

## Stock Concepts

### `availabilityState`

| Value | Meaning |
|-------|---------|
| `available` | Item is purchasable now |
| `sold_out` | Player has used their quota for this reset interval |
| `already_owned` | One-time item (`maxPerPlayer == 1`) already purchased |

### `stockState`

| Value | Meaning |
|-------|---------|
| `unlimited` | No stock policy — no limit |
| `in_stock` | ≥ 2 remaining |
| `low_stock` | Exactly 1 remaining |
| `out_of_stock` | 0 remaining |

### `remainingQuantity`

`-1` means unlimited (no stock policy or `maxQuantity == -1`).

### `resetInterval`

| Value | Resets when |
|-------|------------|
| `"daily"` | Next midnight UTC |
| `"weekly"` | 7 days from start of current UTC day |
| `"none"` | No automatic reset |

---

## P0 — Endpoints

### `GET /store/daily`

Returns all store items that have a stock policy, enriched with the calling player's current stock state.

**Auth:** Required

**Response `200`**

```json
{
  "generatedAt": "2026-04-25T14:00:00Z",
  "resetsAt":    "2026-04-26T00:00:00Z",
  "items": [
    {
      "sku":               "powerup:skip",
      "name":              "Question Skip",
      "description":       "Skip any question once.",
      "itemType":          "powerup",
      "priceCoins":        50,
      "priceDiamonds":     0,
      "remainingQuantity": 3,
      "maxQuantity":       5,
      "resetInterval":     "daily",
      "soldOut":           false,
      "discountPercent":   0,
      "nextResetAt":       "2026-04-26T00:00:00Z"
    }
  ]
}
```

**Flutter call site:**

```dart
final response = await _apiService.get('/store/daily');
final resetsAt = DateTime.parse(response['resetsAt'] as String);
final rawItems = response['items'] as List<dynamic>;
final items = rawItems.map((e) => DailyStoreItem.fromJson(e)).toList();
```

**Flutter model:**

```dart
class DailyStoreItem {
  final String sku;
  final String name;
  final String? description;
  final String itemType;
  final int priceCoins;
  final int priceDiamonds;
  final int remainingQuantity; // -1 = unlimited
  final int maxQuantity;       // -1 = unlimited
  final String resetInterval;
  final bool soldOut;
  final int discountPercent;
  final DateTime? nextResetAt;
}
```

---

### `POST /store/purchase` — stock enforcement added

The existing endpoint now returns `409 store_item_out_of_stock` when a player has exhausted
their stock quota for the current interval.

**New error code:**

| Code | HTTP | When |
|------|------|------|
| `store_item_out_of_stock` | 409 | Player's daily/weekly quota exhausted |

**Existing error codes remain unchanged:**

| Code | HTTP | When |
|------|------|------|
| `PURCHASE_LIMIT` | 409 | `MaxPerPlayer` one-time cap reached |
| `NOT_FOUND` | 404 | SKU not found or inactive |
| `INVALID_CURRENCY` | 400 | Currency not `coins` or `diamonds` |
| `CURRENCY_NOT_ACCEPTED` | 400 | Item not priced in chosen currency |

**Flutter handling:**

```dart
try {
  final result = await _apiService.post('/store/purchase', body: {
    'playerId': playerId.toString(),
    'sku': sku,
    'quantity': 1,
    'currency': 'coins',
  });
  // success
} on ApiException catch (e) {
  switch (e.code) {
    case 'store_item_out_of_stock':
      showStockExhaustedDialog(nextResetAt: dailyItem.nextResetAt);
    case 'PURCHASE_LIMIT':
      showAlreadyOwnedDialog();
    case 'InsufficientFunds':
      showInsufficientFundsDialog();
  }
}
```

---

## P1 — Endpoints

### `GET /store/catalog/{playerId}`

Returns the full store catalog resolved for a specific player. Enriches every item with:
- per-player stock state (remaining quantity, reset times)
- ownership flag (purchased at least once)
- active flash-sale discount
- `availabilityState` and `stockState` convenience fields

**Auth:** Required. JWT `playerId` must match the path `{playerId}` — returns `403 FORBIDDEN` otherwise.

**Query parameters:**

| Parameter | Type | Description |
|-----------|------|-------------|
| `itemType` | string | Filter by exact item type (e.g. `powerup`, `avatar`) |
| `category` | string | Filter by category prefix (e.g. `avatar` matches `avatar:*` and `avatar`) |

**Response `200`**

```json
{
  "playerId":    "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "generatedAt": "2026-04-25T14:00:00Z",
  "items": [
    {
      "sku":               "powerup:skip",
      "name":              "Question Skip",
      "description":       "Skip any question once.",
      "itemType":          "powerup",
      "priceCoins":        50,
      "priceDiamonds":     0,
      "isAvailable":       true,
      "remainingQuantity": 3,
      "maxQuantity":       5,
      "resetInterval":     "daily",
      "lastResetAt":       "2026-04-25T00:00:00Z",
      "nextResetAt":       "2026-04-26T00:00:00Z",
      "soldOut":           false,
      "discountPercent":   0,
      "owned":             false,
      "availabilityState": "available",
      "stockState":        "in_stock",
      "thumbnailUrl":      null,
      "isFeatured":        false
    },
    {
      "sku":               "avatar:cartoon-hero:v1",
      "name":              "Cartoon Hero",
      "description":       "A bold cartoon-style 3D avatar.",
      "itemType":          "avatar",
      "priceCoins":        500,
      "priceDiamonds":     0,
      "isAvailable":       false,
      "remainingQuantity": -1,
      "maxQuantity":       -1,
      "resetInterval":     null,
      "lastResetAt":       null,
      "nextResetAt":       null,
      "soldOut":           false,
      "discountPercent":   0,
      "owned":             true,
      "availabilityState": "already_owned",
      "stockState":        "unlimited",
      "thumbnailUrl":      "https://cdn.example.com/avatars/cartoon-hero-thumb.png",
      "isFeatured":        true
    }
  ]
}
```

**Flutter call site:**

```dart
final response = await _apiService.get(
  '/store/catalog/${playerId.toString()}',
  queryParameters: {'itemType': 'powerup'},
);
final catalog = PlayerStoreCatalog.fromJson(response);
```

**Flutter models:**

```dart
class PlayerStoreCatalog {
  final String playerId;
  final DateTime generatedAt;
  final List<PlayerStoreCatalogItem> items;
}

class PlayerStoreCatalogItem {
  final String sku;
  final String name;
  final String? description;
  final String itemType;
  final int priceCoins;
  final int priceDiamonds;
  final bool isAvailable;
  final int remainingQuantity; // -1 = unlimited
  final int maxQuantity;       // -1 = unlimited
  final String? resetInterval;
  final DateTime? lastResetAt;
  final DateTime? nextResetAt;
  final bool soldOut;
  final int discountPercent;
  final bool owned;
  final String availabilityState; // "available" | "sold_out" | "already_owned"
  final String stockState;        // "unlimited" | "in_stock" | "low_stock" | "out_of_stock"
  final String? thumbnailUrl;
  final bool isFeatured;
}
```

**UI guidance:**

| `availabilityState` | Buy button | Badge |
|--------------------|------------|-------|
| `available` + `in_stock` | Enabled | — |
| `available` + `low_stock` | Enabled | "Last 1!" |
| `sold_out` | Disabled | "Daily limit reached — resets {nextResetAt}" |
| `already_owned` | Hidden / "Equipped" | "Owned" |

---

### `GET /store/hub`

Returns the store hub surface: featured items (enriched), daily stock items, and the category list.

**Auth:** Required

**Response `200`**

```json
{
  "featured": [
    {
      "sku":               "avatar:cartoon-hero:v1",
      "isFeatured":        true,
      "availabilityState": "available",
      "stockState":        "unlimited",
      ...
    }
  ],
  "daily": [
    {
      "sku":               "powerup:skip",
      "remainingQuantity": 3,
      "soldOut":           false,
      "nextResetAt":       "2026-04-26T00:00:00Z",
      ...
    }
  ],
  "categories": ["avatar", "cosmetic", "powerup"]
}
```

**Flutter call site:**

```dart
final response = await _apiService.get('/store/hub');
final hub = StoreHub.fromJson(response);
```

**Flutter models:**

```dart
class StoreHub {
  final List<PlayerStoreCatalogItem> featured;
  final List<DailyStoreItem> daily;
  final List<String> categories;
}
```

**UI usage:** Render featured items in a horizontal carousel at the top of the store screen.
Render `daily` items below with countdown timers driven by `nextResetAt`. Use `categories` to
populate the category filter tabs.

---

### `GET /store/special-offers`

Returns currently active flash sales joined with their catalog items. Includes the original price,
sale price, discount percent, and sale end time.

**Auth:** Required

**Response `200`**

```json
{
  "offers": [
    {
      "sku":               "powerup:hint",
      "name":              "Hint Pack",
      "description":       "Get a hint on any question.",
      "originalPriceCoins": 200,
      "salePriceCoins":     140,
      "discountPercent":    30,
      "endsAt":            "2026-04-27T23:59:59Z"
    }
  ]
}
```

`"offers"` is an empty array `[]` when no flash sales are active.

**Flutter call site:**

```dart
final response = await _apiService.get('/store/special-offers');
final offers = (response['offers'] as List<dynamic>)
    .map((e) => SpecialOffer.fromJson(e))
    .toList();
```

**Flutter model:**

```dart
class SpecialOffer {
  final String sku;
  final String name;
  final String? description;
  final int originalPriceCoins;
  final int salePriceCoins;
  final int discountPercent;
  final DateTime endsAt;
}
```

**UI guidance:** Show a "SALE" badge and a countdown timer using `endsAt`. Strike through
`originalPriceCoins` and display `salePriceCoins` prominently.

---

## Loading Strategy

Recommended load order for the store screen:

```dart
// Parallel load — do not await individually
final hubFuture    = _storeService.getHub();
final offersFuture = _storeService.getSpecialOffers();

final results = await Future.wait([hubFuture, offersFuture]);
final hub    = results[0] as StoreHub;
final offers = results[1] as List<SpecialOffer>;
```

When the player taps a category tab, call `GET /store/catalog/{playerId}?itemType={category}` to
get a filtered, stock-aware view for that category only.

---

## Seeding Stock Policies for Testing

To make items appear in `GET /store/daily` and the stock-aware catalog, insert a row into
`store_stock_policies` via the MinIO seed file or directly in the database:

**`seeds/store-stock-policies.json`** (upload via `mc cp`):

```json
[
  { "sku": "powerup:skip",  "maxQuantityPerUser": 5, "resetInterval": "daily" },
  { "sku": "powerup:hint",  "maxQuantityPerUser": 3, "resetInterval": "daily" },
  { "sku": "powerup:extra-time", "maxQuantityPerUser": 2, "resetInterval": "daily" }
]
```

Items without a policy row in `store_stock_policies` still appear in `GET /store/catalog/{playerId}`
with `stockState: "unlimited"` and `remainingQuantity: -1`. They do **not** appear in `GET /store/daily`.

---

## Seeding Flash Sales

Insert directly into `flash_sales` table via migration seed or admin tooling (P2):

```sql
INSERT INTO flash_sales (id, sku, discount_percent, starts_at_utc, ends_at_utc, is_active, reason, created_at_utc)
VALUES (
  gen_random_uuid(),
  'powerup:hint',
  30,
  '2026-04-25 00:00:00+00',
  '2026-04-27 23:59:59+00',
  true,
  'Weekend promo',
  now()
);
```

---

## Complete New Endpoint Summary

| Method | Route | Auth | P-Level | Description |
|--------|-------|------|---------|-------------|
| `GET` | `/store/daily` | Yes | P0 | Daily stock items for the calling player |
| `GET` | `/store/catalog/{playerId}` | Yes (self-only) | P1 | Full catalog with stock state, ownership, discounts |
| `GET` | `/store/hub` | Yes | P1 | Hub: featured items + daily + categories |
| `GET` | `/store/special-offers` | Yes | P1 | Active flash sales with sale price |

`POST /store/purchase` is unchanged in its interface — it now additionally enforces stock limits.

---

## Open Questions / Remaining P2 Work

| Item | Owner |
|------|-------|
| Admin UI to create/cancel flash sales (`POST /admin/store/flash-sales`) | Backend P2 |
| Admin UI to set/update stock policies (`PUT /admin/store/stock-policies/{sku}`) | Backend P2 |
| Admin override of per-player stock (`POST /admin/store/player-stock/{playerId}/override`) | Backend P2 |
| FastAPI personalization hook in `GetCatalogForPlayerAsync` | Backend P2 / optional |
| Stock policy seeder support in `MinioSeeder` | Backend |
| Countdown timer component for daily reset in Flutter | Frontend |
| "Sold out — resets in X" UI state in store items | Frontend |
| Flash sale badge + strikethrough price component | Frontend |

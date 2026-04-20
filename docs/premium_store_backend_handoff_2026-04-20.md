# Premium Store — Backend API Handoff

> **Audience:** Backend team  
> **Date:** 2026-04-20  
> **Flutter branch:** `claude/fix-nullability-warnings-aszut`  
> **Base URL:** `http(s)://<host>:5000`  
> **OpenAPI docs:** `/swagger` (dev only)

---

## Overview

The Premium Store (`StoreSecondaryScreen`) is the exclusive content hub of the game. It surfaces four sections:

| Section | Content type | Data source needed |
|---|---|---|
| **Remove Ads** | Ad-removal subscription plans | `GET /store/premium` |
| **3D Avatar** | Static widget (local asset) | None — no API needed |
| **Special Offers** | Flash sale with countdown | `GET /store/premium` |
| **Reward Center** | Daily player rewards | `GET /store/rewards/{playerId}` + `POST /store/rewards/{playerId}/claim/{rewardId}` |

The Flutter models, fallback data, and provider wiring are already in place. The app renders correctly using hardcoded fallbacks today. These endpoints replace that fallback data with live, player-aware content.

---

## Screen → Endpoint Mapping

| Flutter file | Endpoint | Method |
|---|---|---|
| `premium_store.dart` (whole screen) | `/store/premium` | `GET` |
| `reward_center.dart` (player state) | `/store/rewards/{playerId}` | `GET` |
| `reward_center.dart` (claim button) | `/store/rewards/{playerId}/claim/{rewardId}` | `POST` |
| `ad_remove_options.dart` (purchase) | Routes to `/offers` checkout — **no new endpoint needed** |
| `sale_info.dart` (purchase) | Routes through existing subscription checkout — **no new endpoint needed** |

---

## Endpoint 1 — `GET /store/premium`

Returns the non-player-specific catalog for the premium store: ad-removal plans and the current flash sale. This response is the **same for all players** and can be cached aggressively (suggested TTL: 15 minutes).

**Auth:** Valid session token required (standard Bearer header).

**Query params:** None.

**Response `200 OK`:**

```json
{
  "adFree": {
    "plans": [
      {
        "id": "ad-free-365",
        "durationLabel": "365 DAYS",
        "price": "$5.99",
        "badge": "Best Value - Save 70%",
        "accentColor": "#10B981",
        "isBestValue": true
      },
      {
        "id": "ad-free-28",
        "durationLabel": "28 DAYS",
        "price": "$3.99",
        "badge": "Popular Choice",
        "accentColor": "#6366F1",
        "isBestValue": false
      },
      {
        "id": "ad-free-7",
        "durationLabel": "7 DAYS",
        "price": "$1.99",
        "badge": "Trial Period",
        "accentColor": "#8B5CF6",
        "isBestValue": false
      }
    ],
    "benefits": [
      "Uninterrupted gameplay",
      "Faster loading times",
      "Less battery usage",
      "Premium experience"
    ]
  },
  "saleInfo": {
    "badgeText": "FLASH SALE",
    "discount": "80% OFF",
    "originalPrice": "$10",
    "salePrice": "$1.99",
    "expiresAt": "2026-04-21T12:00:00Z",
    "buttonText": "Claim This Deal",
    "benefits": [
      {
        "icon": "verified",
        "value": "5",
        "label": "Premium\nFeatures",
        "color": "#10B981"
      },
      {
        "icon": "monetization_on",
        "value": "3400",
        "label": "Bonus\nCoins",
        "color": "#F59E0B"
      },
      {
        "icon": "confirmation_number",
        "value": "400",
        "label": "Special\nTickets",
        "color": "#8B5CF6"
      }
    ]
  },
  "rewardCenter": {
    "cards": [
      {
        "id": "daily-checkin",
        "title": "Daily Check-in",
        "subtitle": "Day 1 of 7",
        "gradient": ["#10B981", "#059669"],
        "reward": "500 Coins",
        "progress": null,
        "isAvailable": true
      },
      {
        "id": "watch-ad",
        "title": "Watch Ad",
        "subtitle": "3 available today",
        "gradient": ["#8B5CF6", "#7C3AED"],
        "reward": "200 Coins",
        "progress": null,
        "isAvailable": true
      }
    ],
    "completedCount": 0,
    "totalCount": 2
  }
}
```

> **Important:** `saleInfo` must be `null` (not omitted — explicitly `null`) when no flash sale is currently active. The Flutter screen conditionally hides the Special Offers section when this field is `null`.

### Field Reference — `adFree`

| Field | Type | Description |
|---|---|---|
| `plans` | `array` | Ordered list of ad-removal plans; rendered top-to-bottom (first plan is full-width, rest in a row) |
| `plans[].id` | `string` | Stable identifier used for purchase routing |
| `plans[].durationLabel` | `string` | Display string shown on the card (e.g. `"365 DAYS"`) |
| `plans[].price` | `string` | Display price string including currency symbol (e.g. `"$5.99"`) |
| `plans[].badge` | `string` | Short label on the coloured badge chip |
| `plans[].accentColor` | `string` | Hex colour for border and button (e.g. `"#10B981"`) |
| `plans[].isBestValue` | `bool` | When `true`, card renders full-width with a thicker green border |
| `benefits` | `array<string>` | Bullet points shown in the "What you get with ad-free" box |

### Field Reference — `saleInfo`

| Field | Type | Description |
|---|---|---|
| `badgeText` | `string` | Text inside the top badge chip (e.g. `"FLASH SALE"`) |
| `discount` | `string` | Large headline discount string (e.g. `"80% OFF"`) |
| `originalPrice` | `string` | Struck-through original price (e.g. `"$10"`) |
| `salePrice` | `string` | Highlighted sale price (e.g. `"$1.99"`) |
| `expiresAt` | `string (ISO 8601)` | UTC expiry datetime; Flutter renders a countdown from this |
| `buttonText` | `string` | CTA button label (default: `"Claim This Deal"`) |
| `benefits[].icon` | `string` | Icon name resolved by Flutter's `resolveIcon()` helper — see icon name table below |
| `benefits[].value` | `string` | Large number shown on the benefit tile (e.g. `"3400"`) |
| `benefits[].label` | `string` | Small label below the number; use `\n` for line breaks |
| `benefits[].color` | `string` | Hex colour for the benefit tile border/tint |

### Field Reference — `rewardCenter` (non-player version)

> **Note:** This section within `GET /store/premium` returns the **catalog definition** of reward cards — the tile structure, gradient, and reward amounts. It does **not** contain player-specific state (`isAvailable`, `progress`, `subtitle`). Those come from `GET /store/rewards/{playerId}` (Endpoint 2). Until Endpoint 2 is implemented, include player-state fields here as a temporary measure using static defaults.

| Field | Type | Description |
|---|---|---|
| `cards[].id` | `string` | Stable reward identifier (`"daily-checkin"`, `"watch-ad"`) |
| `cards[].title` | `string` | Card heading |
| `cards[].subtitle` | `string` | Secondary line — player-specific when coming from Endpoint 2 |
| `cards[].gradient` | `array<string>` | Two hex colours for the card's gradient |
| `cards[].reward` | `string` | Reward label shown on the chip (e.g. `"500 Coins"`) |
| `cards[].progress` | `number \| null` | Float `0.0–1.0` for the progress bar; `null` hides it |
| `cards[].isAvailable` | `bool` | `true` = Claim button is active; `false` = greyed out |
| `completedCount` | `int` | Number of rewards claimed today (shown in progress summary) |
| `totalCount` | `int` | Total rewards available today |

### Valid Icon Names (for `saleInfo.benefits[].icon`)

Flutter's `resolveIcon()` maps these strings to `Icons.*`:

| String | Flutter icon |
|---|---|
| `"store"` | `Icons.store` |
| `"local_offer"` | `Icons.local_offer` |
| `"card_giftcard"` | `Icons.card_giftcard` |
| `"workspace_premium"` | `Icons.workspace_premium` |
| `"star"` | `Icons.star` |
| `"monetization_on"` | `Icons.monetization_on` |
| `"flash_on"` | `Icons.flash_on` |
| `"trending_up"` | `Icons.trending_up` |
| `"diamond"` | `Icons.diamond` |
| `"emoji_events"` | `Icons.emoji_events` |
| `"auto_awesome"` | `Icons.auto_awesome` |
| `"storefront"` | `Icons.storefront` |
| `"favorite"` | `Icons.favorite` |
| `"auto_fix_high"` | `Icons.auto_fix_high` |
| `"bolt"` | `Icons.bolt` |
| `"local_fire_department"` | `Icons.local_fire_department` |
| `"verified"` | `Icons.verified` |
| `"confirmation_number"` | `Icons.confirmation_number` |

Any unrecognised string falls back to `Icons.star`.

---

## Endpoint 2 — `GET /store/rewards/{playerId}`

Returns the **player-specific** reward state for today. This response differs per player and should not be cached (or use a very short TTL, e.g. 30 seconds).

**Auth:** Valid session token required. The authenticated player must match `{playerId}` — reject with `403` otherwise.

**Path params:**

| Param | Type | Description |
|---|---|---|
| `playerId` | `uuid` | The player's unique ID |

**Response `200 OK`:**

```json
{
  "cards": [
    {
      "id": "daily-checkin",
      "title": "Daily Check-in",
      "subtitle": "Day 3 of 7",
      "gradient": ["#10B981", "#059669"],
      "reward": "500 Coins",
      "progress": 0.43,
      "isAvailable": true
    },
    {
      "id": "watch-ad",
      "title": "Watch Ad",
      "subtitle": "2 available today",
      "gradient": ["#8B5CF6", "#7C3AED"],
      "reward": "200 Coins",
      "progress": null,
      "isAvailable": true
    }
  ],
  "completedCount": 1,
  "totalCount": 2
}
```

### Player-state field details

| Field | How to compute |
|---|---|
| `daily-checkin.subtitle` | `"Day {currentStreak} of 7"` — resets to Day 1 if missed yesterday |
| `daily-checkin.progress` | `currentStreak / 7.0` — e.g. Day 3 → `0.43` |
| `daily-checkin.isAvailable` | `true` if the player has not yet claimed today's check-in |
| `watch-ad.subtitle` | `"{remainingToday} available today"` — daily cap configured server-side (default: 3) |
| `watch-ad.isAvailable` | `true` if `remainingToday > 0` |
| `completedCount` | Count of reward IDs claimed today by this player |
| `totalCount` | Count of reward IDs available to this player today |

**Response `404 Not Found`:** Player ID does not exist.  
**Response `403 Forbidden`:** Authenticated user does not match `{playerId}`.

---

## Endpoint 3 — `POST /store/rewards/{playerId}/claim/{rewardId}`

Called when the player taps the **"Claim"** button on a reward card. Currently this button only shows a confirmation dialog — no coins are actually credited. This endpoint closes that gap.

**Auth:** Valid session token required. Authenticated player must match `{playerId}`.

**Path params:**

| Param | Type | Description |
|---|---|---|
| `playerId` | `uuid` | The player's unique ID |
| `rewardId` | `string` | Reward card ID from the cards array (e.g. `"daily-checkin"`, `"watch-ad"`) |

**Request body:** None.

**Response `200 OK`:**

```json
{
  "success": true,
  "rewardId": "daily-checkin",
  "coinsAwarded": 500,
  "newBalance": 1940,
  "nextAvailableAt": null
}
```

| Field | Type | Description |
|---|---|---|
| `success` | `bool` | Always `true` on a 200 response |
| `rewardId` | `string` | Echo of the claimed reward ID |
| `coinsAwarded` | `int` | Coins credited to the player's account |
| `newBalance` | `int` | Player's updated coin balance after the credit |
| `nextAvailableAt` | `string (ISO 8601) \| null` | When this reward becomes claimable again; `null` means "tomorrow at reset time" |

**Response `409 Conflict`:** Reward already claimed today.

```json
{
  "error": "already_claimed",
  "message": "This reward has already been claimed today.",
  "nextAvailableAt": "2026-04-21T00:00:00Z"
}
```

**Response `404 Not Found`:** Unknown `rewardId`.  
**Response `403 Forbidden`:** Player mismatch.

---

## Endpoints NOT Required

The following purchase actions in the Premium Store already route through existing checkout infrastructure — no new backend endpoints are needed:

| Action | How it's handled |
|---|---|
| Ad-removal plan purchase | Tapping any plan card redirects to `/offers` via `context.push('/offers')`. The Special Offers screen handles the full checkout flow via `_startSubscriptionCheckout()` with the existing `/store/subscription` endpoints. |
| Flash sale "Claim This Deal" | The `SaleInfo` widget shows a confirmation dialog. On confirm, this can route through the same `_startSubscriptionCheckout()` path in `OffersScreen`. Backend just needs to ensure the matching offer SKU exists in the offers catalog. |

---

## Flutter Integration Notes

These notes describe what the Flutter team will do once the backend endpoints are live. Included here so the backend team understands the client's expectations.

### Step 1 — Split `premiumStoreProvider` into two

Currently a single `FutureProvider<PremiumStoreData>` calls `GET /store/premium`. Once Endpoint 2 is live, `rewardCenter` data will move to a separate provider:

```dart
// Existing — stays as-is for adFree + saleInfo
final premiumStoreProvider = FutureProvider<PremiumStoreData>(...);

// New — player-specific reward state
final playerRewardsProvider = FutureProvider<RewardCenterData>((ref) async {
  final playerId = await ref.watch(currentUserIdProvider.future);
  return ref.read(storeServiceProvider).getPlayerRewards(playerId);
});
```

### Step 2 — Wire the claim button

`RewardCenter._handleRewardClaim()` will call `POST /store/rewards/{playerId}/claim/{rewardId}`, then:
1. Update `coinBalanceProvider` with the returned `newBalance`
2. Call `ref.invalidate(playerRewardsProvider)` to refresh the card states

### Step 3 — Expiry countdown in `SaleInfo`

`SaleInfoData.expiresAt` is already parsed to `DateTime?` in the Flutter model. The `SaleInfo` widget needs a `Timer` that ticks every second and formats `expiresAt.difference(DateTime.now())` into `HH:MM:SS`. The hardcoded `"23:45:12"` in `OffersScreen` will be replaced by the same pattern.

---

## Error Handling Contract

All endpoints should return errors in this shape (consistent with the rest of the API):

```json
{
  "error": "snake_case_code",
  "message": "Human-readable description"
}
```

Flutter catches any non-2xx response, logs it via `LogManager.debug()`, and falls back to the last known good state (or the static fallback if no prior data exists). No user-visible error is shown for catalog failures — the screen silently uses fallback data.

For claim failures (`409 Conflict`), the Flutter dialog will display the `message` field directly to the player.

---

## Suggested Implementation Order

| Priority | Endpoint | Reason |
|---|---|---|
| 1 | `GET /store/premium` | Unblocks ad-free plans + flash sale display; low complexity, no player state |
| 2 | `GET /store/rewards/{playerId}` | Makes check-in streak and ad-watch count live per player |
| 3 | `POST /store/rewards/{playerId}/claim/{rewardId}` | Closes the coin-credit gap; depends on reward state endpoint |

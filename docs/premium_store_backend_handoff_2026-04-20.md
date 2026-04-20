# Premium Store Backend API Handoff

> **Audience:** Frontend team  
> **Date:** 2026-04-20  
> **Base URL:** `http(s)://<host>:5000`  
> **OpenAPI docs:** `/swagger` (dev only)

---

## Status

The premium store backend baseline is now live and verified in the backend test suite.

Implemented routes:

- `GET /store/premium`
- `GET /store/rewards/{playerId}`
- `POST /store/rewards/{playerId}/claim/{rewardId}`

Validation run on **April 20, 2026**:

- `dotnet test Tycoon.Backend.Api.Tests\Tycoon.Backend.Api.Tests.csproj --no-build --no-restore --filter PremiumStoreEndpointsTests`
- Result: `Passed (9/9)`

### Alignment summary

Current alignment status with frontend is:

- premium catalog and reward flows: aligned
- premium reward claiming: aligned
- premium purchase CTA route: aligned

Reason:

- the Flutter premium purchase flow now launches the existing subscription endpoints directly from premium-store plan data
- the legacy `/offers` frontend route now redirects to `/store-premium`
- the backend does not need a dedicated `GET /store/offers` route for premium-store integration

So the premium-store baseline routes are correct, and the premium purchase journey is now aligned to the existing subscription route family.

---

## Route Matrix

| Flutter surface | Endpoint | Method | Status |
|---|---|---|---|
| `premium_store.dart` | `/store/premium` | `GET` | Implemented |
| `reward_center.dart` | `/store/rewards/{playerId}` | `GET` | Implemented |
| `reward_center.dart` claim action | `/store/rewards/{playerId}/claim/{rewardId}` | `POST` | Implemented |
| Ad-free purchase CTA | existing `/store/subscription/*` flows | mixed | Existing route family |
| Flash-sale purchase CTA | existing `/store/subscription/*` flows | mixed | Existing route family |

### Existing purchase routes already available

These were already in the store surface and remain the right purchase path:

- `GET /store/subscription/status/{playerId}`
- `POST /store/subscription/activate`
- `POST /store/subscription/checkout/session`
- `POST /store/subscription/portal/session`
- `POST /store/subscription/paypal/create`
- `POST /store/subscription/paypal/cancel`

### Purchase-routing conclusion

For premium purchases, the backend-supported route family is the subscription surface above.

There is currently **no** implemented backend route for:

- `GET /store/offers`

Important distinction:

- `/offers` remains only as a legacy **frontend navigation route**
- `GET /store/offers` would be a separate **backend API route**

The frontend no longer depends on that backend API route for premium purchase flow.

---

## Premium Purchase Routing

This section is the concrete backend routing map the frontend team should use for premium purchase flow.

### Recommended frontend routing model

Map premium plan selection directly from `/store/premium` plan data into the existing subscription checkout endpoints.

Current recommended mapping:

- `premium-monthly` or `sku: sub:premium:monthly`
  - `tier = premium`
  - `billingPeriod = monthly`
- `premium-seasonal` or `sku: sub:premium:seasonal`
  - `tier = premium`
  - `billingPeriod = seasonal`

If frontend later introduces elite-tier premium cards, those should map to:

- `tier = elite`
- `billingPeriod = monthly` or `seasonal`

based on the selected plan.

### Stripe checkout path

Endpoint:

- `POST /store/subscription/checkout/session`

Request DTO:

```json
{
  "playerId": "00000000-0000-0000-0000-000000000000",
  "tier": "premium",
  "billingPeriod": "monthly",
  "successUrl": "https://your-app.example/success",
  "cancelUrl": "https://your-app.example/cancel"
}
```

Request notes:

- `tier` must be `premium` or `elite`
- `billingPeriod` must be `monthly` or `seasonal`
- `playerId` must match the authenticated player
- `successUrl` and `cancelUrl` are optional if backend config already supplies them

Response DTO:

```json
{
  "sessionId": "cs_sub_test_123",
  "checkoutUrl": "https://checkout.stripe.com/...",
  "priceId": "price_premium_monthly",
  "tier": "premium",
  "billingPeriod": "monthly",
  "publishableKey": "pk_test_123"
}
```

Frontend action:

- open `checkoutUrl`
- after return, refresh `GET /store/subscription/status/{playerId}`

### Stripe customer portal path

Endpoint:

- `POST /store/subscription/portal/session`

Request DTO:

```json
{
  "playerId": "00000000-0000-0000-0000-000000000000",
  "returnUrl": "https://your-app.example/subscription"
}
```

Response DTO:

```json
{
  "sessionId": "bps_test_123",
  "url": "https://billing.stripe.com/..."
}
```

Use this for:

- manage subscription
- billing portal deep link
- cancellation/plan management from an account settings area

### PayPal subscription path

Endpoint:

- `POST /store/subscription/paypal/create`

Request DTO:

```json
{
  "playerId": "00000000-0000-0000-0000-000000000000",
  "tier": "premium",
  "billingPeriod": "monthly",
  "returnUrl": "https://your-app.example/paypal/return",
  "cancelUrl": "https://your-app.example/paypal/cancel"
}
```

Response DTO:

```json
{
  "subscriptionId": "I-TEST123",
  "status": "APPROVAL_PENDING",
  "approveUrl": "https://www.paypal.com/checkoutnow?...",
  "planId": "plan_premium_monthly",
  "tier": "premium",
  "billingPeriod": "monthly",
  "clientId": "paypal-client-id"
}
```

Frontend action:

- open `approveUrl`
- after return, refresh `GET /store/subscription/status/{playerId}`

### Subscription status path

Endpoint:

- `GET /store/subscription/status/{playerId}`

Response DTO:

```json
{
  "playerId": "00000000-0000-0000-0000-000000000000",
  "isActive": true,
  "tier": "premium",
  "billingPeriod": "monthly",
  "activatedAtUtc": "2026-04-20T15:00:00Z",
  "provider": "stripe",
  "providerSubscriptionId": "sub_123",
  "providerCustomerId": "cus_123",
  "providerStatus": "active",
  "stripeSubscriptionId": "sub_123",
  "stripeCustomerId": "cus_123",
  "stripeStatus": "active",
  "currentPeriodEndUtc": "2026-05-20T15:00:00Z",
  "cancelAtPeriodEnd": false
}
```

Use this as the post-checkout hydration endpoint for:

- active premium state
- provider status
- renewal/cancel state

### Current frontend behavior

The current Flutter app now follows this routing model:

1. Read premium plan selection from `GET /store/premium`.
2. Map the selected plan to `tier` + `billingPeriod`.
3. Launch either:
   - `POST /store/subscription/checkout/session` for Stripe
   - `POST /store/subscription/paypal/create` for PayPal
4. Refresh `GET /store/subscription/status/{playerId}` after checkout return.
5. Redirect any legacy `/offers` navigation to `/store-premium`.

### Important backend/frontend truth

The premium-store-specific backend endpoints are aligned for catalog, rewards, and purchase-path routing.

### Backend team note: `/store/offers` is no longer required for premium-store integration

As of the frontend cleanup on **April 20, 2026**, the premium-store flow no longer calls `GET /store/offers`.

Current behavior:

- premium-store CTAs launch the existing subscription checkout endpoints directly
- player reward cards use `GET /store/rewards/{playerId}`
- reward claims use `POST /store/rewards/{playerId}/claim/{rewardId}`
- the legacy `/offers` frontend route redirects into `/store-premium`

Backend should therefore treat the premium-store contract as:

- `GET /store/premium`
- `GET /store/rewards/{playerId}`
- `POST /store/rewards/{playerId}/claim/{rewardId}`
- existing `/store/subscription/*` routes for checkout and status

No dedicated `/store/offers` backend endpoint is required for the premium-store flow.

---

## Current Backend Shape

The current premium store implementation is intentionally lightweight:

- premium catalog content is config-backed
- `/store/premium` uses a short-lived in-memory cache
- reward claims reuse existing `PlayerTransaction` and `PlayerWallet`
- reward reset semantics are UTC-day based
- the current reward IDs are:
  - `daily-checkin`
  - `watch-ad`

### Current default premium catalog values

Current config-backed defaults in `appsettings.json` are:

- Ad-free plans:
  - `premium-monthly`
  - `premium-seasonal`
- Reward labels:
  - `daily-checkin` → `+25 coins`
  - `watch-ad` → `+15 coins`
- Watch-ad daily cap:
  - `3`
- Flash sale:
  - disabled by default, so `saleInfo` currently returns `null`

---

## Endpoint 1: `GET /store/premium`

Returns the shared premium catalog content for the premium screen.

### Auth

Bearer auth required.

### Response DTO

`PremiumStoreDto`

```json
{
  "adFree": {
    "title": "Ad-Free Plans",
    "subtitle": "Choose a lighter, uninterrupted Tycoon experience.",
    "benefits": [
      "Removes gameplay interstitial ads",
      "Keeps reward center and progression access",
      "Applies across supported mobile sessions"
    ],
    "plans": [
      {
        "id": "premium-monthly",
        "title": "Monthly Ad-Free",
        "subtitle": "Best for trying premium access",
        "priceLabel": "$4.99 / month",
        "badge": "Popular",
        "accentColor": "#0F766E",
        "isBestValue": false,
        "sku": "sub:premium:monthly"
      },
      {
        "id": "premium-seasonal",
        "title": "Seasonal Ad-Free",
        "subtitle": "Three months of uninterrupted play",
        "priceLabel": "$11.99 / season",
        "badge": "Best Value",
        "accentColor": "#1D4ED8",
        "isBestValue": true,
        "sku": "sub:premium:seasonal"
      }
    ]
  },
  "saleInfo": null,
  "rewardCenter": {
    "title": "Reward Center",
    "subtitle": "Pick up daily bonuses and bonus coin drops.",
    "cards": [
      {
        "rewardId": "daily-checkin",
        "title": "Daily Check-In",
        "subtitle": "Claim once per UTC day.",
        "rewardLabel": "+25 coins",
        "availability": "available",
        "gradientStart": "#0EA5E9",
        "gradientEnd": "#2563EB",
        "progress": 0,
        "isClaimAvailable": true,
        "remainingClaims": null,
        "dailyCap": null,
        "nextAvailableAtUtc": null
      },
      {
        "rewardId": "watch-ad",
        "title": "Watch an Ad",
        "subtitle": "Claim up to the daily cap.",
        "rewardLabel": "+15 coins",
        "availability": "available",
        "gradientStart": "#F59E0B",
        "gradientEnd": "#EF4444",
        "progress": 0,
        "isClaimAvailable": true,
        "remainingClaims": 3,
        "dailyCap": 3,
        "nextAvailableAtUtc": null
      }
    ]
  }
}
```

### Important notes

- `saleInfo` is explicitly `null` when no sale is active.
- `rewardCenter` here is still useful to frontend as presentation metadata.
- This endpoint is shared/non-player-specific, so it is the right source for:
  - ad-free plan cards
  - flash-sale visibility
  - reward card titles, labels, and gradients

---

## Endpoint 2: `GET /store/rewards/{playerId}`

Returns player-specific reward state for the current UTC day.

### Auth

- Bearer auth required
- authenticated user must match `{playerId}`

### Response DTO

`RewardCenterDto`

```json
{
  "title": "Reward Center",
  "subtitle": "Pick up daily bonuses and bonus coin drops.",
  "cards": [
    {
      "rewardId": "daily-checkin",
      "title": "Daily Check-In",
      "subtitle": "Day 1 reward is ready to claim.",
      "rewardLabel": "+25 coins",
      "availability": "available",
      "gradientStart": "#0EA5E9",
      "gradientEnd": "#2563EB",
      "progress": 0.0,
      "isClaimAvailable": true,
      "remainingClaims": null,
      "dailyCap": 7,
      "nextAvailableAtUtc": null
    },
    {
      "rewardId": "watch-ad",
      "title": "Watch an Ad",
      "subtitle": "3 of 3 claims remaining today.",
      "rewardLabel": "+15 coins",
      "availability": "available",
      "gradientStart": "#F59E0B",
      "gradientEnd": "#EF4444",
      "progress": 0.0,
      "isClaimAvailable": true,
      "remainingClaims": 3,
      "dailyCap": 3,
      "nextAvailableAtUtc": null
    }
  ]
}
```

### Status semantics

For `daily-checkin`:

- `subtitle` is backend-computed from streak state
- `progress` is a `0.0` to `1.0` ratio
- `isClaimAvailable` is `false` after the day is claimed
- `nextAvailableAtUtc` is populated after same-day claim

For `watch-ad`:

- `remainingClaims` counts remaining slots for the current UTC day
- `dailyCap` is currently `3`
- `progress` reflects usage against the cap
- `nextAvailableAtUtc` is currently `null` in the response shape

### Error cases

- `401` if unauthenticated
- `403` if player mismatch
- `404` if the player is not found

---

## Endpoint 3: `POST /store/rewards/{playerId}/claim/{rewardId}`

Claims a reward and credits coins through the existing wallet/transaction infrastructure.

### Auth

- Bearer auth required
- authenticated user must match `{playerId}`

### Supported reward IDs

- `daily-checkin`
- `watch-ad`

### Response DTO

`ClaimStoreRewardResponseDto`

Example for `daily-checkin`:

```json
{
  "rewardId": "daily-checkin",
  "coinsAwarded": 25,
  "newBalance": 25,
  "status": "claimed",
  "claimedAtUtc": "2026-04-20T14:30:00Z",
  "nextAvailableAtUtc": "2026-04-21T00:00:00Z",
  "currentStreak": 1,
  "remainingClaims": null
}
```

Example for `watch-ad`:

```json
{
  "rewardId": "watch-ad",
  "coinsAwarded": 15,
  "newBalance": 40,
  "status": "claimed",
  "claimedAtUtc": "2026-04-20T14:35:00Z",
  "nextAvailableAtUtc": null,
  "currentStreak": null,
  "remainingClaims": 2
}
```

### Claim rules

`daily-checkin`

- one successful claim per UTC day
- streak increments only if the prior successful claim was yesterday UTC
- otherwise streak resets to day 1

`watch-ad`

- max 3 successful claims per UTC day
- fourth claim attempt returns conflict

### Error cases

- `401` if unauthenticated
- `403` if player mismatch
- `404` for unknown reward ID
- `409` if reward is already exhausted for the current claim window

---

## Error Envelope

These premium store endpoints currently use the shared backend standard:

```json
{
  "error": {
    "code": "already_claimed",
    "message": "Daily check-in has already been claimed for today.",
    "details": {}
  }
}
```

Frontend should parse:

- `error.code`
- `error.message`
- `error.details`

Do not assume a flat body like:

```json
{
  "error": "already_claimed",
  "message": "This reward has already been claimed today."
}
```

That older flat shape appeared in an earlier handoff draft, but it is not the currently shipped backend format.

---

## Frontend Integration Guidance

### Recommended source-of-truth split

Use:

- `/store/premium` for:
  - ad-free catalog
  - flash-sale visibility/content
  - reward card presentation metadata
- `/store/rewards/{playerId}` for:
  - player-specific reward state
  - streak text
  - remaining claims
  - claim availability

### Purchase-path clarification

The premium-store contract does not require a dedicated new purchase endpoint beyond the existing subscription route family. The current Flutter implementation now routes premium purchase taps directly through those subscription endpoints, and the legacy `/offers` path is only a frontend redirect for backward navigation compatibility.

### Claim flow

When a claim succeeds:

1. update local coin balance from `newBalance`
2. invalidate or refetch `GET /store/rewards/{playerId}`
3. optionally keep `/store/premium` cached longer since it is shared content

### Safe assumptions

- `saleInfo` may be `null`
- current reward IDs are only `daily-checkin` and `watch-ad`
- the backend is the source of truth for reward availability
- premium store errors use the nested backend-standard error envelope

### Unsafe assumptions

- do not assume the old handoff field names such as `durationLabel`, `price`, `gradient`, `reward`, or `isAvailable`
- do not assume reward claims return `success: true`
- do not assume `watch-ad` will always remain coin-only forever

---

## Backend Notes For Future Expansion

The current implementation is intentionally v1:

- config-backed catalog content
- transaction-derived reward state
- no dedicated premium entitlement model yet
- no admin-managed premium campaign storage yet

Frontend should treat this contract as stable for current integration, but not assume today’s config-backed implementation is the permanent storage model.

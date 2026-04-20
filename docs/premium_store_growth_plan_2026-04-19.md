# Premium Store Growth Plan (Backend + Frontend Coordination)

> **Audience:** Frontend + backend teams  
> **Date:** 2026-04-19  
> **Scope:** Long-term Premium Store evolution after the initial fast-track backend release

---

## Executive Summary

The Premium Store now has a working backend baseline:

- `GET /store/premium`
- `GET /store/rewards/{playerId}`
- `POST /store/rewards/{playerId}/claim/{rewardId}`

That baseline is intentionally lightweight:

- premium catalog content is **configuration-backed**
- player rewards are derived from **existing `PlayerTransaction` history**
- coin credits flow through **existing `PlayerWallet` + `PlayerTransactionService`**
- the premium catalog uses a **short in-memory cache**

This is the right shape for a first release because it avoids schema churn and lets the frontend stop relying on hardcoded fallback content. Long-term, though, the Premium Store should move from a config-driven UI surface into a governed commerce and live-ops surface with better tooling, analytics, experimentation, and entitlement handling.

This document lays out a multi-phase approach for getting there without throwing away the systems we already have.

---

## What We Have Today

### Shipped backend systems

The current backend already gives us several strong foundations:

- **Store route family** in `Tycoon.Backend.Api/Features/Store/StoreEndpoints.cs`
- **Config-backed premium options** in `Tycoon.Backend.Api/Features/Store/StorePremiumOptions.cs`
- **Shared DTO contract** in `Tycoon.Shared.Contracts/Dtos/StoreDtos.cs`
- **Short-lived catalog caching** via `IMemoryCache`
- **Wallet persistence** via `PlayerWallet`
- **Idempotent reward crediting** via `PlayerTransactionService`
- **Applied transaction history** via `PlayerTransaction`
- **External checkout groundwork** via existing Stripe and PayPal subscription/payment flows
- **Admin store infrastructure** already used for catalog/system controls on the broader `/store` surface
- **Automated contract coverage** in `Tycoon.Backend.Api.Tests/Store/PremiumStoreEndpointsTests.cs`

### Why this is a good baseline

This baseline already gives us:

- a stable frontend integration point
- deterministic reward-claim behavior
- no double-crediting on retries
- no need for a new reward table just to ship
- room to evolve the premium catalog independently from gameplay endpoints

### Current limitations

The fast-track release is intentionally not the final system. Current constraints are:

- premium catalog content lives in app config, not in durable admin-managed storage
- flash sales are config toggles, not scheduled campaign entities
- rewards are inferred from transactions rather than modeled as first-class reward claims/campaigns
- no entitlement service currently maps premium plans into explicit ad-free access state
- no dedicated premium analytics/event stream exists yet
- no A/B testing or segmentation exists for pricing, sale copy, or reward-center variants
- no admin UX yet exists for premium-specific curation

---

## Multi-Phase Plan

## Phase 1: Stabilize The v1 Contract

### Goal

Make the current premium store safe to build against and easy to operate without changing the data model yet.

### Backend focus

- Keep `/store/premium` and `/store/rewards/{playerId}` as the canonical premium read surface.
- Keep reward IDs limited to:
  - `daily-checkin`
  - `watch-ad`
- Preserve idempotent claim behavior using deterministic transaction event IDs.
- Expand tests to cover:
  - cache behavior
  - config ordering
  - reward-state edge cases around UTC reset
  - reward transaction uniqueness over repeated requests
- Add stronger logging/telemetry around:
  - premium catalog fetches
  - reward-claim attempts
  - reward-claim conflicts

### Frontend focus

- Treat `/store/premium` as the source of truth for ad-free plans, sale card visibility, and reward card presentation metadata.
- Treat `/store/rewards/{playerId}` as the source of truth for player-specific reward status.
- Parse the **backend-standard nested error envelope**:
  - `error.code`
  - `error.message`
  - `error.details`
- Keep local fallbacks only as resilience, not as parallel business logic.

### Exit criteria

- frontend no longer depends on hardcoded premium catalog defaults for normal flows
- reward-center claim actions fully refresh from backend state
- error parsing matches the shipped API envelope

---

## Phase 2: Move Premium Content From Config To Live-Ops Data

### Goal

Reduce deploy-time coupling by allowing premium catalog and sale content to change without shipping appsettings edits.

### What we already have that helps

- store DTOs already separate the frontend contract from internal storage
- admin store patterns already exist elsewhere in the backend
- cache boundaries already exist, so swapping config for DB-backed reads is low risk

### What we need to add

- a durable premium catalog model, likely something like:
  - `PremiumPlan`
  - `PremiumCampaign`
  - `PremiumRewardDefinition`
- admin endpoints or dashboard tools for:
  - plan ordering
  - reward-card copy/gradient metadata
  - flash-sale activation windows
  - sale CTA text and linked product SKU
- cache invalidation strategy after admin updates

### Recommended backend sequence

1. Add read models first while keeping config as fallback.
2. Add admin CRUD for premium plans and campaigns.
3. Cut the runtime over from config-primary to DB-primary.
4. Keep config support only as bootstrap or emergency fallback.

### Frontend impact

- frontend should not need route changes if the response DTOs remain stable
- frontend should avoid keying UX off implementation details like plan count or config-only IDs

---

## Phase 3: Add Real Entitlements And Premium Access State

### Goal

Make “premium” a true product entitlement rather than just a store screen plus existing subscription checkout wiring.

### Current gap

Today the premium catalog can show ad-free plans, but there is not yet a dedicated entitlement layer that clearly answers:

- does this player currently have ad-free access?
- what plan is active?
- when does it expire?
- was it granted by Stripe, PayPal, admin action, promo, or platform restore?

### What we need to add

- a canonical premium entitlement model
- provider-to-entitlement reconciliation
- a player-facing premium status endpoint, likely one of:
  - `GET /store/premium/status/{playerId}`
  - or fold into existing subscription status if product semantics stay aligned
- webhook reconciliation rules for:
  - activation
  - renewal
  - cancellation
  - grace period / billing failure

### Existing systems that help

- existing Stripe subscription flow
- existing PayPal subscription flow
- existing transaction recording patterns
- existing subscription status DTO patterns

### Why this matters for frontend

Without explicit entitlement state, the frontend can display plans but cannot confidently reflect active premium access, renewal state, or restoration state.

---

## Phase 4: Make Rewards A First-Class Live Reward System

### Goal

Move beyond “two hardcoded reward IDs” into an extensible reward center.

### Current state

The current implementation is intentionally simple and good for launch:

- reward definitions are config-backed
- reward state is computed from transactions
- successful claims directly award coins

### What we will eventually want

- explicit reward definitions with start/end windows
- claim rules per reward type
- support for more reward kinds:
  - streak ladders
  - event-based premium claims
  - limited-time sponsor/ad multipliers
  - one-time comeback rewards
- non-coin reward payloads:
  - diamonds
  - powerups
  - tickets
  - premium trial days

### What we need to add

- dedicated reward configuration/persistence
- optional claim-history table or reward-claim projection for efficient querying
- generalized reward-grant execution layer that can issue:
  - coins
  - diamonds
  - inventory items
  - premium entitlements

### Recommendation

Do **not** add all of this to the current transaction-derived model. Use the current model as a bridge, then introduce a dedicated reward domain when the reward center becomes broader than daily check-in and watch-ad.

---

## Phase 5: Add Premium Analytics, Segmentation, And Experimentation

### Goal

Turn the Premium Store into an optimizable product surface instead of a static merch screen.

### What we should measure

- premium store impressions
- plan card taps
- flash-sale impressions and conversions
- reward-card impressions
- reward claims
- reward-claim conflict rates
- store exits without conversion

### What backend support is needed

- standardized analytics events for premium store interactions
- optional campaign/variant IDs in premium payloads
- segment-aware catalog selection
- A/B allocation support if experimentation becomes product-critical

### Frontend needs

- stable analytics field names
- campaign identifiers passed through in store payloads
- explicit impression/click event hooks in the store widgets

### Existing systems that help

- the backend already has analytics infrastructure and rollup patterns
- config/DTO separation makes it easy to attach campaign IDs later

---

## Phase 6: Build Premium Operations And Admin UX

### Goal

Give operators a way to manage premium content and investigate issues without appsettings edits or direct DB work.

### What to add

- premium plans admin page
- premium campaign/sale scheduling page
- reward-center definition page
- claim audit/search tools
- premium entitlement lookup by player
- replay/reconciliation tools for payment-provider inconsistencies

### Existing systems that help

- operator dashboard work already in progress
- existing admin route and auth patterns
- admin store endpoints already establish a precedent for commerce operations

### Outcome

This phase is what turns the premium store from “backend feature” into “operable product surface.”

---

## Recommended Technical Direction By Area

## Catalog And Sale Management

### Keep now

- config-backed `StorePremiumOptions`
- memory cache
- DTO-based response shaping

### Add next

- DB-backed premium plans/campaigns
- admin mutation surface
- cache busting on change

## Rewards

### Keep now

- transaction-backed credits
- UTC daily reset semantics
- deterministic idempotency

### Add next

- first-class reward definition storage
- generalized reward payout support
- claim-history/read-optimized projection if reward breadth grows

## Payments And Entitlements

### Keep now

- Stripe and PayPal plumbing
- existing subscription status patterns

### Add next

- premium entitlement record
- provider reconciliation
- restore/status endpoint for premium access state

## Analytics

### Keep now

- basic API test coverage

### Add next

- premium-specific events
- conversion funnel reporting
- campaign IDs and variant IDs

---

## Current Systems That Facilitate Growth

These are the main assets we should actively build on instead of replacing:

- **`StoreEndpoints` route family**
  The premium store sits inside an existing commerce surface rather than as a one-off API island.

- **`PlayerTransactionService`**
  This gives us idempotent, ledger-aware coin crediting and should remain the mechanism for reward grants even after the reward domain grows.

- **`PlayerWallet`**
  The wallet model already supports coin balance mutation and is the correct settlement destination for store rewards.

- **Stripe/PayPal store integrations**
  These give us the purchase-path base we need for later entitlement work.

- **Typed shared DTOs**
  These reduce drift and let frontend/backed iterate with clearer contract discipline.

- **Admin store patterns**
  We do not yet have premium-specific admin UX, but the repo already has admin commerce patterns we can extend.

- **Automated contract tests**
  The new premium-store endpoint tests mean the contract is already under validation and can grow safely.

---

## Major Gaps We Still Need To Add

- durable premium catalog storage
- premium campaign/sale scheduling
- explicit premium entitlement model
- premium-focused admin tooling
- premium analytics instrumentation
- experimentation/segmentation support
- broader reward types beyond coin-only grants
- payment-provider reconciliation workflows for premium access

---

## Frontend Team Guidance

## What frontend can safely build against now

- `GET /store/premium`
- `GET /store/rewards/{playerId}`
- `POST /store/rewards/{playerId}/claim/{rewardId}`

### Safe assumptions

- `saleInfo` may be `null`
- reward IDs are currently only `daily-checkin` and `watch-ad`
- reward card presentation metadata comes from the premium catalog endpoint
- reward card player-state comes from the player rewards endpoint
- claim conflicts should be surfaced from backend-provided error messages

### Unsafe assumptions

- do not assume price labels are permanent
- do not assume current plan IDs are the final entitlement IDs
- do not assume the store will remain config-backed forever
- do not assume all future rewards will be coin-only
- do not assume errors are flat `{ error, message }` responses

## Error envelope note

The current backend standard is:

```json
{
  "error": {
    "code": "already_claimed",
    "message": "Daily check-in has already been claimed for today.",
    "details": {}
  }
}
```

Frontend premium-store code should parse:

- `error.code`
- `error.message`
- `error.details`

If the client already has a generic error parser for other endpoints, it should reuse that parser here.

---

## Recommended Next Backend Steps

If we want the best ROI after the fast-track release, the next backend work should be:

1. Add premium-specific analytics events and basic observability.
2. Add a premium entitlement/status layer so ad-free access becomes a first-class backend concept.
3. Move premium catalog and sale content from config to admin-managed data.
4. Add admin UX for premium plans, campaigns, and reward definitions.

That order keeps the current release stable while building toward a system that can scale beyond one screen and two reward IDs.

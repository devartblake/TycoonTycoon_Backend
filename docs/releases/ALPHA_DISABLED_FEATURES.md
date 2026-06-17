# Alpha Release — Disabled Features

**Release:** alpha-beta-2026  
**Last updated:** 2026-06-16

All features listed here are **disabled for the Alpha release** via server-side feature flag gates. Any request to these endpoints returns:

```json
HTTP 403 Forbidden
{
  "error": {
    "code": "FeatureDisabled",
    "message": "This feature is not available in the current release.",
    "details": {}
  }
}
```

Feature flags are managed via `PATCH /api/v1/admin/config` without requiring a deployment. The current `FeatureFlagsJson` defaults for Alpha set all items below to `false`.

---

## Realtime Multiplayer (SignalR)

**Flag:** `realtime_multiplayer_enabled = false`  
**Gate:** Inline middleware in `Program.cs` — intercepts all `/ws/*` WebSocket upgrade requests

Affected endpoints:
- `WS /ws/match` — real-time match hub
- `WS /ws/presence` — player presence hub
- `WS /ws/notify` — push notification hub

**Reason disabled:** Real-time multiplayer requires load testing and Redis backplane validation before Alpha exposure. REST-only quiz flow is the Alpha golden path.

---

## Ranked Matchmaking

**Flag:** `matchmaking_enabled = false`  
**Gate:** Group-level filter on `MatchmakingEndpoints`

Affected endpoints:
- `POST /matchmaking/enqueue`
- `POST /matchmaking/cancel`
- `GET /matchmaking/status/{playerId}`

**Reason disabled:** Ranked matchmaking requires a stable player base. Alpha uses solo trivia sessions via `POST /quiz/complete` instead.

---

## Friends / Social

**Flag:** `social_enabled = false`  
**Gate:** Group-level filter on `FriendsEndpoints`, `MessagesEndpoints`, `PartyEndpoints`

Affected endpoints:
- `POST /friends/request`
- `POST /friends/request/{id}/accept`
- `POST /friends/request/{id}/decline`
- `GET /friends`, `DELETE /friends`
- `GET /friends/requests`
- `GET /messages/conversations`
- `POST /messages/conversations/direct`
- `GET /messages/conversations/{id}/messages`
- `POST /messages/conversations/{id}/messages`
- `POST /party`, `GET /party/{id}`
- `POST /party/{id}/invite`, `POST /party/{id}/enqueue`

**Reason disabled:** Social graph and messaging systems are built but require content moderation infrastructure before Alpha exposure.

---

## Skill Tree

**Flag:** `skill_tree_enabled = false`  
**Gate:** Group-level filter on `SkillsEndpoints`

Affected endpoints:
- `GET /skills/tree`
- `GET /skills/state/{playerId}`
- `POST /skills/unlock`
- `POST /skills/respec`

**Reason disabled:** Skill tree balance has not been tuned for the Alpha player population. Re-speccing costs and unlock prices require playtesting before release.

---

## Player Notifications

**Flag:** `notifications_enabled = false`  
**Gate:** Group-level filter on `PlayerNotificationsEndpoints`

Affected endpoints:
- `GET /notifications/inbox`
- `GET /notifications/unread-count`
- `POST /notifications/{id}/read`
- `POST /notifications/read-all`
- `DELETE /notifications/{id}`

**Reason disabled:** Push notification delivery (Hangfire dispatch + SignalR `NotificationHub`) requires real-time multiplayer to be enabled. Admin notification dispatch remains active server-side; player inbox read is gated.

---

## A/B Experiments

**Flag:** `experiments_enabled = false`  
**Gate:** Group-level filter on `ExperimentEndpoints`

Affected endpoints:
- `GET /experiments/player/{playerId}`
- `GET /experiments/player/{playerId}/{experimentKey}`
- `POST /experiments/player/{playerId}/{experimentKey}/impression`
- `POST /experiments/player/{playerId}/{experimentKey}/outcome`

**Reason disabled:** A/B experiment infrastructure is built but no active experiments are configured for Alpha. Enabling prematurely could create noise in early analytics.

---

## ToM Personalization

**Flag:** `tom_personalization_enabled = false`  
**Gate:** Group-level filter on `PersonalizationEndpoints`

Affected endpoints:
- `GET /personalization/profile/{playerId}`
- `POST /personalization/profile/{playerId}/event`
- `POST /personalization/profile/{playerId}/recalculate`
- `GET /personalization/home/{playerId}`
- `GET /personalization/recommendations/{playerId}`
- `GET /personalization/notifications/{playerId}`
- `POST /personalization/recommendations/{id}/accept`
- `POST /personalization/recommendations/{id}/dismiss`

**Reason disabled:** Theory of Mind personalization requires sufficient player behavior data (minimum 7-day session history) to produce meaningful recommendations. Will be enabled during Beta.

---

## Crypto Economy

**Flag:** `crypto_enabled = false`  
**Gate:** Group-level filter on `CryptoEconomyEndpoints`

Affected endpoints: All `/crypto/*` routes including SNX token, Solana, and XRP settlement endpoints.

**Reason disabled:** Crypto economy requires regulatory review and mainnet wallet configuration before any player-facing exposure. Devnet/testnet infrastructure is built but not Alpha-eligible.

---

## AI Sidecar Scoring

**Flag:** `ai_sidecar_enabled = false`  
**Gate:** Group-level filter on `MlScoringEndpoints`

Affected endpoints:
- `POST /ml/churn-risk`
- `POST /ml/match-quality`

**Reason disabled:** ML model endpoints proxy to the Python FastAPI sidecar. Alpha scope is REST + quiz; ML scoring is a Beta enhancement.

---

## Store Purchases (Stripe / PayPal)

**Flag:** `store_purchases_enabled = false`  
**Gate:** `EnsurePaymentsEnabledAsync` in `StoreEndpoints.cs` — fires before any payment-provider check

Affected endpoints:
- `POST /store/purchase`
- `POST /store/payments/*` (Stripe + PayPal flows)
- `POST /store/subscription/*`
- `POST /store/iap/validate`

**Reason disabled:** Payment provider sandbox credentials are not configured for Alpha. Purchase flows are internal-only scope and require regulatory/compliance review before player-facing exposure. Store catalog browsing (`GET /store/*`) remains active.

**Note:** If the flag is enabled without configured payment credentials, the existing `503 PAYMENTS_DISABLED` / `503 STRIPE_NOT_READY` / `503 PAYPAL_NOT_READY` responses from the provider check serve as a secondary guard.

**When enabled, the following compliance gates are active even in Alpha:**
- `StorePurchaseEligibilityService` checks COPPA `minor_purchase_restricted` restriction via the compliance service before any purchase proceeds.
- Items with `RequiresParentApproval = true` require an active `ParentalPurchaseControl` record with `PurchasesEnabled = true`.
- Items with `IsRandomized = true` are blocked for any user with the minor purchase restriction.
- Returns `403 MINOR_PURCHASE_RESTRICTED` or `403 PARENTAL_APPROVAL_REQUIRED` as appropriate.

**Deferred (Phase 2):**
- Monthly spend limit enforcement (limit is stored in `ParentalPurchaseControl.MonthlySpendLimitCents` but running monthly spend is not yet checked)
- Per-purchase parent notification email
- Daily/weekly spend caps
- Purchase cooldown periods
- Chargeback auto-lock of wallet balance

---

## Tournaments

**No dedicated flag.** Controlled indirectly by `matchmaking_enabled = false`.

Tournament bracket creation requires matchmaking to be active. When `matchmaking_enabled` is toggled on for Beta, tournament flows become available without additional flag changes.

---

## Advanced Seasons

**No dedicated flag.** Basic season entity and reward rules exist; advanced season lifecycle (bracket playoffs, season resets) are admin-controlled and not player-facing in Alpha.

Season reward claims at `GET /seasons` and `POST /seasons/rewards/claim` are active for Alpha's simple season structure.

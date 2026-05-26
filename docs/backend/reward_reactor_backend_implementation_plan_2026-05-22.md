# Reward Reactor Beta Backend Implementation Plan

**Date:** 2026-05-22  
**Frontend status:** Reward Reactor Alpha scaffold complete  
**Purpose:** Define backend contracts required before frontend Beta implementation begins.

## Summary

Reward Reactor Beta depends on backend-authoritative reward flows. The frontend must only render animation hints, previews, banners, particles, sounds, and claim state. It must not choose reward outcomes, reward amounts, winning symbols, wheel segments, RNG values, cooldowns, chain eligibility, event multipliers, or mission reward mechanisms.

Backend work is required for:

- Reward chains
- Mission-triggered reactor rewards
- Live event reactor modifiers
- Seasonal reactor configuration
- Arcade Spin `segmentId` -> `spinId` / `claimToken` migration

Frontend-only polish such as shaders, haptics, and audio can proceed separately once assets/specs are ready.

## Backend Contracts To Implement

### 1. Reward Reactor Chain Bonus

Add support for server-triggered follow-on reactor spins after rare or legendary rewards.

#### Update `POST /arcade/reactor/claim`

The claim response should include an optional chain reference:

```json
{
  "spinId": "reactor_spin_123",
  "status": "applied",
  "reward": {
    "rewardId": "rare_bonus",
    "displayName": "Rare Bonus",
    "lines": []
  },
  "walletSnapshot": {
    "coins": 1200,
    "gems": 25,
    "xp": 3400
  },
  "chainedSpinId": "reactor_chain_456"
}
```

#### Add chain spin endpoint

Preferred route:

```http
POST /arcade/reactor/chain
```

Request:

```json
{
  "chainedSpinId": "reactor_chain_456"
}
```

Response should match the existing reactor spin payload:

```json
{
  "spinId": "reactor_spin_457",
  "status": "pending_claim",
  "expiresAtUtc": "2026-05-22T18:30:00Z",
  "cooldownUntilUtc": null,
  "animation": {
    "layout": "reel3",
    "symbols": ["coin", "gem", "star"],
    "winningSymbolIndexes": [0, 3, 6],
    "rarity": "rare",
    "intensity": "high"
  },
  "rewardPreview": {
    "rewardId": "chain_bonus",
    "displayName": "Chain Bonus",
    "lines": []
  },
  "claimToken": "signed-or-opaque-token"
}
```

Backend requirements:

- Chain eligibility must be decided server-side.
- `chainedSpinId` must be nullable.
- Chain IDs must expire.
- Chain endpoint must be idempotent.
- Chain spin must not allow frontend-selected outcomes.

#### Chain endpoint contract examples

Success (`200 OK`) on first activation:

```json
{
  "spinId": "rr_9f5578d9f5d44719b675ca73a4ea7ef5",
  "status": "PendingClaim",
  "expiresAtUtc": "2026-05-22T18:30:00Z",
  "cooldownUntilUtc": null,
  "animation": {
    "layout": "three_reel_reactor",
    "symbols": ["syncoins", "syncoins", "syncoins"],
    "winningSymbolIndexes": [0, 1, 2],
    "rarity": "rare",
    "intensity": "high"
  },
  "rewardPreview": {
    "rewardId": "chain_bonus",
    "displayName": "Chain Bonus",
    "lines": [
      { "type": "coins", "amount": 100 }
    ]
  },
  "claimToken": "opaque-claim-token"
}
```

Duplicate/idempotent replay (`200 OK`) with same `chainedSpinId`:

```json
{
  "spinId": "rr_9f5578d9f5d44719b675ca73a4ea7ef5",
  "status": "PendingClaim",
  "expiresAtUtc": "2026-05-22T18:30:00Z",
  "cooldownUntilUtc": null,
  "animation": {
    "layout": "three_reel_reactor",
    "symbols": ["syncoins", "syncoins", "syncoins"],
    "winningSymbolIndexes": [0, 1, 2],
    "rarity": "rare",
    "intensity": "high"
  },
  "rewardPreview": {
    "rewardId": "chain_bonus",
    "displayName": "Chain Bonus",
    "lines": [
      { "type": "coins", "amount": 100 }
    ]
  },
  "claimToken": "opaque-claim-token"
}
```

Expired chain ticket (`409 Conflict`):

```json
{
  "error": {
    "code": "REWARD_CHAIN_EXPIRED",
    "message": "Reward chain ticket has expired."
  }
}
```

Invalid chain ticket (`404 Not Found`):

```json
{
  "error": {
    "code": "REWARD_CHAIN_NOT_FOUND",
    "message": "Chain ticket not found."
  }
}
```

### 2. Mission Integration

Allow mission claims to trigger Reward Reactor instead of, or in addition to, direct coin/gem rewards.

#### Update mission claim response

Mission claim responses should include:

```json
{
  "missionId": "daily_score_001",
  "status": "claimed",
  "rewardMechanismId": "reactor",
  "reactorSpinPayload": {
    "spinId": "reactor_spin_789",
    "status": "pending_claim",
    "expiresAtUtc": "2026-05-22T18:30:00Z",
    "cooldownUntilUtc": null,
    "animation": {
      "layout": "reel3",
      "symbols": ["coin", "gem", "star"],
      "winningSymbolIndexes": [0, 3, 6],
      "rarity": "common",
      "intensity": "medium"
    },
    "rewardPreview": {
      "rewardId": "mission_reactor_reward",
      "displayName": "Mission Reward",
      "lines": []
    },
    "claimToken": "signed-or-opaque-token"
  }
}
```

Backend requirements:

- `rewardMechanismId` should be nullable or default to existing direct reward behavior.
- Valid values for Beta: `direct`, `reactor`.
- `reactorSpinPayload` is required only when `rewardMechanismId == "reactor"`.
- Mission claim must remain idempotent.
- Backend must prevent duplicate mission reward grants across retries.

### 3. Live Events

Support event-specific reactor modifiers such as double coins, seasonal jackpots, or anniversary bonuses.

#### Add active events endpoint

```http
GET /events/active
```

Response:

```json
{
  "events": [
    {
      "eventId": "double_coins_weekend_2026_05",
      "displayName": "Double Coins Weekend",
      "startsAtUtc": "2026-05-22T00:00:00Z",
      "endsAtUtc": "2026-05-25T00:00:00Z",
      "eventMultiplier": 2.0
    }
  ]
}
```

#### Extend `POST /arcade/reactor/spin`

Add nullable event fields:

```json
{
  "eventId": "double_coins_weekend_2026_05",
  "eventMultiplier": 2.0
}
```

Backend requirements:

- Backend determines whether an event applies.
- Event multiplier must be applied server-side before preview/claim.
- Frontend uses event fields only for display.
- `eventId` and `eventMultiplier` must be nullable when no event is active.
- Event catalog must contain at least one active test event before frontend UI work begins.

### 4. Seasonal Reactors

Expose a season key so frontend can swap symbol sets, particles, and border themes.

#### Extend reactor spin/config response

Preferred minimal addition to `POST /arcade/reactor/spin`:

```json
{
  "seasonKey": "halloween_2026"
}
```

Optional future config endpoint:

```http
GET /arcade/reactor/config
```

Response:

```json
{
  "seasonKey": "halloween_2026",
  "symbolSet": "halloween",
  "assetBaseUrl": "https://cdn.example.com/reactor/halloween_2026/"
}
```

Backend requirements:

- `seasonKey` must be stable for the duration of a season.
- Seasonal timing must be product-controlled, not client-clock-controlled.
- If assets are remote, backend/CDN must provide stable asset URLs and cache policy.
- If assets are bundled in-app, backend only needs to return `seasonKey`.

### 5. Arcade Spin Migration

Remove trusted frontend reward authority from Spin & Earn.

#### Add `POST /arcade/spin/start`

Request:

```json
{
  "playerId": "player_123"
}
```

Response:

```json
{
  "spinId": "spin_abc",
  "wheelStopIndex": 4,
  "claimToken": "signed-or-opaque-token",
  "expiresAtUtc": "2026-05-22T18:30:00Z"
}
```

#### Update `POST /arcade/spin/claim`

New request:

```json
{
  "spinId": "spin_abc",
  "claimToken": "signed-or-opaque-token",
  "idempotencyKey": "client-or-server-idempotency-key"
}
```

Backend requirements:

- `segmentId` must no longer be required from frontend.
- `wheelStopIndex` is animation-only.
- Backend persists the pending outcome at start time.
- Claim must be idempotent.
- Claim token must expire.
- Claim route should keep encrypted-channel compatibility.
- Temporary compatibility may accept both old and new payloads during rollout, but new contract must be documented.

## Shared Backend Rules

- All reward outcomes must be selected server-side.
- All reward grants must be idempotent.
- Cooldowns, eligibility, feature flags, event multipliers, and reward limits must be enforced server-side.
- Claim responses should distinguish:
  - `applied`
  - `duplicate`
  - `expired`
  - `cooldown`
  - `invalid`
- Pending rewards should expire.
- Reward preview must match what claim will grant unless the claim expires or is rejected.
- Frontend must receive only animation-safe data.

## Acceptance Criteria

Backend is ready for frontend Beta when:

- `POST /arcade/reactor/spin` returns all current Alpha fields plus any agreed Beta nullable fields.
- `POST /arcade/reactor/claim` supports `chainedSpinId`.
- Chain endpoint or equivalent chain spin flow is implemented.
- Mission claim response can emit `rewardMechanismId: "reactor"` and a full `reactorSpinPayload`.
- Live event fields are available in reactor spin responses.
- Seasonal `seasonKey` source is available.
- `POST /arcade/spin/start` exists.
- `POST /arcade/spin/claim` accepts `spinId` + `claimToken`.
- Backend contract examples are documented with success, duplicate, cooldown, expired, and invalid-token cases.

## Current Contract Examples (Implementation-Aligned)

The examples below reflect the currently implemented backend wire shapes and status/error behavior.

### Reactor Spin

Success (`200 OK`):

```json
{
  "spinId": "rr_64ba8be80ca64f9cae3bb5bcb30b7f8f",
  "status": "PendingClaim",
  "expiresAtUtc": "2026-05-22T22:10:00Z",
  "cooldownUntilUtc": "2026-05-23T00:00:00Z",
  "animation": {
    "layout": "three_reel_reactor",
    "symbols": ["syncoins", "syncoins", "syncoins"],
    "winningSymbolIndexes": [0, 1, 2],
    "rarity": "rare",
    "intensity": "high"
  },
  "rewardPreview": {
    "rewardId": "coins_medium",
    "displayName": "Coin Chest",
    "lines": [
      { "type": "coins", "amount": 500 }
    ]
  },
  "claimToken": "opaque-token",
  "eventId": "double_coins_weekend_2026_05",
  "eventMultiplier": 2.0,
  "seasonKey": "halloween_2026"
}
```

Cooldown/daily-limit (`409 Conflict`):

```json
{
  "error": {
    "code": "REWARD_DAILY_LIMIT_REACHED",
    "message": "Daily reward limit reached."
  }
}
```

### Reactor Claim

Success (`200 OK`):

```json
{
  "spinId": "rr_64ba8be80ca64f9cae3bb5bcb30b7f8f",
  "status": "Applied",
  "duplicate": false,
  "appliedAtUtc": "2026-05-22T22:06:15Z",
  "lines": [
    { "type": "coins", "amount": 500 }
  ],
  "wallet": {
    "coins": 1500,
    "diamonds": 12,
    "xp": 3400
  },
  "chainedSpinId": "chain_9f90fdedfbde4c86a3654cb4d58ac7b9"
}
```

Duplicate/idempotent replay (`200 OK`):

```json
{
  "spinId": "rr_64ba8be80ca64f9cae3bb5bcb30b7f8f",
  "status": "Applied",
  "duplicate": true,
  "appliedAtUtc": "2026-05-22T22:06:15Z",
  "lines": [
    { "type": "coins", "amount": 500 }
  ],
  "wallet": {
    "coins": 1500,
    "diamonds": 12,
    "xp": 3400
  },
  "chainedSpinId": "chain_9f90fdedfbde4c86a3654cb4d58ac7b9"
}
```

Expired (`409 Conflict`):

```json
{
  "error": {
    "code": "REWARD_PENDING_EXPIRED",
    "message": "Pending reward has expired."
  }
}
```

Invalid token (`403 Forbidden`):

```json
{
  "error": {
    "code": "REWARD_INVALID_TOKEN",
    "message": "Claim token is invalid."
  }
}
```

### Reactor Chain

Success (`200 OK`) and duplicate replay (`200 OK`) both return the same generated chain spin payload for a given `chainedSpinId`:

```json
{
  "spinId": "rr_17af2fca7ce04abca368f728f7cd37db",
  "status": "PendingClaim",
  "expiresAtUtc": "2026-05-22T22:12:00Z",
  "cooldownUntilUtc": null,
  "animation": {
    "layout": "three_reel_reactor",
    "symbols": ["syncoins", "syncoins", "syncoins"],
    "winningSymbolIndexes": [0, 1, 2],
    "rarity": "rare",
    "intensity": "high"
  },
  "rewardPreview": {
    "rewardId": "chain_bonus",
    "displayName": "chain_bonus",
    "lines": [
      { "type": "coins", "amount": 100 }
    ]
  },
  "claimToken": "opaque-token",
  "eventId": "double_coins_weekend_2026_05",
  "eventMultiplier": 2.0,
  "seasonKey": "halloween_2026"
}
```

Expired chain ticket (`409 Conflict`):

```json
{
  "error": {
    "code": "REWARD_CHAIN_EXPIRED",
    "message": "Reward chain ticket has expired."
  }
}
```

Invalid chain ticket (`404 Not Found`):

```json
{
  "error": {
    "code": "REWARD_CHAIN_NOT_FOUND",
    "message": "Chain ticket not found."
  }
}
```

### Reactor Config

`GET /arcade/reactor/config` success (`200 OK`):

```json
{
  "seasonKey": "halloween_2026",
  "symbolSet": "halloween",
  "assetBaseUrl": "https://cdn.example.com/reactor/halloween_2026/"
}
```

### Active Events

`GET /events/active` success (`200 OK`):

```json
{
  "events": [
    {
      "eventId": "double_coins_weekend_2026_05",
      "displayName": "Double Coins Weekend",
      "startsAtUtc": "2026-05-22T00:00:00Z",
      "endsAtUtc": "2026-05-25T00:00:00Z",
      "eventMultiplier": 2.0
    }
  ]
}
```

### Mission Claim (Direct vs Reactor)

Direct reward mechanism (`200 OK`):

```json
{
  "status": "Claimed",
  "playerId": "f4c72ca5-c057-4384-b319-2576f9366a88",
  "missionId": "f0067161-5f47-445d-a0b4-573f26f1849e",
  "missionType": "Daily",
  "missionKey": "daily_play_3",
  "rewardXp": 50,
  "rewardCoins": 10,
  "rewardDiamonds": 0,
  "rewardMechanismId": "direct",
  "reactorSpinPayload": null
}
```

Reactor reward mechanism (`200 OK`) for allowlisted mission keys:

```json
{
  "status": "Claimed",
  "playerId": "f4c72ca5-c057-4384-b319-2576f9366a88",
  "missionId": "f0067161-5f47-445d-a0b4-573f26f1849e",
  "missionType": "Daily",
  "missionKey": "reactor_daily_score",
  "rewardXp": 50,
  "rewardCoins": 10,
  "rewardDiamonds": 0,
  "rewardMechanismId": "reactor",
  "reactorSpinPayload": {
    "spinId": "rr_59fcb386958f4b06a2361c9d2094a1a8",
    "status": "PendingClaim",
    "expiresAtUtc": "2026-05-22T22:10:00Z",
    "cooldownUntilUtc": null,
    "animation": {
      "layout": "three_reel_reactor",
      "symbols": ["xp_small", "xp_small", "xp_small"],
      "winningSymbolIndexes": [0, 1, 2],
      "rarity": "common",
      "intensity": "low"
    },
    "rewardPreview": {
      "rewardId": "xp_boost_small",
      "displayName": "Small XP Boost",
      "lines": [
        { "type": "xp", "amount": 100 }
      ]
    },
    "claimToken": "opaque-token"
  }
}
```

Already-claimed mission replay (`200 OK`) is idempotent and reuses the same mission reactor spin payload (no duplicate mission spin sessions).

## QA Checklist Appendix (Contract Example Traceability)

Use this appendix to map each documented contract example to a concrete existing automated test.

### Reactor Spin

- [x] Success (`200 OK`) shape, event fields, season key
  - `RewardReactorTests.Spin_Returns_PendingClaim_With_ClaimToken`
- [x] Duplicate/idempotent spin replay (same idempotency key)
  - `RewardReactorTests.Spin_IsDuplicate_SameIdempotencyKey_ReturnsSameSpinId`
- [x] Cooldown/daily-limit conflict (`409`)
  - `RewardReactorTests.Second_Spin_Without_Claim_Returns_CooldownActive`

### Reactor Claim

- [x] Success (`200 OK`) applied + wallet update
  - `RewardReactorTests.Spin_Then_Claim_AppliesRewardToWallet`
- [x] Duplicate/idempotent claim replay (`200 OK`, `duplicate = true`)
  - `RewardReactorTests.Claim_Duplicate_ReturnsDuplicate_True`
- [x] Invalid token (`403 Forbidden`)
  - `RewardReactorTests.Claim_WithInvalidToken_Returns403`
- [x] Expired pending claim (`409 Conflict`, `REWARD_PENDING_EXPIRED`)
  - `RewardReactorTests.Claim_WhenPendingExpired_Returns409`

### Reactor Chain

- [x] First activation success (`200 OK`) and idempotent replay (`200 OK`)
  - `RewardReactorTests.ReactorChain_ActivatesTicket_And_IsIdempotent`
- [x] Expired chain ticket (`409 Conflict`, `REWARD_CHAIN_EXPIRED`)
  - `RewardReactorTests.ReactorChain_ExpiredTicket_Returns409`
- [x] Invalid/missing chain ticket (`404 Not Found`, `REWARD_CHAIN_NOT_FOUND`)
  - `RewardReactorTests.ReactorChain_MissingTicket_Returns404`

### Reactor Config

- [x] Unauthorized request (`401`)
  - `RewardReactorTests.GetReactorConfig_WithoutAuth_Returns401`
- [x] Authorized response payload (`seasonKey`, `symbolSet`, `assetBaseUrl`)
  - `RewardReactorTests.GetReactorConfig_WithAuth_ReturnsAssetSwitchingConfig`

### Active Events

- [x] Active events endpoint shape (`200 OK`)
  - `RewardReactorTests.GetActiveEvents_ReturnsOk_WithExpectedShape`

### Mission Claim (Direct vs Reactor)

- [x] Reactor mechanism payload for allowlisted mission keys
  - `ClaimMissionHandlerTests.Handle_ReactorMission_Claimed_ReturnsReactorPayload`
- [x] Reactor mission idempotent replay (already-claimed flow)
  - `ClaimMissionHandlerTests.Handle_ReactorMission_AlreadyClaimed_IsIdempotent_AndDoesNotCreateDuplicateSpin`
- [x] Direct mechanism for non-allowlisted mission keys
  - `ClaimMissionHandlerTests.Handle_MissionNotInAllowlist_UsesDirectMechanism`

### Arcade Spin Migration

- [x] New start endpoint contract path
  - `RewardReactorTests.ArcadeSpinStart_Returns_PendingClaim`
- [x] New claim contract (`spinId` + `claimToken` + `idempotencyKey`)
  - `RewardReactorTests.ArcadeSpinClaim_NewContract_UsesSpinIdClaimTokenAndIdempotency`
- [x] Legacy claim compatibility during rollout
  - `RewardReactorTests.ArcadeSpinClaim_LegacyContract_RemainsSupported`

## Frontend Handoff After Backend Completion

Cross-repo handoff note for trivia_tycoon frontend team:

- docs/handoffs/reward_reactor_frontend_handoff_trivia_tycoon_2026-05-22.md

Once the above contracts are available, frontend can implement:

- Reactor chain state machine and Chain Bonus banner.
- Mission reactor overlay.
- Live event badge and multiplier display.
- Seasonal symbol/theme rendering.
- Spin & Earn migration away from trusted `segmentId`.
- Contract tests and UI tests against backend-compatible fixtures.

## Flame Engine Recommendation

Do not make Flame a default dependency for Reward Reactor Beta particles.

Flame can help if the reactor becomes a larger game-like scene with many sprite emitters, component lifecycles, game-loop timing, collision-style effects, or reusable arcade animation systems. Flame provides game-oriented effects and particles, including sprite-based particles, but it would also introduce a larger rendering abstraction into a widget that currently fits Flutter's native composition model.

For this specific reactor, the existing `CustomPainter` plus `RepaintBoundary` approach is simpler and already matches the current UI architecture. Use Flutter-native `CustomPainter`, sprite sheets/WebP, and optionally fragment shaders first. Revisit Flame only after a small prototype proves it gives better performance or maintainability than the current renderer.

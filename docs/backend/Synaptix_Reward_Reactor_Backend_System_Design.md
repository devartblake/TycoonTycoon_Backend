# Synaptix Reward Reactor Backend System Design

**Version:** Alpha/Beta backend planning document  
**Status:** Planning-only; no implementation implied by this document  
**Primary goal:** Define a backend-authoritative reward pipeline for the Synaptix Reward Reactor and align Arcade Spin to the same server-generated outcome model.

> The Reward Reactor and Arcade Spin must not behave like gambling systems. They are progression and reward feedback mechanisms. The backend determines every outcome, the client animates the result, and reward application is idempotent and auditable.

---

## 1. Executive Summary

The Synaptix Reward Reactor needs a shared backend reward system that can power:

- Reward Reactor reels
- Arcade Spin
- Daily rewards
- Mission completion rewards
- Event bonuses
- Rank promotions
- Skill tree bonuses
- Inventory or cosmetic drops

The current Arcade Spin backend already exposes:

- `GET /arcade/spin/segments`
- `POST /arcade/spin/claim`

However, the current claim flow accepts a client-provided `segmentId`. That is acceptable as a legacy/current implementation note, but the Reward Reactor design should move both Arcade Spin and Reward Reactor onto the same backend-authoritative lifecycle:

```text
Start / Spin request
        |
        v
Backend validates player eligibility, cooldown, and feature state
        |
        v
Backend selects reward using server-side RNG and configured reward pool
        |
        v
Backend persists pending reward outcome
        |
        v
Client receives animation-safe result payload
        |
        v
Client animates reel/spin/reward sequence
        |
        v
Claim request idempotently applies pending reward
```

The frontend must never send the chosen reward, segment, reward amount, inventory mutation, or RNG value as authority.

---

## 2. Core Principles

### Backend Authority

The backend owns:

- RNG
- Reward pool selection
- Reward quantities
- Eligibility
- Cooldowns
- Daily caps
- Anti-cheat validation
- Inventory/wallet/progression mutation
- Claim idempotency

The client owns:

- Animation
- Sound/haptics timing
- Display labels
- Reel/symbol choreography
- Rendering of server-provided animation hints

### Unified Reward Lifecycle

Reward Reactor and Arcade Spin should share the same lifecycle:

1. `Start` creates a server-generated pending reward.
2. Client animates the returned outcome.
3. `Claim` applies the pending reward once.
4. Duplicate claim returns a deterministic duplicate response.
5. Expired, replayed, or wrong-player claims are rejected.

### Compatibility First

Existing Arcade Spin routes should be evolved, not removed abruptly. `GET /arcade/spin/segments` remains useful for catalog/display compatibility. The claim route should be migrated away from trusting `segmentId`.

---

## 3. Proposed API Contracts

These are proposed future contracts unless explicitly marked as current.

### Reward Reactor

#### `POST /arcade/reactor/spin`

Creates a server-generated pending reactor reward.

Request:

```json
{
  "idempotencyKey": "reactor-20260520-player-action-001",
  "reactorId": "daily-xp-reactor",
  "context": {
    "source": "daily_login",
    "missionId": null,
    "eventId": null
  }
}
```

Response:

```json
{
  "spinId": "rr_01HX...",
  "status": "PendingClaim",
  "expiresAtUtc": "2026-05-20T22:15:00Z",
  "cooldownUntilUtc": "2026-05-21T00:00:00Z",
  "animation": {
    "layout": "three_reel_reactor",
    "symbols": ["xp_multiplier", "syncoins", "xp_vault"],
    "winningSymbolIndexes": [0, 1, 2],
    "rarity": "rare",
    "intensity": "high"
  },
  "rewardPreview": {
    "rewardId": "xp_multiplier_2x",
    "displayName": "2x XP Multiplier",
    "lines": [
      { "type": "xp", "amount": 250 },
      { "type": "coins", "amount": 50 }
    ]
  },
  "claimToken": "opaque-server-token"
}
```

#### `POST /arcade/reactor/claim`

Claims a pending reactor reward.

Request:

```json
{
  "spinId": "rr_01HX...",
  "idempotencyKey": "claim-rr-01HX...",
  "claimToken": "opaque-server-token"
}
```

Response:

```json
{
  "spinId": "rr_01HX...",
  "status": "Applied",
  "duplicate": false,
  "appliedAtUtc": "2026-05-20T22:10:15Z",
  "lines": [
    { "type": "xp", "amount": 250 },
    { "type": "coins", "amount": 50 }
  ],
  "wallet": {
    "coins": 1250,
    "diamonds": 12,
    "xp": 9820
  }
}
```

#### `GET /users/me/rewards`

Returns current pending and recent reward state for the authenticated player.

Response:

```json
{
  "pending": [
    {
      "mechanism": "reactor",
      "spinId": "rr_01HX...",
      "rewardId": "xp_multiplier_2x",
      "expiresAtUtc": "2026-05-20T22:15:00Z"
    }
  ],
  "recentClaims": [
    {
      "mechanism": "arcade_spin",
      "rewardId": "syncoins_small",
      "claimedAtUtc": "2026-05-20T20:30:00Z"
    }
  ]
}
```

### Arcade Spin Alignment

#### Current Route: `GET /arcade/spin/segments`

Keep this route for display/catalog compatibility. It may continue returning segment visuals, labels, weights, and enabled state for Flutter rendering.

#### Proposed Route: `POST /arcade/spin/start`

Creates a server-generated pending arcade spin outcome.

Request:

```json
{
  "idempotencyKey": "arcade-spin-20260520-001"
}
```

Response:

```json
{
  "spinId": "spin_01HX...",
  "status": "PendingClaim",
  "expiresAtUtc": "2026-05-20T22:15:00Z",
  "segmentId": "coins_100",
  "animation": {
    "wheelStopIndex": 3,
    "segmentId": "coins_100",
    "rarity": "common"
  },
  "rewardPreview": {
    "rewardId": "coins_100",
    "displayName": "100 SynCoins",
    "lines": [
      { "type": "coins", "amount": 100 }
    ]
  },
  "claimToken": "opaque-server-token"
}
```

#### Updated Semantics: `POST /arcade/spin/claim`

The current route exists, but should evolve so `segmentId` is no longer trusted as authority. Claim should use `spinId`, `idempotencyKey`, and optionally an opaque `claimToken`.

Request:

```json
{
  "spinId": "spin_01HX...",
  "idempotencyKey": "claim-spin-01HX...",
  "claimToken": "opaque-server-token"
}
```

Legacy/current gap:

- Current `POST /arcade/spin/claim` accepts `segmentId` and `spinId`.
- The proposed design keeps `spinId`, removes trust in client-selected `segmentId`, and reads the reward from persisted pending outcome state.

---

## 4. Backend Components

### `RewardOutcomeService`

Responsible for weighted reward selection.

Responsibilities:

- Load reward pool by mechanism and context.
- Filter disabled or expired rewards.
- Apply player eligibility constraints.
- Use `IRewardRng` to select a reward.
- Return reward lines and animation hints.

### `RewardPolicyService`

Responsible for policy and eligibility.

Responsibilities:

- Feature flag checks.
- Daily caps.
- Cooldowns.
- Event window checks.
- Player status checks.
- Anti-cheat prechecks.

### `RewardClaimService`

Responsible for applying rewards idempotently.

Responsibilities:

- Validate pending reward exists.
- Validate player owns pending reward.
- Validate pending reward is not expired.
- Detect duplicate claims.
- Apply reward lines to wallet, inventory, XP, or progression services.
- Write claim ledger.

### `RewardAuditService`

Responsible for telemetry and security evidence.

Responsibilities:

- Emit successful spin and claim audit events.
- Emit rejected/duplicate/expired claim events.
- Record suspicious behavior signals.
- Provide enough metadata for operator investigation.

### `IRewardRng`

Cryptographically secure server-side RNG abstraction.

Requirements:

- No client seed.
- No predictable sequence.
- Testable deterministic implementation for unit tests.
- Production implementation backed by cryptographic randomness.

### Shared DTOs

Recommended DTO families:

- `StartRewardRequest`
- `StartRewardResponse`
- `ClaimRewardRequest`
- `ClaimRewardResponse`
- `RewardLineDto`
- `RewardAnimationHintDto`
- `PendingRewardDto`
- `RewardInventoryDto`
- `RewardErrorDto`

---

## 5. Persistence Model

### `RewardMechanism`

Use an enum or discriminator:

- `reactor`
- `arcade_spin`
- `daily`
- `mission`
- `event`
- `season`
- `skill_tree`

### `RewardSession` / `RewardSpin`

Stores generated outcomes before claim.

Recommended fields:

| Field | Purpose |
|---|---|
| `Id` | Internal row ID |
| `SpinId` | Public spin/session ID |
| `PlayerId` | Owner |
| `Mechanism` | Reactor, arcade spin, daily, event, etc. |
| `RewardId` | Selected reward |
| `RewardLinesJson` | XP/coins/diamonds/inventory/progression lines |
| `AnimationJson` | Client-safe animation hints |
| `Status` | PendingClaim, Applied, Expired, Cancelled, Rejected |
| `IdempotencyKey` | Start idempotency |
| `ClaimTokenHash` | Optional opaque token hash |
| `CreatedAtUtc` | Creation timestamp |
| `ExpiresAtUtc` | Claim expiry |
| `ClaimedAtUtc` | Claim timestamp |
| `PolicySnapshotJson` | Cooldown/cap/context evidence |

### `RewardClaimLedger`

Stores idempotent reward application.

Recommended uniqueness:

- `(PlayerId, Mechanism, SpinId)`
- `(PlayerId, IdempotencyKey)` for claim requests

Recommended fields:

| Field | Purpose |
|---|---|
| `Id` | Internal row ID |
| `PlayerId` | Claiming player |
| `Mechanism` | Reward mechanism |
| `SpinId` | Generated outcome ID |
| `RewardId` | Applied reward |
| `RewardLinesJson` | Applied reward lines |
| `Status` | Applied, Duplicate, Rejected |
| `IdempotencyKey` | Claim idempotency |
| `AppliedAtUtc` | Application timestamp |
| `AuditCorrelationId` | Trace/audit linkage |

### Existing `SpinClaim` Compatibility

Current Arcade Spin uses `SpinClaim` to ensure one `spinId` can only be claimed once.

Recommended migration path:

1. Keep `SpinClaim` for existing clients during Alpha compatibility.
2. Add server-generated `spin/start` and shared pending reward persistence.
3. Update Flutter to use start/claim token flow.
4. Stop trusting `segmentId` in claim.
5. Migrate or mirror old `SpinClaim` records into `RewardClaimLedger` if historical reporting requires it.

---

## 6. Reward Lines

Reward lines should be generic so the same claim pipeline can apply multiple reward types.

Recommended reward line types:

| Type | Example |
|---|---|
| `xp` | `{ "type": "xp", "amount": 250 }` |
| `coins` | `{ "type": "coins", "amount": 100 }` |
| `diamonds` | `{ "type": "diamonds", "amount": 2 }` |
| `inventory_item` | `{ "type": "inventory_item", "sku": "avatar_hero_v1", "quantity": 1 }` |
| `skill_shard` | `{ "type": "skill_shard", "skillNodeId": "logic_core", "amount": 3 }` |
| `mission_progress` | `{ "type": "mission_progress", "missionKey": "daily_reactor", "amount": 1 }` |
| `season_points` | `{ "type": "season_points", "amount": 50 }` |

Alpha should start with XP, coins, diamonds, and placeholder inventory/cosmetic support.

---

## 7. Error Codes

Use deterministic error codes so Flutter can render correct states.

| Code | HTTP | Meaning |
|---|---:|---|
| `REWARD_FEATURE_DISABLED` | 403 | Mechanism disabled by feature flag |
| `REWARD_COOLDOWN_ACTIVE` | 409 | Player must wait before starting again |
| `REWARD_DAILY_LIMIT_REACHED` | 409 | Player has reached cap |
| `REWARD_PENDING_NOT_FOUND` | 404 | Spin/session does not exist |
| `REWARD_PENDING_EXPIRED` | 409 | Pending reward expired |
| `REWARD_PLAYER_MISMATCH` | 403 | Reward belongs to another player |
| `REWARD_DUPLICATE` | 200 | Idempotent duplicate claim; return prior result |
| `REWARD_REJECTED` | 409 | Anti-cheat or policy rejected reward |
| `REWARD_INVALID_TOKEN` | 403 | Claim token invalid or mismatched |

---

## 8. Anti-Cheat And Security Requirements

The backend validates:

- JWT identity as the only player ID source.
- Start and claim idempotency keys.
- Cooldowns and daily limits.
- Packet replay.
- Expired pending rewards.
- Mismatched player IDs.
- Duplicate claims.
- Impossible spin frequency.
- Suspicious velocity from repeated start/claim attempts.
- Invalid or tampered pending reward tokens.

Recommended protections:

| Protection | Purpose |
|---|---|
| JWT validation | Session integrity |
| Server-side RNG | Fair reward selection |
| Start idempotency | Prevent duplicate pending outcomes |
| Claim idempotency | Prevent duplicate reward application |
| Opaque claim token | Prevent client tampering |
| Pending reward persistence | Keep server as source of truth |
| Audit correlation ID | Operator investigation and abuse tracing |
| Rate limits | Prevent brute force / automation |

Suspicious behavior should emit audit events and may block reward application.

---

## 9. Integration Points

### Wallet / Economy

Coin, diamond, and XP lines should use existing economy/wallet application services where possible. Reward application should be transactional with the claim ledger.

### Inventory / Store

Inventory and cosmetic lines should use the same ownership and stock policy concepts as store inventory. If inventory support is not ready for Alpha, emit placeholder reward lines but do not expose them publicly until the path is stable.

### Missions

Mission progress reward lines can increment mission goals or emit mission progress events. Mission reward claims should remain idempotent.

### Seasons / Leaderboards

Season point reward lines should integrate with existing season rank point transaction patterns, not bypass them.

### Anti-Cheat

Reward start and claim should call anti-cheat policy hooks before generating and before applying rewards.

### Admin Audit

Every start, apply, duplicate, reject, and suspicious event should be queryable by operators.

---

## 10. Rollout And Compatibility

### Alpha

Focus:

- Shared backend design.
- Server-authoritative Arcade Spin `start` / `claim` path.
- Minimal Reward Reactor support.
- Reward types: XP, coins, diamonds, cosmetic/inventory placeholders.
- Keep `GET /arcade/spin/segments` for Flutter display compatibility.

Do not add:

- Real-money mechanics.
- Purchasable spins.
- Client-authoritative reward selection.
- Tournament reward chains.
- Seasonal reactor campaigns.

### Beta

Add:

- Full Arcade Spin migration off client-selected segment claims.
- Mission, skill shard, event, and inventory reward lines.
- Admin-configurable reward pools and limits.
- Richer anti-cheat telemetry.
- Reward inventory history.

### Production

Add:

- Dynamic reward campaigns.
- Seasonal reactors.
- Live-event integration.
- Realtime tournament reward reactors.
- Operator dashboards for reward pools, claim velocity, and suspicious activity.

---

## 11. Current Arcade Spin Gap

Current implementation summary:

- `GET /arcade/spin/segments` returns enabled display segments.
- `POST /arcade/spin/claim` accepts `segmentId` and `spinId`.
- `SpinClaim` prevents the same `spinId` from being claimed twice.
- Wallet coins are applied from the segment selected by the client request.

Design gap:

- The client can currently identify the segment it wants to claim.
- The backend validates the segment exists and is enabled, but the backend does not currently generate and persist the selected segment before claim.

Required alignment:

- Add server-generated `start`.
- Persist selected reward outcome.
- Claim by `spinId` and server-side pending state.
- Treat client `segmentId` as display-only or legacy compatibility, not authority.

---

## 12. Future Implementation Test Scenarios

### Start / Spin

- Start returns pending server-generated reward.
- Duplicate start with same idempotency key returns same pending reward.
- Cooldown violation returns `REWARD_COOLDOWN_ACTIVE`.
- Daily cap violation returns `REWARD_DAILY_LIMIT_REACHED`.
- Disabled feature returns `REWARD_FEATURE_DISABLED`.

### Claim

- Claim applies reward once.
- Duplicate claim returns `REWARD_DUPLICATE` and prior applied result.
- Expired pending reward returns `REWARD_PENDING_EXPIRED`.
- Wrong player returns `REWARD_PLAYER_MISMATCH`.
- Invalid token returns `REWARD_INVALID_TOKEN`.
- Anti-cheat rejection returns `REWARD_REJECTED`.

### Arcade Spin Compatibility

- `GET /arcade/spin/segments` remains usable for display.
- New `POST /arcade/spin/start` returns segment/animation hints chosen by backend.
- Updated claim path ignores client-selected segment authority.
- Legacy client behavior is either supported with explicit compatibility rules or blocked after migration.

### Persistence

- Pending reward and claim ledger are written transactionally.
- Claim ledger uniqueness prevents duplicate wallet/inventory mutation.
- Audit records are emitted for applied, duplicate, rejected, expired, and suspicious flows.

---

## 13. Related Existing Docs

- [`docs/frontend/Synaptix_Arcade_Reward_Reactor_Flutter_Implementation_Blueprint.md`](../frontend/Synaptix_Arcade_Reward_Reactor_Flutter_Implementation_Blueprint.md)
- [`docs/store/spin_and_earn_backend_handoff_net10.md`](../store/spin_and_earn_backend_handoff_net10.md)
- [`docs/releases/ALPHA_ENABLED_FEATURES.md`](../releases/ALPHA_ENABLED_FEATURES.md)
- [`docs/releases/ALPHA_KNOWN_ISSUES.md`](../releases/ALPHA_KNOWN_ISSUES.md)

---

## 14. Final Recommendation

Build one shared reward authority pipeline and let Reward Reactor and Arcade Spin use different presentation layers on top of it.

The implementation should converge on:

```text
Server-generated reward outcome
+
Persisted pending reward
+
Animation-safe client payload
+
Idempotent claim
+
Wallet/inventory/progression transaction
+
Audit and anti-cheat evidence
```

This keeps Synaptix reward experiences exciting in Flutter while preserving backend authority, anti-cheat safety, and long-term compatibility with missions, seasons, inventory, economy, and operator auditing.

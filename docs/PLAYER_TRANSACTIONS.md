# Player Transactions

This document describes the two player-facing transaction systems in TycoonTycoon: **Economy Transactions** (currency ledger) and **Season Point Transactions** (ranked-season points ledger). Both are append-only, idempotent, and event-sourced.

---

## Table of Contents

1. [Overview](#overview)
2. [Economy Transactions](#economy-transactions)
   - [Domain Model](#economy-domain-model)
   - [Enums](#economy-enums)
   - [DTOs](#economy-dtos)
   - [Service Layer](#economy-service-layer)
   - [API Endpoints](#economy-api-endpoints)
   - [Database Schema](#economy-database-schema)
3. [Season Point Transactions](#season-point-transactions)
   - [Domain Model](#season-point-domain-model)
   - [DTOs](#season-point-dtos)
   - [Service Layer](#season-point-service-layer)
   - [Database Schema](#season-point-database-schema)
4. [Related Entities](#related-entities)
5. [Idempotency & Duplicate Handling](#idempotency--duplicate-handling)
6. [Rollback / Reversal](#rollback--reversal)
7. [Operator Dashboard Integration](#operator-dashboard-integration)

---

## Overview

There is **no single `PlayerTransaction` entity**. Instead, the codebase splits player transactions into two purpose-specific ledgers:

| Ledger | Entity | Table | Currencies |
|---|---|---|---|
| Economy | `EconomyTransaction` + `EconomyTransactionLine` | `economy_transactions` / `economy_transaction_lines` | Xp, Coins, Diamonds |
| Season Points | `SeasonPointTransaction` | `season_point_transactions` | RankPoints (single int delta) |

Both ledgers use an `EventId` (GUID) as an idempotency key. Submitting the same `EventId` twice is a no-op that returns a `Duplicate` status.

---

## Economy Transactions

### Economy Domain Model

**`EconomyTransaction`** (`Tycoon.Backend.Domain/Entities/EconomyTransaction.cs`)

| Property | Type | Description |
|---|---|---|
| `Id` | `Guid` | Primary key (auto-generated) |
| `ReversalOfTransactionId` | `Guid?` | Links to original transaction when this is a rollback |
| `EventId` | `Guid` | Idempotency key (unique index) |
| `PlayerId` | `Guid` | Owning player (indexed) |
| `Kind` | `string` (max 64) | Category tag, e.g. `"mission-complete"`, `"referral-redeem"`, `"skill-unlock"`, `"rollback:mission-complete"` |
| `Note` | `string?` (max 512) | Optional human-readable note |
| `CreatedAtUtc` | `DateTimeOffset` | Timestamp |
| `Lines` | `List<EconomyTransactionLine>` | One or more currency deltas (cascade delete) |

**`EconomyTransactionLine`** (same file)

| Property | Type | Description |
|---|---|---|
| `Id` | `Guid` | Primary key |
| `EconomyTransactionId` | `Guid` | FK to parent transaction (indexed) |
| `Currency` | `CurrencyType` | Which currency |
| `Delta` | `int` | Signed change (+credit, -debit) |

### Economy Enums

**`CurrencyType`** (`Tycoon.Shared.Contracts/Dtos/EconomyDtos.cs`)

```csharp
public enum CurrencyType
{
    Xp       = 1,
    Coins    = 2,
    Diamonds = 3
}
```

**`EconomyTxnStatus`** (same file)

```csharp
public enum EconomyTxnStatus
{
    Applied           = 1,   // Success - wallet updated
    Duplicate         = 2,   // EventId already processed
    InsufficientFunds = 3,   // Would drive a balance negative
    Invalid           = 4,   // No lines supplied
}
```

### Economy DTOs

All defined in `Tycoon.Shared.Contracts/Dtos/EconomyDtos.cs`.

**Request:**

```csharp
public sealed record CreateEconomyTxnRequest(
    Guid EventId,
    Guid PlayerId,
    string Kind,                         // e.g. "mission-complete", "referral-redeem"
    IReadOnlyList<EconomyLineDto> Lines,
    string? Note = null
);
```

**Line item:**

```csharp
public sealed record EconomyLineDto(CurrencyType Currency, int Delta);
```

**Result:**

```csharp
public sealed record EconomyTxnResultDto(
    Guid EventId,
    Guid PlayerId,
    EconomyTxnStatus Status,
    IReadOnlyList<EconomyLineDto> AppliedLines,
    int BalanceXp,
    int BalanceCoins,
    int BalanceDiamonds,
    DateTimeOffset ProcessedAtUtc
);
```

**History (paginated):**

```csharp
public sealed record EconomyHistoryDto(
    Guid PlayerId, int Page, int PageSize, int Total,
    IReadOnlyList<EconomyTxnListItemDto> Items
);

public sealed record EconomyTxnListItemDto(
    Guid EventId, string Kind,
    IReadOnlyList<EconomyLineDto> Lines,
    DateTimeOffset CreatedAtUtc
);
```

**Rollback request:**

```csharp
public sealed record AdminRollbackEconomyRequest(Guid EventId, string Reason);
```

### Economy Service Layer

**`EconomyService`** (`Tycoon.Backend.Application/Economy/EconomyService.cs`)

| Method | Signature | Description |
|---|---|---|
| `ApplyAsync` | `(CreateEconomyTxnRequest, CancellationToken) -> EconomyTxnResultDto` | Core ledger write. Checks for duplicates, validates balances, atomically updates `PlayerWallet` and persists the transaction. |
| `GetHistoryAsync` | `(Guid playerId, int page, int pageSize, CancellationToken) -> EconomyHistoryDto` | Paginated read of a player's transaction log, ordered newest-first. |
| `RollbackByEventIdAsync` | `(Guid eventId, string reason, CancellationToken) -> EconomyTxnResultDto` | Creates a counter-transaction that negates the original. Sets `ReversalOfTransactionId`. Prevents double-rollback. |

**Flow — `ApplyAsync`:**

1. Fast-path duplicate check (`EventId` already exists).
2. Validate at least one line is present.
3. Sum deltas per currency (Xp, Coins, Diamonds).
4. Load or create `PlayerWallet`.
5. `CanApply()` — reject if any balance would go negative (`InsufficientFunds`).
6. `Apply()` — mutate wallet balances.
7. Persist `EconomyTransaction` + lines.
8. On `DbUpdateException` (race condition on unique `EventId` index), treat as duplicate.

### Economy API Endpoints

All mounted under `admin.MapGroup("/economy")` in `Tycoon.Backend.Api/Features/AdminEconomy/AdminEconomyEndpoints.cs`.

| Method | Route | Description |
|---|---|---|
| `POST` | `/admin/economy/transactions` | Apply a new economy transaction |
| `GET` | `/admin/economy/history/{playerId}` | Get paginated transaction history for a player (`?page=&pageSize=`) |
| `POST` | `/admin/economy/rollback` | Rollback a transaction by `EventId` |
| `GET` | `/admin/economy/balance` | Get current game balance configuration |
| `PATCH` | `/admin/economy/balance` | Update game balance configuration |
| `POST` | `/admin/economy/simulate` | Run an economy simulation |

Mobile endpoints under `/mobile/economy` (`MobileEconomyEndpoints.cs`):

| Method | Route | Description |
|---|---|---|
| `GET` | `/mobile/economy/state` | Get current energy/mode/safeguard config |
| `POST` | `/mobile/economy/session/start` | Start a session (returns adjusted costs) |
| `POST` | `/mobile/economy/daily-jackpot-ticket/claim` | Claim daily jackpot ticket |
| `POST` | `/mobile/economy/revive/quote` | Get revive gem cost quote |
| `POST` | `/mobile/economy/pity/report-loss` | Report a loss (pity system) |
| `POST` | `/mobile/economy/pity/report-win` | Report a win (resets pity) |

### Economy Database Schema

**Table: `economy_transactions`**

| Column | Type | Constraints |
|---|---|---|
| `id` | `uuid` | PK |
| `reversal_of_transaction_id` | `uuid?` | Indexed |
| `event_id` | `uuid` | NOT NULL, UNIQUE INDEX (idempotency) |
| `player_id` | `uuid` | NOT NULL, INDEXED |
| `kind` | `varchar(64)` | NOT NULL |
| `note` | `varchar(512)` | Nullable |
| `created_at_utc` | `timestamptz` | NOT NULL |

**Table: `economy_transaction_lines`**

| Column | Type | Constraints |
|---|---|---|
| `id` | `uuid` | PK |
| `economy_transaction_id` | `uuid` | NOT NULL, INDEXED, FK -> `economy_transactions.id` CASCADE |
| `currency` | `int` (enum) | NOT NULL |
| `delta` | `int` | NOT NULL |

---

## Season Point Transactions

### Season Point Domain Model

**`SeasonPointTransaction`** (`Tycoon.Backend.Domain/Entities/SeasonPointTransaction.cs`)

| Property | Type | Description |
|---|---|---|
| `Id` | `Guid` | Primary key |
| `EventId` | `Guid` | Idempotency key (unique index) |
| `SeasonId` | `Guid` | Which season this applies to |
| `PlayerId` | `Guid` | Owning player |
| `Kind` | `string` (max 48) | Category, e.g. `"match-result"`, `"admin-adjust"` |
| `Delta` | `int` | Signed point change |
| `Note` | `string?` | Optional note |
| `CreatedAtUtc` | `DateTimeOffset` | Timestamp |

### Season Point DTOs

Defined in `Tycoon.Shared.Contracts/Dtos/SeasonDtos.cs`.

**Request:**

```csharp
public sealed record ApplySeasonPointsRequest(
    Guid EventId,
    Guid SeasonId,
    Guid PlayerId,
    string Kind,     // "match-result", "admin-adjust", etc.
    int Delta,
    string? Note
);
```

**Result:**

```csharp
public sealed record ApplySeasonPointsResultDto(
    Guid EventId,
    Guid SeasonId,
    Guid PlayerId,
    string Status,        // "Applied" | "Duplicate"
    int NewRankPoints
);
```

### Season Point Service Layer

**`SeasonPointsService`** (`Tycoon.Backend.Application/Seasons/SeasonPointsService.cs`)

| Method | Signature | Description |
|---|---|---|
| `ApplyAsync` | `(ApplySeasonPointsRequest, CancellationToken) -> ApplySeasonPointsResultDto` | Idempotent write. Creates transaction row and updates `PlayerSeasonProfile.RankPoints`. |
| `GetActiveSeasonAsync` | `(CancellationToken) -> Season?` | Returns the currently active season. |

**Flow — `ApplyAsync`:**

1. Duplicate check on `EventId`.
2. Load or create `PlayerSeasonProfile` for `(SeasonId, PlayerId)`.
3. Persist `SeasonPointTransaction`.
4. Call `profile.ApplyPoints(delta)`.
5. On `DbUpdateException`, treat as duplicate.

### Season Point Database Schema

**Table: `season_point_transactions`**

| Column | Type | Constraints |
|---|---|---|
| `id` | `uuid` | PK |
| `event_id` | `uuid` | NOT NULL, UNIQUE INDEX |
| `season_id` | `uuid` | NOT NULL |
| `player_id` | `uuid` | NOT NULL |
| `kind` | `varchar(48)` | NOT NULL |
| `delta` | `int` | NOT NULL |
| `note` | `text` | Nullable |
| `created_at_utc` | `timestamptz` | NOT NULL, INDEXED |

**Composite index:** `(season_id, player_id)` for fast per-player-per-season queries.

---

## Related Entities

### PlayerWallet

(`Tycoon.Backend.Domain/Entities/PlayerWallet.cs`)

The **running balance** for a player's Economy currencies. Updated atomically alongside `EconomyTransaction`.

| Property | Type | Description |
|---|---|---|
| `Id` | `Guid` | PK |
| `PlayerId` | `Guid` | Unique — one wallet per player |
| `Xp` | `int` | Current XP balance |
| `Coins` | `int` | Current Coins balance |
| `Diamonds` | `int` | Current Diamonds (premium currency) balance |
| `UpdatedAtUtc` | `DateTimeOffset` | Last modification timestamp |

Key methods:
- `CanApply(dxp, dcoins, ddiamonds)` — returns `false` if any balance would go negative.
- `Apply(dxp, dcoins, ddiamonds)` — mutates balances in-place.

### PlayerSeasonProfile

The running state for a player within a specific season. Updated alongside `SeasonPointTransaction`.

Key fields: `SeasonId`, `PlayerId`, `RankPoints`, `Wins`, `Losses`, `Draws`, `MatchesPlayed`, `Tier`, `TierRank`, `SeasonRank`.

---

## Idempotency & Duplicate Handling

Both transaction systems use the same pattern:

1. **Unique index** on `EventId` at the database level.
2. **Fast-path check** — `AnyAsync(x => x.EventId == req.EventId)` before doing any work.
3. **Race-condition safety** — if the unique index throws `DbUpdateException` during `SaveChangesAsync`, the service catches it and returns `Duplicate` status instead of an error.

Callers must generate a deterministic `EventId` for each logical operation (e.g., hash of `matchId + playerId + action`) to ensure retries are safe.

---

## Rollback / Reversal

Only **Economy Transactions** support explicit rollback (via `EconomyService.RollbackByEventIdAsync`).

**How it works:**

1. Look up the original transaction by `EventId`.
2. Check that no existing rollback already points to it (`ReversalOfTransactionId`).
3. Create a new transaction with negated line deltas and `Kind = "rollback:{originalKind}"`.
4. Apply the counter-transaction through the normal `ApplyAsync` flow (which updates the wallet).
5. Set `ReversalOfTransactionId` on the new transaction to link it to the original.

**Guards:**
- 404 if original `EventId` not found.
- 409 Conflict if already rolled back.
- Rollback itself is idempotent via its own generated `EventId`.

Season Point Transactions do not currently have a built-in rollback mechanism — adjustments are made by applying a new transaction with a negative `Delta` and `Kind = "admin-adjust"`.

---

## Operator Dashboard Integration

The Next.js Operator Dashboard (`Tycoon.OperatorDashboard.Web`) consumes the admin API via `economyService.ts`:

```typescript
export const economyService = {
  // GET /admin/players/{playerId}/economy-history
  history(playerId, { page?, pageSize? }),

  // POST /admin/players/transactions
  createTransaction(req: CreateEconomyTxnRequest),

  // POST /admin/economy/rollback
  rollback(eventId, reason),
}
```

The dashboard `Transactions.tsx` component on the main dashboard page shows aggregate transaction statistics (sales, users, products, revenue cards) — this is a placeholder/template component not yet wired to live transaction data.

---

## File Reference

| Layer | File | Purpose |
|---|---|---|
| Domain | `Tycoon.Backend.Domain/Entities/EconomyTransaction.cs` | Economy transaction + line entities |
| Domain | `Tycoon.Backend.Domain/Entities/SeasonPointTransaction.cs` | Season point transaction entity |
| Domain | `Tycoon.Backend.Domain/Entities/PlayerWallet.cs` | Running currency balances |
| Contracts | `Tycoon.Shared.Contracts/Dtos/EconomyDtos.cs` | Economy DTOs, enums, request/response records |
| Contracts | `Tycoon.Shared.Contracts/Dtos/SeasonDtos.cs` | Season DTOs including point transaction records |
| Application | `Tycoon.Backend.Application/Economy/EconomyService.cs` | Economy transaction service (apply, history, rollback) |
| Application | `Tycoon.Backend.Application/Seasons/SeasonPointsService.cs` | Season points service (apply) |
| Infrastructure | `Tycoon.Backend.Infrastructure/Persistence/Configurations/EconomyTransactionConfiguration.cs` | EF Core config for `economy_transactions` |
| Infrastructure | `Tycoon.Backend.Infrastructure/Persistence/Configurations/EconomyTransactionLineConfiguration.cs` | EF Core config for `economy_transaction_lines` |
| Infrastructure | `Tycoon.Backend.Infrastructure/Persistence/Configurations/SeasonPointTransactionConfiguration.cs` | EF Core config for `season_point_transactions` |
| Infrastructure | `Tycoon.Backend.Infrastructure/Persistence/AppDb.cs` | DbContext — exposes `DbSet` for all transaction entities |
| API | `Tycoon.Backend.Api/Features/AdminEconomy/AdminEconomyEndpoints.cs` | Admin economy endpoints |
| API | `Tycoon.Backend.Api/Features/Mobile/Economy/MobileEconomyEndpoints.cs` | Mobile economy endpoints |
| Dashboard | `Tycoon.OperatorDashboard.Web/src/lib/services/economyService.ts` | Frontend API client |

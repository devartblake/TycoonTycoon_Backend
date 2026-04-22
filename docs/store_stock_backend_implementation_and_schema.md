# Trivia Tycoon / Synaptix Store Stock System
## Full Backend Implementation Plan (C# / FastAPI Hybrid) + PostgreSQL Schema & Migrations

Date: 2026-04-21

---

## 1. Objective

This package defines a production-ready backend design for a **player-scoped store stock system** with:

- per-user stock limits
- stock reset intervals
- item-level availability windows
- purchase validation and stock decrementing
- reward-claim limits
- support for premium items, flash sales, cosmetics, currency packs, and power-ups
- integration points for ML personalization via FastAPI

This design is intended to fit the current backend direction where the .NET API owns the transactional store flows and the FastAPI sidecar can provide personalization or churn-risk scoring.

---

## 2. Recommended Responsibility Split

### .NET / C# service responsibilities

The .NET API should remain the **system of record** for:

- store catalog retrieval
- player-specific stock calculation
- purchase and reward claim validation
- transaction safety
- wallet updates
- stock consumption
- inventory ownership
- reset logic
- admin stock tuning endpoints

### FastAPI responsibilities

The FastAPI sidecar should provide **advisory personalization** only:

- churn-risk scoring
- offer personalization
- dynamic quantity boosts
- discount recommendations
- audience segmentation
- stock policy overrides based on player behavior

### Why this split is correct

Stock and purchase logic is stateful, financial, and concurrency-sensitive. That belongs in the transactional .NET service and Postgres. ML-driven adjustments are probabilistic and should remain optional, isolated, and easy to disable.

---

## 3. High-Level Backend Flow

### Read flow

1. Flutter calls `GET /store/catalog/{playerId}`
2. .NET loads global catalog items
3. .NET loads the player's stock state for those items
4. .NET lazily resets any expired player stock rows
5. .NET optionally asks FastAPI for personalization adjustments
6. .NET returns a player-specific catalog payload with:
   - remaining quantity
   - reset time
   - sold-out state
   - active discounts
   - item availability state

### Purchase flow

1. Flutter calls `POST /store/purchase`
2. .NET validates item availability
3. .NET locks the relevant player stock row
4. .NET lazily resets stock if needed
5. .NET verifies remaining quantity and player balance
6. .NET decrements stock
7. .NET writes transaction + inventory records
8. .NET commits atomically
9. .NET returns updated wallet + stock state

### Reward claim flow

1. Flutter calls `POST /store/rewards/{playerId}/claim/{rewardId}`
2. .NET checks claim interval and max claims for the reward
3. .NET creates or updates the stock/claim state row
4. .NET awards the reward
5. .NET returns updated reward claim availability

---

## 4. Domain Model

### Core concepts

#### Store catalog item
The global item definition used by all players.

#### Stock policy
The rules for how an item behaves per user:

- unlimited
- per-user quantity cap
- one-time purchase
- time-limited
- seasonal
- claim-limited

#### Player stock state
The player-specific state for a catalog item:

- quantity used
- quantity remaining
- last reset timestamp
- next reset timestamp
- dynamic override quantity

#### Personalization adjustment
Optional override values returned by FastAPI, such as:

- extra quantity
- discount percent
- boosted availability
- player segment tag

---

## 5. Recommended API Surface

## 5.1 Existing routes to keep

These should remain and be extended rather than replaced.

- `GET /store/catalog`
- `GET /store/catalog/{sku}`
- `GET /store/premium`
- `GET /store/rewards/{playerId}`
- `POST /store/rewards/{playerId}/claim/{rewardId}`
- `GET /store/inventory/{playerId}`
- `GET /store/subscription/status/{playerId}`

## 5.2 New routes to add

### Player-specific catalog

`GET /store/catalog/{playerId}`

Purpose:
Return the store catalog resolved for a specific player, including stock and reset information.

#### Response shape

```json
{
  "playerId": "player_123",
  "generatedAt": "2026-04-21T14:00:00Z",
  "items": [
    {
      "sku": "coin_pack_small",
      "title": "Small Coin Pack",
      "type": "currency",
      "price": 100,
      "priceCurrency": "coins",
      "isAvailable": true,
      "remainingQuantity": 2,
      "maxQuantity": 5,
      "resetInterval": "daily",
      "lastResetAt": "2026-04-21T00:00:00Z",
      "nextResetAt": "2026-04-22T00:00:00Z",
      "soldOut": false,
      "discountPercent": 0,
      "availabilityStart": null,
      "availabilityEnd": null,
      "tags": ["starter", "currency"]
    }
  ]
}
```

### Purchase endpoint

`POST /store/purchase`

#### Request

```json
{
  "playerId": "player_123",
  "sku": "coin_pack_small",
  "quantity": 1,
  "clientRequestId": "b64c5408-d769-4b4a-9209-801f36b0f61d"
}
```

#### Response

```json
{
  "success": true,
  "transactionId": "txn_001",
  "sku": "coin_pack_small",
  "quantityPurchased": 1,
  "remainingQuantity": 1,
  "nextResetAt": "2026-04-22T00:00:00Z",
  "wallet": {
    "coins": 1250,
    "gems": 50
  }
}
```

### Preview personalized store

`POST /store/catalog/{playerId}/personalize`

Purpose:
Force-refresh personalized boosts or discounts from FastAPI.

### Admin stock policy endpoints

- `GET /admin/store/stock-policies`
- `PUT /admin/store/stock-policies/{sku}`
- `POST /admin/store/stock-policies/bulk-reset`
- `GET /admin/store/player-stock/{playerId}`
- `POST /admin/store/player-stock/{playerId}/override`

---

## 6. C# Implementation Plan

## 6.1 Folder layout

```text
Tycoon.Backend.Api/
  Features/
    Store/
      StoreEndpoints.cs
      StorePurchaseEndpoints.cs
      StoreAdminStockEndpoints.cs
      Contracts/
        StoreCatalogItemResponse.cs
        PlayerStoreCatalogResponse.cs
        PurchaseStoreItemRequest.cs
        PurchaseStoreItemResponse.cs
        StoreStockPolicyDto.cs
        PlayerStoreItemStateDto.cs
      Services/
        StoreCatalogService.cs
        StoreStockService.cs
        StorePurchaseService.cs
        StoreResetService.cs
        StorePricingService.cs
        StorePersonalizationService.cs
      Persistence/
        Entities/
          StoreCatalogItem.cs
          StoreStockPolicy.cs
          PlayerStoreStockState.cs
          PlayerStorePurchase.cs
          PlayerStoreInventoryItem.cs
          RewardClaimRule.cs
        Repositories/
          IStoreCatalogRepository.cs
          IPlayerStoreStockRepository.cs
```

---

## 6.2 Core entities

### StoreCatalogItem

```csharp
public sealed class StoreCatalogItem
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty; // currency, powerup, cosmetic, premium, reward
    public string PriceCurrency { get; set; } = string.Empty; // coins, gems, usd
    public decimal PriceAmount { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? AvailabilityStartUtc { get; set; }
    public DateTimeOffset? AvailabilityEndUtc { get; set; }
    public bool RequiresPremium { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }

    public StoreStockPolicy? StockPolicy { get; set; }
}
```

### StoreStockPolicy

```csharp
public sealed class StoreStockPolicy
{
    public Guid Id { get; set; }
    public Guid StoreCatalogItemId { get; set; }
    public string PolicyType { get; set; } = string.Empty; // unlimited, per_user, one_time_purchase, claim_limited, time_limited
    public int? MaxQuantity { get; set; }
    public string? ResetInterval { get; set; } // hourly, daily, weekly, seasonal
    public int? ResetEveryMinutes { get; set; }
    public int? MaxClaimsPerInterval { get; set; }
    public bool IsResetEnabled { get; set; } = true;
    public bool AllowDynamicOverride { get; set; } = true;
    public DateTimeOffset? ExpiresAtUtc { get; set; }
    public string MetadataJson { get; set; } = "{}";

    public StoreCatalogItem StoreCatalogItem { get; set; } = default!;
}
```

### PlayerStoreStockState

```csharp
public sealed class PlayerStoreStockState
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public int QuantityUsed { get; set; }
    public int QuantityRemaining { get; set; }
    public int? EffectiveMaxQuantity { get; set; }
    public DateTimeOffset? LastResetAtUtc { get; set; }
    public DateTimeOffset? NextResetAtUtc { get; set; }
    public string ResetInterval { get; set; } = string.Empty;
    public string Source { get; set; } = "system"; // system, ml_override, admin_override
    public string MetadataJson { get; set; } = "{}";
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }
}
```

### PlayerStorePurchase

```csharp
public sealed class PlayerStorePurchase
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPriceAmount { get; set; }
    public string PriceCurrency { get; set; } = string.Empty;
    public string ClientRequestId { get; set; } = string.Empty;
    public string Status { get; set; } = "completed";
    public string MetadataJson { get; set; } = "{}";
    public DateTimeOffset PurchasedUtc { get; set; }
}
```

### PlayerStoreInventoryItem

```csharp
public sealed class PlayerStoreInventoryItem
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public int QuantityOwned { get; set; }
    public string OwnershipType { get; set; } = "purchased"; // purchased, reward, subscription, granted
    public DateTimeOffset? ExpiresAtUtc { get; set; }
    public string MetadataJson { get; set; } = "{}";
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }
}
```

---

## 6.3 Service design

### StoreStockService responsibilities

- resolve player-specific stock
- lazily reset expired stock rows
- apply stock policy
- enforce max quantity
- return remaining quantity + reset timestamps

#### Interface

```csharp
public interface IStoreStockService
{
    Task<PlayerStoreCatalogResponse> GetCatalogForPlayerAsync(Guid playerId, CancellationToken ct);
    Task<StoreItemAvailabilityResult> GetAvailabilityAsync(Guid playerId, string sku, CancellationToken ct);
    Task<ConsumeStockResult> ConsumeStockAsync(Guid playerId, string sku, int quantity, CancellationToken ct);
    Task ResetStockIfNeededAsync(Guid playerId, string sku, CancellationToken ct);
}
```

### StorePurchaseService responsibilities

- validate purchases
- ensure idempotency by `clientRequestId`
- update wallet
- decrement stock
- write purchase records
- write inventory rows

#### Interface

```csharp
public interface IStorePurchaseService
{
    Task<PurchaseStoreItemResponse> PurchaseAsync(PurchaseStoreItemRequest request, CancellationToken ct);
}
```

### StorePersonalizationService responsibilities

- call FastAPI
- retrieve optional overrides
- merge discount/quantity overrides into response models
- fail open if FastAPI is unavailable

#### Interface

```csharp
public interface IStorePersonalizationService
{
    Task<StorePersonalizationResult?> GetOverridesAsync(Guid playerId, IReadOnlyCollection<string> skus, CancellationToken ct);
}
```

---

## 6.4 Lazy reset algorithm

Lazy reset is recommended over full scheduled resets because it is cheaper and works automatically as players return.

### Reset rules

A row should reset when:

- policy has reset enabled
- `next_reset_at_utc <= now`
- item is not one-time purchase

### Sample implementation

```csharp
public async Task ResetStockIfNeededAsync(Guid playerId, string sku, CancellationToken ct)
{
    var stock = await _stockRepo.GetForUpdateAsync(playerId, sku, ct);
    if (stock is null)
        return;

    var now = _clock.UtcNow;
    if (stock.NextResetAtUtc is null || stock.NextResetAtUtc > now)
        return;

    var catalogItem = await _catalogRepo.GetBySkuAsync(sku, ct)
        ?? throw new InvalidOperationException($"Catalog item '{sku}' was not found.");

    var policy = catalogItem.StockPolicy
        ?? throw new InvalidOperationException($"Stock policy missing for sku '{sku}'.");

    var effectiveMax = stock.EffectiveMaxQuantity ?? policy.MaxQuantity ?? int.MaxValue;

    stock.QuantityUsed = 0;
    stock.QuantityRemaining = effectiveMax;
    stock.LastResetAtUtc = now;
    stock.NextResetAtUtc = CalculateNextReset(now, policy);
    stock.UpdatedUtc = now;

    await _stockRepo.SaveChangesAsync(ct);
}
```

---

## 6.5 Purchase algorithm

### Rules

Before purchase:

- item must exist and be active
- item must be inside availability window
- premium requirement must be satisfied
- stock must be reset if expired
- remaining quantity must be enough
- player must have enough wallet balance or payment authorization
- request must not already exist by `clientRequestId`

### Transaction strategy

Use a single database transaction with row locking on:

- player wallet row
- player stock state row
- player inventory row (if updating existing quantity)

### Sample purchase flow

```csharp
public async Task<PurchaseStoreItemResponse> PurchaseAsync(PurchaseStoreItemRequest request, CancellationToken ct)
{
    await using var tx = await _db.Database.BeginTransactionAsync(ct);

    var existing = await _purchaseRepo.FindByClientRequestIdAsync(request.ClientRequestId, ct);
    if (existing is not null)
        return await BuildIdempotentResponseAsync(existing, ct);

    var catalogItem = await _catalogRepo.GetBySkuForUpdateAsync(request.Sku, ct)
        ?? throw new StoreDomainException("store_item_not_found", "Item was not found.");

    await _stockService.ResetStockIfNeededAsync(request.PlayerId, request.Sku, ct);

    var availability = await _stockService.GetAvailabilityAsync(request.PlayerId, request.Sku, ct);
    if (!availability.IsAvailable)
        throw new StoreDomainException("store_item_unavailable", availability.Reason);

    if (availability.RemainingQuantity < request.Quantity)
        throw new StoreDomainException("store_item_out_of_stock", "Not enough stock remaining.");

    await _walletService.DebitAsync(request.PlayerId, catalogItem.PriceCurrency, catalogItem.PriceAmount * request.Quantity, ct);
    await _stockService.ConsumeStockAsync(request.PlayerId, request.Sku, request.Quantity, ct);
    await _inventoryService.GrantAsync(request.PlayerId, request.Sku, request.Quantity, "purchased", ct);

    var purchase = new PlayerStorePurchase
    {
        Id = Guid.NewGuid(),
        PlayerId = request.PlayerId,
        Sku = request.Sku,
        Quantity = request.Quantity,
        UnitPriceAmount = catalogItem.PriceAmount,
        PriceCurrency = catalogItem.PriceCurrency,
        ClientRequestId = request.ClientRequestId,
        PurchasedUtc = _clock.UtcNow,
        Status = "completed"
    };

    _db.PlayerStorePurchases.Add(purchase);
    await _db.SaveChangesAsync(ct);
    await tx.CommitAsync(ct);

    return await BuildPurchaseResponseAsync(request.PlayerId, request.Sku, purchase, ct);
}
```

---

## 6.6 Reward claim implementation

Reward claim logic should use the same stock framework.

### Example reward rules

- daily login: 1 per day
- watch ad: 5 per day
- weekend reward: 1 per weekend
- event reward: 1 per event window

### Recommended model behavior

Represent reward claims as stock/claim-limited items with `PolicyType = claim_limited`.

### Benefit

This avoids a separate reward-limit subsystem and keeps all quantity resets consistent.

---

## 6.7 FastAPI sidecar design

FastAPI should not directly mutate stock. It should return recommendations.

### Endpoint

`POST /ml/store-personalization`

#### Request

```json
{
  "player_id": "player_123",
  "skus": ["coin_pack_small", "flash_multiplier_pack"],
  "context": {
    "churn_risk": 0.82,
    "segment": "returning_low_engagement",
    "premium_status": false,
    "days_since_last_login": 7
  }
}
```

#### Response

```json
{
  "player_id": "player_123",
  "overrides": [
    {
      "sku": "coin_pack_small",
      "max_quantity_override": 8,
      "discount_percent": 20,
      "reason": "high_churn_recovery"
    }
  ]
}
```

### Python Pydantic models

```python
from pydantic import BaseModel
from typing import List, Optional

class StorePersonalizationContext(BaseModel):
    churn_risk: Optional[float] = None
    segment: Optional[str] = None
    premium_status: Optional[bool] = None
    days_since_last_login: Optional[int] = None

class StorePersonalizationRequest(BaseModel):
    player_id: str
    skus: List[str]
    context: StorePersonalizationContext

class StoreItemOverride(BaseModel):
    sku: str
    max_quantity_override: Optional[int] = None
    discount_percent: Optional[float] = None
    reason: Optional[str] = None

class StorePersonalizationResponse(BaseModel):
    player_id: str
    overrides: List[StoreItemOverride]
```

### FastAPI route

```python
from fastapi import APIRouter

router = APIRouter(prefix="/ml", tags=["ml-store"])

@router.post("/store-personalization", response_model=StorePersonalizationResponse)
def store_personalization(request: StorePersonalizationRequest) -> StorePersonalizationResponse:
    overrides: list[StoreItemOverride] = []

    if request.context.churn_risk and request.context.churn_risk >= 0.8:
        for sku in request.skus:
            if sku == "coin_pack_small":
                overrides.append(
                    StoreItemOverride(
                        sku=sku,
                        max_quantity_override=8,
                        discount_percent=20,
                        reason="high_churn_recovery"
                    )
                )

    return StorePersonalizationResponse(
        player_id=request.player_id,
        overrides=overrides,
    )
```

---

## 7. PostgreSQL Schema Design

## 7.1 Tables overview

### Required tables

1. `store_catalog_items`
2. `store_stock_policies`
3. `player_store_stock_states`
4. `player_store_purchases`
5. `player_store_inventory_items`
6. `store_price_overrides` (optional)
7. `store_personalization_audit` (optional)

---

## 7.2 Table definitions

### 7.2.1 store_catalog_items

```sql
CREATE TABLE store_catalog_items (
    id UUID PRIMARY KEY,
    sku TEXT NOT NULL UNIQUE,
    title TEXT NOT NULL,
    description TEXT NOT NULL DEFAULT '',
    item_type TEXT NOT NULL,
    price_currency TEXT NOT NULL,
    price_amount NUMERIC(18,2) NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    requires_premium BOOLEAN NOT NULL DEFAULT FALSE,
    availability_start_utc TIMESTAMPTZ NULL,
    availability_end_utc TIMESTAMPTZ NULL,
    payload_json JSONB NOT NULL DEFAULT '{}'::jsonb,
    created_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX ix_store_catalog_items_item_type ON store_catalog_items (item_type);
CREATE INDEX ix_store_catalog_items_is_active ON store_catalog_items (is_active);
CREATE INDEX ix_store_catalog_items_availability ON store_catalog_items (availability_start_utc, availability_end_utc);
```

### 7.2.2 store_stock_policies

```sql
CREATE TABLE store_stock_policies (
    id UUID PRIMARY KEY,
    store_catalog_item_id UUID NOT NULL REFERENCES store_catalog_items(id) ON DELETE CASCADE,
    policy_type TEXT NOT NULL,
    max_quantity INT NULL,
    reset_interval TEXT NULL,
    reset_every_minutes INT NULL,
    max_claims_per_interval INT NULL,
    is_reset_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    allow_dynamic_override BOOLEAN NOT NULL DEFAULT TRUE,
    expires_at_utc TIMESTAMPTZ NULL,
    metadata_json JSONB NOT NULL DEFAULT '{}'::jsonb,
    created_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_store_stock_policies_item UNIQUE (store_catalog_item_id)
);

CREATE INDEX ix_store_stock_policies_policy_type ON store_stock_policies (policy_type);
CREATE INDEX ix_store_stock_policies_reset_interval ON store_stock_policies (reset_interval);
```

### 7.2.3 player_store_stock_states

```sql
CREATE TABLE player_store_stock_states (
    id UUID PRIMARY KEY,
    player_id UUID NOT NULL,
    sku TEXT NOT NULL,
    quantity_used INT NOT NULL DEFAULT 0,
    quantity_remaining INT NOT NULL DEFAULT 0,
    effective_max_quantity INT NULL,
    last_reset_at_utc TIMESTAMPTZ NULL,
    next_reset_at_utc TIMESTAMPTZ NULL,
    reset_interval TEXT NOT NULL DEFAULT '',
    source TEXT NOT NULL DEFAULT 'system',
    metadata_json JSONB NOT NULL DEFAULT '{}'::jsonb,
    row_version BIGINT NOT NULL DEFAULT 0,
    created_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_player_store_stock_states_player_sku UNIQUE (player_id, sku)
);

CREATE INDEX ix_player_store_stock_states_player_id ON player_store_stock_states (player_id);
CREATE INDEX ix_player_store_stock_states_sku ON player_store_stock_states (sku);
CREATE INDEX ix_player_store_stock_states_next_reset_at_utc ON player_store_stock_states (next_reset_at_utc);
```

Note:
`row_version` can be used as an optimistic concurrency counter if preferred over a SQL Server-style rowversion byte array.

### 7.2.4 player_store_purchases

```sql
CREATE TABLE player_store_purchases (
    id UUID PRIMARY KEY,
    player_id UUID NOT NULL,
    sku TEXT NOT NULL,
    quantity INT NOT NULL,
    unit_price_amount NUMERIC(18,2) NOT NULL,
    price_currency TEXT NOT NULL,
    client_request_id TEXT NOT NULL,
    status TEXT NOT NULL,
    metadata_json JSONB NOT NULL DEFAULT '{}'::jsonb,
    purchased_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_player_store_purchases_client_request UNIQUE (client_request_id)
);

CREATE INDEX ix_player_store_purchases_player_id ON player_store_purchases (player_id);
CREATE INDEX ix_player_store_purchases_sku ON player_store_purchases (sku);
CREATE INDEX ix_player_store_purchases_purchased_utc ON player_store_purchases (purchased_utc DESC);
```

### 7.2.5 player_store_inventory_items

```sql
CREATE TABLE player_store_inventory_items (
    id UUID PRIMARY KEY,
    player_id UUID NOT NULL,
    sku TEXT NOT NULL,
    quantity_owned INT NOT NULL DEFAULT 0,
    ownership_type TEXT NOT NULL DEFAULT 'purchased',
    expires_at_utc TIMESTAMPTZ NULL,
    metadata_json JSONB NOT NULL DEFAULT '{}'::jsonb,
    created_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_utc TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT uq_player_store_inventory_items_player_sku UNIQUE (player_id, sku)
);

CREATE INDEX ix_player_store_inventory_items_player_id ON player_store_inventory_items (player_id);
CREATE INDEX ix_player_store_inventory_items_sku ON player_store_inventory_items (sku);
CREATE INDEX ix_player_store_inventory_items_expires_at_utc ON player_store_inventory_items (expires_at_utc);
```

### 7.2.6 store_price_overrides (optional)

Use this if you want admin or event-driven global price overrides without editing catalog rows directly.

```sql
CREATE TABLE store_price_overrides (
    id UUID PRIMARY KEY,
    sku TEXT NOT NULL,
    override_price_amount NUMERIC(18,2) NOT NULL,
    override_currency TEXT NOT NULL,
    start_utc TIMESTAMPTZ NOT NULL,
    end_utc TIMESTAMPTZ NOT NULL,
    reason TEXT NOT NULL DEFAULT '',
    created_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX ix_store_price_overrides_sku_window ON store_price_overrides (sku, start_utc, end_utc);
```

### 7.2.7 store_personalization_audit (optional)

Use this to track ML-driven stock or pricing decisions.

```sql
CREATE TABLE store_personalization_audit (
    id UUID PRIMARY KEY,
    player_id UUID NOT NULL,
    sku TEXT NOT NULL,
    source TEXT NOT NULL,
    override_json JSONB NOT NULL,
    created_utc TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX ix_store_personalization_audit_player_id ON store_personalization_audit (player_id);
CREATE INDEX ix_store_personalization_audit_sku ON store_personalization_audit (sku);
```

---

## 8. EF Core Migration Plan

## 8.1 Migration order

### Migration 1
Create catalog + stock policy tables.

- `store_catalog_items`
- `store_stock_policies`

### Migration 2
Create player stock and purchase tables.

- `player_store_stock_states`
- `player_store_purchases`
- `player_store_inventory_items`

### Migration 3
Seed base catalog data.

### Migration 4
Add optional audit / override tables.

- `store_price_overrides`
- `store_personalization_audit`

---

## 8.2 Example EF Core migration code

### Migration: CreateStoreCatalogAndPolicies

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.CreateTable(
        name: "store_catalog_items",
        columns: table => new
        {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            sku = table.Column<string>(type: "text", nullable: false),
            title = table.Column<string>(type: "text", nullable: false),
            description = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
            item_type = table.Column<string>(type: "text", nullable: false),
            price_currency = table.Column<string>(type: "text", nullable: false),
            price_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
            is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
            requires_premium = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
            availability_start_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            availability_end_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            payload_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
            created_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
            updated_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
        },
        constraints: table =>
        {
            table.PrimaryKey("pk_store_catalog_items", x => x.id);
        });

    migrationBuilder.CreateIndex(
        name: "ix_store_catalog_items_sku",
        table: "store_catalog_items",
        column: "sku",
        unique: true);

    migrationBuilder.CreateTable(
        name: "store_stock_policies",
        columns: table => new
        {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            store_catalog_item_id = table.Column<Guid>(type: "uuid", nullable: false),
            policy_type = table.Column<string>(type: "text", nullable: false),
            max_quantity = table.Column<int>(type: "integer", nullable: true),
            reset_interval = table.Column<string>(type: "text", nullable: true),
            reset_every_minutes = table.Column<int>(type: "integer", nullable: true),
            max_claims_per_interval = table.Column<int>(type: "integer", nullable: true),
            is_reset_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
            allow_dynamic_override = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
            expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            metadata_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
            created_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
            updated_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
        },
        constraints: table =>
        {
            table.PrimaryKey("pk_store_stock_policies", x => x.id);
            table.ForeignKey(
                name: "fk_store_stock_policies_store_catalog_items_store_catalog_item_id",
                column: x => x.store_catalog_item_id,
                principalTable: "store_catalog_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        });

    migrationBuilder.CreateIndex(
        name: "ix_store_stock_policies_store_catalog_item_id",
        table: "store_stock_policies",
        column: "store_catalog_item_id",
        unique: true);
}
```

### Migration: CreatePlayerStoreStateTables

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.CreateTable(
        name: "player_store_stock_states",
        columns: table => new
        {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            player_id = table.Column<Guid>(type: "uuid", nullable: false),
            sku = table.Column<string>(type: "text", nullable: false),
            quantity_used = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
            quantity_remaining = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
            effective_max_quantity = table.Column<int>(type: "integer", nullable: true),
            last_reset_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            next_reset_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            reset_interval = table.Column<string>(type: "text", nullable: false, defaultValue: ""),
            source = table.Column<string>(type: "text", nullable: false, defaultValue: "system"),
            metadata_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
            row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
            created_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
            updated_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
        },
        constraints: table =>
        {
            table.PrimaryKey("pk_player_store_stock_states", x => x.id);
        });

    migrationBuilder.CreateIndex(
        name: "ix_player_store_stock_states_player_id_sku",
        table: "player_store_stock_states",
        columns: new[] { "player_id", "sku" },
        unique: true);

    migrationBuilder.CreateTable(
        name: "player_store_purchases",
        columns: table => new
        {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            player_id = table.Column<Guid>(type: "uuid", nullable: false),
            sku = table.Column<string>(type: "text", nullable: false),
            quantity = table.Column<int>(type: "integer", nullable: false),
            unit_price_amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
            price_currency = table.Column<string>(type: "text", nullable: false),
            client_request_id = table.Column<string>(type: "text", nullable: false),
            status = table.Column<string>(type: "text", nullable: false),
            metadata_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
            purchased_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
        },
        constraints: table =>
        {
            table.PrimaryKey("pk_player_store_purchases", x => x.id);
        });

    migrationBuilder.CreateIndex(
        name: "ix_player_store_purchases_client_request_id",
        table: "player_store_purchases",
        column: "client_request_id",
        unique: true);

    migrationBuilder.CreateTable(
        name: "player_store_inventory_items",
        columns: table => new
        {
            id = table.Column<Guid>(type: "uuid", nullable: false),
            player_id = table.Column<Guid>(type: "uuid", nullable: false),
            sku = table.Column<string>(type: "text", nullable: false),
            quantity_owned = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
            ownership_type = table.Column<string>(type: "text", nullable: false, defaultValue: "purchased"),
            expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            metadata_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
            created_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
            updated_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
        },
        constraints: table =>
        {
            table.PrimaryKey("pk_player_store_inventory_items", x => x.id);
        });

    migrationBuilder.CreateIndex(
        name: "ix_player_store_inventory_items_player_id_sku",
        table: "player_store_inventory_items",
        columns: new[] { "player_id", "sku" },
        unique: true);
}
```

---

## 9. Seed Data Strategy

Seed a small initial catalog to validate the system.

### Example starter items

1. `coin_pack_small`
2. `coin_pack_medium`
3. `gem_pack_small`
4. `double_xp_boost`
5. `fifty_fifty_lifeline`
6. `avatar_neon_frame`
7. `premium_trial_reward`
8. `watch_ad_reward`

### Example seed SQL

```sql
INSERT INTO store_catalog_items (
    id, sku, title, description, item_type, price_currency, price_amount, is_active, requires_premium, payload_json
)
VALUES
(
    gen_random_uuid(),
    'coin_pack_small',
    'Small Coin Pack',
    'Starter coin pack for quick progression.',
    'currency',
    'coins',
    100,
    TRUE,
    FALSE,
    '{"coinAmount":500}'::jsonb
);
```

### Example stock policy seed

```sql
INSERT INTO store_stock_policies (
    id, store_catalog_item_id, policy_type, max_quantity, reset_interval, is_reset_enabled, allow_dynamic_override, metadata_json
)
SELECT
    gen_random_uuid(),
    sci.id,
    'per_user',
    5,
    'daily',
    TRUE,
    TRUE,
    '{}'::jsonb
FROM store_catalog_items sci
WHERE sci.sku = 'coin_pack_small';
```

---

## 10. Concurrency and Safety Requirements

## 10.1 Required protections

### Idempotency
Use `clientRequestId` on purchase requests.

### Row locking
Use `FOR UPDATE` or EF transaction locking equivalents for:

- `player_store_stock_states`
- wallet rows
- `player_store_inventory_items`

### Optimistic versioning
Increment `row_version` on stock updates if using optimistic concurrency.

### Unique constraints
Protect against duplicate rows with unique keys on:

- `(player_id, sku)` in stock state
- `(player_id, sku)` in inventory
- `client_request_id` in purchases

---

## 11. Reset Strategy Recommendation

## Recommended approach

Use **lazy reset by default**, with an optional scheduled cleanup job.

### Lazy reset advantages

- cheaper than resetting every player row on a schedule
- automatically works for inactive users returning later
- simpler to reason about
- no large midnight reset spikes

### Optional scheduled job

A Hangfire cleanup job can be added for:

- expired availability windows
- stale personalization overrides
- archiving audit rows
- precomputing next-day featured store slices

---

## 12. What the Frontend Team Should Expect

The frontend should consume a player-specific store response with these key fields per item:

- `remainingQuantity`
- `maxQuantity`
- `soldOut`
- `nextResetAt`
- `discountPercent`
- `isAvailable`
- `availabilityStart`
- `availabilityEnd`

That lets the UI render:

- sold-out badges
- limited-quantity labels
- reset countdown timers
- flash sale cards
- personalized offer callouts

---

## 13. Recommended Implementation Order

### Phase 1
Create database tables and seed starter catalog.

### Phase 2
Add `GET /store/catalog/{playerId}` with lazy reset.

### Phase 3
Add `POST /store/purchase` with idempotency + stock decrement.

### Phase 4
Refactor reward claim logic to use the shared stock-policy system.

### Phase 5
Add FastAPI personalization endpoint and fail-open merge logic.

### Phase 6
Add admin stock-policy management endpoints.

---

## 14. Final Recommendation

The stock system should be implemented as a **policy-driven, player-scoped availability engine**, not a hardcoded limit system.

That means:

- catalog items define rules
- player state tracks actual usage and resets
- .NET owns all transactional writes
- FastAPI only suggests adjustments
- Postgres enforces integrity through constraints and transactions

This will give you a store system that supports monetization, retention, premium mechanics, events, and future behavioral personalization without needing to redesign the backend later.


-- =============================================================================
-- Pending EF Migrations — Apply before May 15 cutover
-- Generated: 2026-04-29
-- Target DB: PostgreSQL (Npgsql EF Core naming conventions: snake_case)
--
-- Run order matters — execute top-to-bottom in a single transaction.
-- After applying, run: dotnet ef database update (or re-run to confirm idempotency)
-- =============================================================================

BEGIN;

-- ─── Record applied migrations in EF history table ───────────────────────────
-- EF will skip migrations already listed here; only add rows for ones you apply.

-- =============================================================================
-- 1. 20260422132225_AddStoreItemAvatarFields
--    Adds thumbnail_url, is_featured, version to store_items for avatar catalog
-- =============================================================================

ALTER TABLE store_items
    ADD COLUMN IF NOT EXISTS is_featured   boolean                      NOT NULL DEFAULT false,
    ADD COLUMN IF NOT EXISTS thumbnail_url character varying(500),
    ADD COLUMN IF NOT EXISTS version       character varying(20);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260422132225_AddStoreItemAvatarFields', '9.0.0')
ON CONFLICT DO NOTHING;

-- =============================================================================
-- 2. 20260425120000_AddSeasonRewardRules
--    Defines per-tier season rewards (XP + coins) for end-of-season payouts
-- =============================================================================

CREATE TABLE IF NOT EXISTS season_reward_rules (
    id            uuid     NOT NULL,
    tier          integer  NOT NULL,
    max_tier_rank integer  NOT NULL,
    reward_xp     integer  NOT NULL,
    reward_coins  integer  NOT NULL,
    CONSTRAINT pk_season_reward_rules PRIMARY KEY (id)
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_season_reward_rules_tier_max_tier_rank
    ON season_reward_rules (tier, max_tier_rank);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260425120000_AddSeasonRewardRules', '9.0.0')
ON CONFLICT DO NOTHING;

-- =============================================================================
-- 3. 20260425130000_AddStoreStockSystem
--    Per-SKU purchase limits (policies) and per-player purchase tracking
-- =============================================================================

CREATE TABLE IF NOT EXISTS store_stock_policies (
    id                    uuid                        NOT NULL,
    sku                   character varying(128)      NOT NULL,
    max_quantity_per_user integer                     NOT NULL,
    reset_interval        character varying(20)       NOT NULL,
    is_active             boolean                     NOT NULL DEFAULT true,
    created_at_utc        timestamp with time zone    NOT NULL,
    updated_at_utc        timestamp with time zone    NOT NULL,
    CONSTRAINT pk_store_stock_policies PRIMARY KEY (id)
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_store_stock_policies_sku
    ON store_stock_policies (sku);

CREATE TABLE IF NOT EXISTS player_store_stock_states (
    id                  uuid                        NOT NULL,
    player_id           uuid                        NOT NULL,
    sku                 character varying(128)      NOT NULL,
    quantity_used       integer                     NOT NULL,
    last_reset_at_utc   timestamp with time zone,
    next_reset_at_utc   timestamp with time zone,
    updated_at_utc      timestamp with time zone    NOT NULL,
    CONSTRAINT pk_player_store_stock_states PRIMARY KEY (id)
);

CREATE INDEX IF NOT EXISTS ix_player_store_stock_states_player_id
    ON player_store_stock_states (player_id);

CREATE UNIQUE INDEX IF NOT EXISTS ix_player_store_stock_states_player_id_sku
    ON player_store_stock_states (player_id, sku);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260425130000_AddStoreStockSystem', '9.0.0')
ON CONFLICT DO NOTHING;

-- =============================================================================
-- 4. 20260425140000_AddFlashSale
--    Time-windowed discount promotions tied to a SKU
-- =============================================================================

CREATE TABLE IF NOT EXISTS flash_sales (
    id               uuid                        NOT NULL,
    sku              character varying(128)      NOT NULL,
    discount_percent integer                     NOT NULL,
    starts_at_utc    timestamp with time zone    NOT NULL,
    ends_at_utc      timestamp with time zone    NOT NULL,
    is_active        boolean                     NOT NULL DEFAULT true,
    reason           character varying(256),
    created_at_utc   timestamp with time zone    NOT NULL,
    CONSTRAINT pk_flash_sales PRIMARY KEY (id)
);

CREATE INDEX IF NOT EXISTS ix_flash_sales_ends_at_utc
    ON flash_sales (ends_at_utc);

CREATE INDEX IF NOT EXISTS ix_flash_sales_sku_window
    ON flash_sales (sku, starts_at_utc, ends_at_utc);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260425140000_AddFlashSale', '9.0.0')
ON CONFLICT DO NOTHING;

-- =============================================================================
-- 5. 20260426100000_AddRewardClaimRule
--    Rate-limiting rules for claimable rewards (max claims per reset interval)
-- =============================================================================

CREATE TABLE IF NOT EXISTS reward_claim_rules (
    id                      uuid                        NOT NULL,
    reward_id               character varying(128)      NOT NULL,
    max_claims_per_interval integer                     NOT NULL,
    reset_interval          character varying(20)       NOT NULL,
    is_active               boolean                     NOT NULL DEFAULT true,
    created_at_utc          timestamp with time zone    NOT NULL,
    updated_at_utc          timestamp with time zone    NOT NULL,
    CONSTRAINT pk_reward_claim_rules PRIMARY KEY (id)
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_reward_claim_rules_reward_id
    ON reward_claim_rules (reward_id);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260426100000_AddRewardClaimRule', '9.0.0')
ON CONFLICT DO NOTHING;

-- =============================================================================
-- 6. 20260426110000_AddEffectiveMaxQuantity
--    Adds effective_max_quantity override column to player_store_stock_states
--    (allows per-player policy overrides set by admin)
-- =============================================================================

ALTER TABLE player_store_stock_states
    ADD COLUMN IF NOT EXISTS effective_max_quantity integer;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260426110000_AddEffectiveMaxQuantity', '9.0.0')
ON CONFLICT DO NOTHING;

-- =============================================================================
-- Done. Verify with:
--   SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId";
-- Expected last row: 20260426110000_AddEffectiveMaxQuantity
-- =============================================================================

COMMIT;

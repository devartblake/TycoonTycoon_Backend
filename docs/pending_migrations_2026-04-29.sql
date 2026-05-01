-- =============================================================================
-- Pending EF Migrations — Apply before May 15 cutover
-- Generated: 2026-04-29 | Updated: 2026-05-01
-- Target DB: PostgreSQL (Npgsql EF Core naming conventions: snake_case)
--
-- Run order matters — execute top-to-bottom in a single transaction.
-- After applying, verify with:
--   SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId";
-- Expected last row: 20260501090000_AddReasonToPersonalizationRecommendation
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
VALUES ('20260422132225_AddStoreItemAvatarFields', '9.0.11')
ON CONFLICT DO NOTHING;

-- =============================================================================
-- 2. 20260425130000_AddStoreStockSystem
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
VALUES ('20260425130000_AddStoreStockSystem', '9.0.11')
ON CONFLICT DO NOTHING;

-- =============================================================================
-- 3. 20260425140000_AddFlashSale
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
VALUES ('20260425140000_AddFlashSale', '9.0.11')
ON CONFLICT DO NOTHING;

-- =============================================================================
-- 4. 20260425150120_AddSeasonRewardRules
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
VALUES ('20260425150120_AddSeasonRewardRules', '9.0.11')
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
VALUES ('20260426100000_AddRewardClaimRule', '9.0.11')
ON CONFLICT DO NOTHING;

-- =============================================================================
-- 6. 20260426110000_AddEffectiveMaxQuantity
--    Adds effective_max_quantity override column to player_store_stock_states
--    (allows per-player policy overrides set by admin)
-- =============================================================================

ALTER TABLE player_store_stock_states
    ADD COLUMN IF NOT EXISTS effective_max_quantity integer;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260426110000_AddEffectiveMaxQuantity', '9.0.11')
ON CONFLICT DO NOTHING;

-- =============================================================================
-- 7. 20260429100000_AddPersonalizationTables
--    Creates player_mind_profiles, player_behavior_events,
--    personalization_recommendations, and personalization_rules
-- =============================================================================

CREATE TABLE IF NOT EXISTS player_mind_profiles (
    id                          uuid                        NOT NULL,
    player_id                   uuid                        NOT NULL,
    confidence_level            numeric(5,2)                NOT NULL DEFAULT 0.50,
    risk_tolerance              numeric(5,2)                NOT NULL DEFAULT 0.50,
    preferred_pace              character varying(64)       NOT NULL DEFAULT 'balanced',
    learning_style              character varying(64)       NOT NULL DEFAULT 'mixed',
    competitive_preference      character varying(64)       NOT NULL DEFAULT 'balanced',
    social_preference           character varying(64)       NOT NULL DEFAULT 'solo',
    churn_risk_score            numeric(5,2)                NOT NULL DEFAULT 0.00,
    frustration_risk_score      numeric(5,2)                NOT NULL DEFAULT 0.00,
    reward_sensitivity_score    numeric(5,2)                NOT NULL DEFAULT 0.50,
    store_affinity_score        numeric(5,2)                NOT NULL DEFAULT 0.50,
    notification_fatigue_score  numeric(5,2)                NOT NULL DEFAULT 0.00,
    archetype                   character varying(96)       NOT NULL DEFAULT 'new_player',
    category_strengths_json     jsonb                       NOT NULL DEFAULT '{}',
    category_weaknesses_json    jsonb                       NOT NULL DEFAULT '{}',
    preference_json             jsonb                       NOT NULL DEFAULT '{}',
    guardrail_json              jsonb                       NOT NULL DEFAULT '{}',
    sidecar_scores_json         jsonb                       NOT NULL DEFAULT '{}',
    personalization_enabled     boolean                     NOT NULL DEFAULT true,
    sidecar_scoring_enabled     boolean                     NOT NULL DEFAULT true,
    last_calculated_at          timestamp with time zone,
    created_at                  timestamp with time zone    NOT NULL,
    updated_at                  timestamp with time zone    NOT NULL,
    CONSTRAINT pk_player_mind_profiles PRIMARY KEY (id)
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_player_mind_profiles_player_id
    ON player_mind_profiles (player_id);
CREATE INDEX IF NOT EXISTS ix_player_mind_profiles_archetype
    ON player_mind_profiles (archetype);
CREATE INDEX IF NOT EXISTS ix_player_mind_profiles_churn_risk
    ON player_mind_profiles (churn_risk_score);
CREATE INDEX IF NOT EXISTS ix_player_mind_profiles_frustration_risk
    ON player_mind_profiles (frustration_risk_score);
CREATE INDEX IF NOT EXISTS ix_player_mind_profiles_updated_at
    ON player_mind_profiles (updated_at);

CREATE TABLE IF NOT EXISTS player_behavior_events (
    id            uuid                        NOT NULL,
    player_id     uuid                        NOT NULL,
    event_type    character varying(128)      NOT NULL,
    event_source  character varying(128)      NOT NULL,
    category      character varying(128),
    difficulty    character varying(64),
    mode          character varying(64),
    metadata_json jsonb                       NOT NULL DEFAULT '{}',
    occurred_at   timestamp with time zone    NOT NULL,
    ingested_at   timestamp with time zone    NOT NULL,
    CONSTRAINT pk_player_behavior_events PRIMARY KEY (id)
);

CREATE INDEX IF NOT EXISTS ix_player_behavior_events_player_time
    ON player_behavior_events (player_id, occurred_at);
CREATE INDEX IF NOT EXISTS ix_player_behavior_events_type
    ON player_behavior_events (event_type);
CREATE INDEX IF NOT EXISTS ix_player_behavior_events_source
    ON player_behavior_events (event_source);
CREATE INDEX IF NOT EXISTS ix_player_behavior_events_category
    ON player_behavior_events (category);

CREATE TABLE IF NOT EXISTS personalization_recommendations (
    id                  uuid                        NOT NULL,
    player_id           uuid                        NOT NULL,
    recommendation_type character varying(128)      NOT NULL,
    source              character varying(64)       NOT NULL DEFAULT 'backend',
    priority            integer                     NOT NULL DEFAULT 0,
    score               numeric(5,2)                NOT NULL DEFAULT 0.50,
    payload_json        jsonb                       NOT NULL DEFAULT '{}',
    guardrail_json      jsonb                       NOT NULL DEFAULT '{}',
    expires_at          timestamp with time zone,
    accepted_at         timestamp with time zone,
    dismissed_at        timestamp with time zone,
    created_at          timestamp with time zone    NOT NULL,
    CONSTRAINT pk_personalization_recommendations PRIMARY KEY (id)
);

CREATE INDEX IF NOT EXISTS ix_personalization_recommendations_player_created
    ON personalization_recommendations (player_id, created_at);
CREATE INDEX IF NOT EXISTS ix_personalization_recommendations_type
    ON personalization_recommendations (recommendation_type);

CREATE TABLE IF NOT EXISTS personalization_rules (
    id          uuid                        NOT NULL,
    rule_key    character varying(256)      NOT NULL,
    description character varying(512)      NOT NULL DEFAULT '',
    is_enabled  boolean                     NOT NULL DEFAULT true,
    rule_json   jsonb                       NOT NULL DEFAULT '{}',
    created_at  timestamp with time zone    NOT NULL,
    updated_at  timestamp with time zone    NOT NULL,
    CONSTRAINT pk_personalization_rules PRIMARY KEY (id)
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_personalization_rules_rule_key
    ON personalization_rules (rule_key);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260429100000_AddPersonalizationTables', '10.0.7')
ON CONFLICT DO NOTHING;

-- =============================================================================
-- 8. 20260429120000_AddExperimentTables
--    Creates experiments, experiment_variants, and experiment_assignments
-- =============================================================================

CREATE TABLE IF NOT EXISTS experiments (
    id                 uuid                        NOT NULL,
    key                character varying(128)      NOT NULL,
    name               character varying(256)      NOT NULL,
    description        character varying(1000),
    status             character varying(32)       NOT NULL DEFAULT 'draft',
    allocation_percent numeric(5,2)                NOT NULL DEFAULT 100,
    starts_at          timestamp with time zone,
    ends_at            timestamp with time zone,
    metadata_json      jsonb                       NOT NULL DEFAULT '{}',
    created_at         timestamp with time zone    NOT NULL,
    updated_at         timestamp with time zone    NOT NULL,
    CONSTRAINT "PK_experiments" PRIMARY KEY (id)
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_experiments_key"
    ON experiments (key);
CREATE INDEX IF NOT EXISTS "IX_experiments_status"
    ON experiments (status);

CREATE TABLE IF NOT EXISTS experiment_variants (
    id            uuid                        NOT NULL,
    experiment_id uuid                        NOT NULL,
    key           character varying(128)      NOT NULL,
    name          character varying(256)      NOT NULL,
    weight        numeric(6,2)                NOT NULL DEFAULT 50,
    is_control    boolean                     NOT NULL DEFAULT false,
    config_json   jsonb                       NOT NULL DEFAULT '{}',
    CONSTRAINT "PK_experiment_variants" PRIMARY KEY (id),
    CONSTRAINT "FK_experiment_variants_experiments_experiment_id"
        FOREIGN KEY (experiment_id) REFERENCES experiments (id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_experiment_variants_experiment_id_key"
    ON experiment_variants (experiment_id, key);

CREATE TABLE IF NOT EXISTS experiment_assignments (
    id               uuid                        NOT NULL,
    player_id        uuid                        NOT NULL,
    experiment_id    uuid                        NOT NULL,
    experiment_key   character varying(128)      NOT NULL,
    variant_key      character varying(128)      NOT NULL,
    assigned_at      timestamp with time zone    NOT NULL,
    first_seen_at    timestamp with time zone,
    impression_count integer                     NOT NULL DEFAULT 0,
    outcome_count    integer                     NOT NULL DEFAULT 0,
    outcome_json     jsonb                       NOT NULL DEFAULT '{}',
    CONSTRAINT "PK_experiment_assignments" PRIMARY KEY (id)
);

CREATE UNIQUE INDEX IF NOT EXISTS "IX_experiment_assignments_player_id_experiment_id"
    ON experiment_assignments (player_id, experiment_id);
CREATE INDEX IF NOT EXISTS "IX_experiment_assignments_experiment_id_variant_key"
    ON experiment_assignments (experiment_id, variant_key);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260429120000_AddExperimentTables', '10.0.7')
ON CONFLICT DO NOTHING;

-- =============================================================================
-- 9. 20260430120000_AddPersonalizationAuditLog
--    Creates personalization_audit_logs with full JSONB decision trace
-- =============================================================================

CREATE TABLE IF NOT EXISTS personalization_audit_logs (
    id                      uuid                        NOT NULL,
    player_id               uuid                        NOT NULL,
    recommendation_id       uuid,
    decision_type           character varying(128)      NOT NULL,
    source                  character varying(64)       NOT NULL DEFAULT 'backend',
    reason                  character varying(512)      NOT NULL DEFAULT '',
    input_signals_json      jsonb                       NOT NULL DEFAULT '{}',
    candidate_json          jsonb                       NOT NULL DEFAULT '{}',
    guardrails_applied_json jsonb                       NOT NULL DEFAULT '{}',
    final_decision_json     jsonb                       NOT NULL DEFAULT '{}',
    created_at              timestamp with time zone    NOT NULL,
    CONSTRAINT pk_personalization_audit_logs PRIMARY KEY (id)
);

CREATE INDEX IF NOT EXISTS ix_personalization_audit_logs_player_created
    ON personalization_audit_logs (player_id, created_at);
CREATE INDEX IF NOT EXISTS ix_personalization_audit_logs_decision_type
    ON personalization_audit_logs (decision_type);
CREATE INDEX IF NOT EXISTS ix_personalization_audit_logs_recommendation_id
    ON personalization_audit_logs (recommendation_id);

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260430120000_AddPersonalizationAuditLog', '10.0.7')
ON CONFLICT DO NOTHING;

-- =============================================================================
-- 10. 20260501090000_AddReasonToPersonalizationRecommendation
--     Adds reason column (explainability text) to personalization_recommendations
-- =============================================================================

ALTER TABLE personalization_recommendations
    ADD COLUMN IF NOT EXISTS reason character varying(512) NOT NULL DEFAULT '';

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260501090000_AddReasonToPersonalizationRecommendation', '10.0.7')
ON CONFLICT DO NOTHING;

-- =============================================================================
-- Done. Verify with:
--   SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId";
-- Expected last row: 20260501090000_AddReasonToPersonalizationRecommendation
-- =============================================================================

COMMIT;

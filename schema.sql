CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    migration_id character varying(150) NOT NULL,
    product_version character varying(32) NOT NULL,
    CONSTRAINT pk___ef_migrations_history PRIMARY KEY (migration_id)
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE admin_app_config (
        id character varying(32) NOT NULL,
        api_base_url character varying(500) NOT NULL,
        enable_logging boolean NOT NULL,
        feature_flags_json jsonb NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_admin_app_config PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE admin_email_acls (
        id uuid NOT NULL,
        email character varying(256) NOT NULL,
        normalized_email character varying(256) NOT NULL,
        list_type character varying(16) NOT NULL,
        role character varying(32) NOT NULL,
        notes character varying(500),
        added_by character varying(256) NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_admin_email_acls PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE admin_notification_channels (
        key character varying(100) NOT NULL,
        name character varying(200) NOT NULL,
        description character varying(1000) NOT NULL,
        importance character varying(32) NOT NULL,
        enabled boolean NOT NULL,
        updated_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_admin_notification_channels PRIMARY KEY (key)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE admin_notification_history (
        id character varying(100) NOT NULL,
        channel_key character varying(100) NOT NULL,
        title character varying(300) NOT NULL,
        status character varying(64) NOT NULL,
        created_at timestamp with time zone NOT NULL,
        metadata_json jsonb,
        CONSTRAINT pk_admin_notification_history PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE admin_notification_schedules (
        schedule_id character varying(100) NOT NULL,
        title character varying(300) NOT NULL,
        body character varying(2000) NOT NULL,
        channel_key character varying(100) NOT NULL,
        scheduled_at timestamp with time zone NOT NULL,
        status character varying(32) NOT NULL,
        retry_count integer NOT NULL,
        max_retries integer NOT NULL,
        last_error character varying(2000),
        processed_at_utc timestamp with time zone,
        created_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_admin_notification_schedules PRIMARY KEY (schedule_id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE admin_notification_templates (
        template_id character varying(100) NOT NULL,
        name character varying(100) NOT NULL,
        title character varying(300) NOT NULL,
        body character varying(2000) NOT NULL,
        channel_key character varying(100) NOT NULL,
        variables_json jsonb NOT NULL,
        updated_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_admin_notification_templates PRIMARY KEY (template_id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE anti_cheat_flags (
        id uuid NOT NULL,
        match_id uuid NOT NULL,
        player_id uuid,
        rule_key character varying(64) NOT NULL,
        severity integer NOT NULL,
        action integer NOT NULL,
        message character varying(300) NOT NULL,
        evidence_json text,
        created_at_utc timestamp with time zone NOT NULL,
        reviewed_at_utc timestamp with time zone,
        reviewed_by character varying(64),
        review_note character varying(400),
        CONSTRAINT pk_anti_cheat_flags PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE friend_edges (
        id uuid NOT NULL,
        player_id uuid NOT NULL,
        friend_player_id uuid NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_friend_edges PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE friend_requests (
        id uuid NOT NULL,
        from_player_id uuid NOT NULL,
        to_player_id uuid NOT NULL,
        status character varying(16) NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        responded_at_utc timestamp with time zone,
        CONSTRAINT pk_friend_requests PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE game_balance_configs (
        id character varying(32) NOT NULL,
        config_json jsonb NOT NULL,
        updated_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_game_balance_configs PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE game_event_participants (
        id uuid NOT NULL,
        game_event_id uuid NOT NULL,
        player_id uuid NOT NULL,
        entry_event_id uuid NOT NULL,
        eliminated_at timestamp with time zone,
        final_rank integer,
        revives_used integer NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_game_event_participants PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE game_event_prize_claims (
        id uuid NOT NULL,
        game_event_id uuid NOT NULL,
        player_id uuid NOT NULL,
        event_id uuid NOT NULL,
        awarded_xp integer NOT NULL,
        awarded_coins integer NOT NULL,
        rank integer NOT NULL,
        claimed_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_game_event_prize_claims PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE game_events (
        id uuid NOT NULL,
        kind character varying(32) NOT NULL,
        tier_id integer NOT NULL,
        status integer NOT NULL,
        scheduled_at_utc timestamp with time zone NOT NULL,
        open_at_utc timestamp with time zone,
        entry_fee_coins integer NOT NULL,
        revive_cost_gems integer NOT NULL,
        jackpot_pool integer NOT NULL,
        max_participants integer NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_game_events PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE guardian_challenges (
        id uuid NOT NULL,
        season_id uuid NOT NULL,
        tier_number integer NOT NULL,
        challenger_id uuid NOT NULL,
        guardian_id uuid NOT NULL,
        match_id uuid NOT NULL,
        status integer NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        resolved_at_utc timestamp with time zone,
        CONSTRAINT pk_guardian_challenges PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE leaderboard_entries (
        id uuid NOT NULL,
        player_id uuid NOT NULL,
        global_rank integer NOT NULL,
        tier_rank integer NOT NULL,
        tier_id integer NOT NULL,
        score integer NOT NULL,
        xp_progress double precision NOT NULL,
        CONSTRAINT pk_leaderboard_entries PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE match_results (
        id uuid NOT NULL,
        match_id uuid NOT NULL,
        submit_event_id uuid NOT NULL,
        mode character varying(32) NOT NULL,
        category character varying(64) NOT NULL,
        question_count integer NOT NULL,
        ended_at_utc timestamp with time zone NOT NULL,
        status integer NOT NULL,
        CONSTRAINT pk_match_results PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE matches (
        id uuid NOT NULL,
        host_player_id uuid NOT NULL,
        mode character varying(32) NOT NULL,
        started_at timestamp with time zone NOT NULL,
        finished_at timestamp with time zone,
        row_version bytea,
        CONSTRAINT pk_matches PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE matchmaking_tickets (
        id uuid NOT NULL,
        row_version bytea NOT NULL,
        player_id uuid NOT NULL,
        mode character varying(32) NOT NULL,
        tier integer NOT NULL,
        scope character varying(32) NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        expires_at_utc timestamp with time zone NOT NULL,
        status character varying(16) NOT NULL,
        CONSTRAINT pk_matchmaking_tickets PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE mission_claims (
        id uuid NOT NULL,
        player_id uuid NOT NULL,
        mission_id uuid NOT NULL,
        progress integer NOT NULL,
        completed boolean NOT NULL,
        completed_at_utc timestamp with time zone,
        claimed boolean NOT NULL,
        claimed_at_utc timestamp with time zone,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone NOT NULL,
        last_reset_at_utc timestamp with time zone,
        CONSTRAINT pk_mission_claims PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE missions (
        id uuid NOT NULL,
        type text NOT NULL,
        key text NOT NULL,
        title text NOT NULL,
        description text NOT NULL,
        goal integer NOT NULL,
        reward_xp integer NOT NULL,
        reward_coins integer NOT NULL,
        reward_diamonds integer NOT NULL,
        active boolean NOT NULL,
        CONSTRAINT pk_missions PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE moderation_action_logs (
        id uuid NOT NULL,
        player_id uuid NOT NULL,
        new_status integer NOT NULL,
        reason character varying(200),
        notes character varying(1000),
        set_by_admin character varying(120),
        created_at_utc timestamp with time zone NOT NULL,
        expires_at_utc timestamp with time zone,
        related_flag_id uuid,
        CONSTRAINT pk_moderation_action_logs PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE parties (
        id uuid NOT NULL,
        leader_player_id uuid NOT NULL,
        status character varying(16) NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_parties PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE party_invites (
        id uuid NOT NULL,
        party_id uuid NOT NULL,
        from_player_id uuid NOT NULL,
        to_player_id uuid NOT NULL,
        status character varying(16) NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        responded_at_utc timestamp with time zone,
        CONSTRAINT pk_party_invites PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE party_match_links (
        id uuid NOT NULL,
        party_id uuid NOT NULL,
        match_id uuid NOT NULL,
        status character varying(16) NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        closed_at_utc timestamp with time zone,
        CONSTRAINT pk_party_match_links PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE party_match_members (
        party_id uuid NOT NULL,
        match_id uuid NOT NULL,
        player_id uuid NOT NULL,
        role character varying(16) NOT NULL,
        captured_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_party_match_members PRIMARY KEY (party_id, match_id, player_id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE party_matchmaking_tickets (
        id uuid NOT NULL,
        party_id uuid NOT NULL,
        leader_player_id uuid NOT NULL,
        mode character varying(24) NOT NULL,
        tier integer NOT NULL,
        scope character varying(16) NOT NULL,
        party_size integer NOT NULL,
        status character varying(16) NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        expires_at_utc timestamp with time zone NOT NULL,
        row_version bigint NOT NULL,
        CONSTRAINT pk_party_matchmaking_tickets PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE party_members (
        id uuid NOT NULL,
        party_id uuid NOT NULL,
        player_id uuid NOT NULL,
        role character varying(16) NOT NULL,
        joined_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_party_members PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE player_economy_safeguard_states (
        id uuid NOT NULL,
        player_id uuid NOT NULL,
        sessions_started integer NOT NULL,
        loss_streak integer NOT NULL,
        current_energy integer NOT NULL,
        last_energy_regen_at_utc timestamp with time zone NOT NULL,
        last_free_ticket_claim_date date,
        free_tickets_claimed_today integer NOT NULL,
        updated_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_player_economy_safeguard_states PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE player_event_stats (
        id uuid NOT NULL,
        season_id uuid NOT NULL,
        player_id uuid NOT NULL,
        events_entered integer NOT NULL DEFAULT 0,
        events_top20 integer NOT NULL DEFAULT 0,
        events_won integer NOT NULL DEFAULT 0,
        total_event_xp_earned integer NOT NULL DEFAULT 0,
        total_event_coins_earned integer NOT NULL DEFAULT 0,
        champion_battle_eliminations integer NOT NULL DEFAULT 0,
        guardian_promotions integer NOT NULL DEFAULT 0,
        guardian_defences_won integer NOT NULL DEFAULT 0,
        guardian_defences_lost integer NOT NULL DEFAULT 0,
        guardian_days_total integer NOT NULL DEFAULT 0,
        tiles_ever_captured integer NOT NULL DEFAULT 0,
        current_tiles_owned integer NOT NULL DEFAULT 0,
        peak_xp_multiplier_bps integer NOT NULL DEFAULT 0,
        updated_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_player_event_stats PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE player_moderation_profiles (
        id uuid NOT NULL,
        player_id uuid NOT NULL,
        status integer NOT NULL,
        reason character varying(200),
        notes character varying(1000),
        set_by_admin character varying(120),
        set_at_utc timestamp with time zone NOT NULL,
        expires_at_utc timestamp with time zone,
        CONSTRAINT pk_player_moderation_profiles PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE player_powerups (
        id uuid NOT NULL,
        player_id uuid NOT NULL,
        type integer NOT NULL,
        quantity integer NOT NULL,
        cooldown_until_utc timestamp with time zone,
        updated_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_player_powerups PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE player_season_profiles (
        id uuid NOT NULL,
        season_id uuid NOT NULL,
        player_id uuid NOT NULL,
        rank_points integer NOT NULL,
        wins integer NOT NULL,
        losses integer NOT NULL,
        draws integer NOT NULL,
        matches_played integer NOT NULL,
        tier integer NOT NULL,
        tier_rank integer NOT NULL,
        season_rank integer NOT NULL,
        updated_at_utc timestamp with time zone NOT NULL,
        placement_matches_completed integer NOT NULL,
        last_promotion_at_utc timestamp with time zone,
        last_demotion_at_utc timestamp with time zone,
        CONSTRAINT pk_player_season_profiles PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE player_skill_unlocks (
        id uuid NOT NULL,
        player_id uuid NOT NULL,
        node_key character varying(80) NOT NULL,
        unlocked_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_player_skill_unlocks PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE player_transactions (
        id uuid NOT NULL,
        event_id uuid NOT NULL,
        correlated_event_id uuid,
        kind character varying(64) NOT NULL,
        status integer NOT NULL,
        receipt character varying(2048),
        dispute_reason character varying(1024),
        dispute_linked_to_transaction_id uuid,
        created_at_utc timestamp with time zone NOT NULL,
        completed_at_utc timestamp with time zone,
        CONSTRAINT pk_player_transactions PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE player_wallets (
        id uuid NOT NULL,
        player_id uuid NOT NULL,
        xp integer NOT NULL,
        coins integer NOT NULL,
        diamonds integer NOT NULL,
        updated_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_player_wallets PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE players (
        id uuid NOT NULL,
        username character varying(64) NOT NULL,
        country_code character varying(4) NOT NULL,
        score integer NOT NULL,
        tier_id uuid,
        level integer NOT NULL,
        xp double precision NOT NULL,
        coins integer NOT NULL,
        diamonds integer NOT NULL,
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_players PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE processed_gameplay_events (
        id uuid NOT NULL,
        event_id uuid NOT NULL,
        player_id uuid NOT NULL,
        kind character varying(64) NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_processed_gameplay_events PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE qr_scan_events (
        id uuid NOT NULL,
        event_id uuid NOT NULL,
        player_id uuid NOT NULL,
        value character varying(512) NOT NULL,
        occurred_at_utc timestamp with time zone NOT NULL,
        type integer NOT NULL,
        stored_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_qr_scan_events PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE question_answered_analytics_events (
        id text NOT NULL,
        match_id uuid NOT NULL,
        player_id uuid NOT NULL,
        question_id text NOT NULL,
        mode text NOT NULL,
        category text NOT NULL,
        difficulty integer NOT NULL,
        is_correct boolean NOT NULL,
        points_awarded integer NOT NULL,
        answer_time_ms integer NOT NULL,
        answered_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_question_answered_analytics_events PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE question_answered_daily_rollups (
        id text NOT NULL,
        day date NOT NULL,
        mode text NOT NULL,
        category text NOT NULL,
        difficulty integer NOT NULL,
        total_answers integer NOT NULL,
        correct_answers integer NOT NULL,
        wrong_answers integer NOT NULL,
        sum_answer_time_ms bigint NOT NULL,
        min_answer_time_ms integer NOT NULL,
        max_answer_time_ms integer NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_question_answered_daily_rollups PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE question_answered_player_daily_rollups (
        id text NOT NULL,
        day date NOT NULL,
        player_id uuid NOT NULL,
        mode text NOT NULL,
        category text NOT NULL,
        difficulty integer NOT NULL,
        total_answers integer NOT NULL,
        correct_answers integer NOT NULL,
        wrong_answers integer NOT NULL,
        sum_answer_time_ms bigint NOT NULL,
        min_answer_time_ms integer NOT NULL,
        max_answer_time_ms integer NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_question_answered_player_daily_rollups PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE questions (
        id uuid NOT NULL,
        text character varying(2000) NOT NULL,
        category character varying(64) NOT NULL,
        difficulty integer NOT NULL,
        correct_option_id character varying(64) NOT NULL,
        media_key character varying(256),
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_questions PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE referral_codes (
        id uuid NOT NULL,
        code character varying(32) NOT NULL,
        owner_player_id uuid NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_referral_codes PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE referral_redemptions (
        id uuid NOT NULL,
        event_id uuid NOT NULL,
        referral_code_id uuid NOT NULL,
        owner_player_id uuid NOT NULL,
        redeemer_player_id uuid NOT NULL,
        award_xp_to_owner integer NOT NULL,
        award_coins_to_owner integer NOT NULL,
        award_xp_to_redeemer integer NOT NULL,
        award_coins_to_redeemer integer NOT NULL,
        redeemed_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_referral_redemptions PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE refresh_tokens (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        token character varying(128) NOT NULL,
        device_id character varying(256) NOT NULL,
        client_type character varying(32) NOT NULL,
        expires_at timestamp with time zone NOT NULL,
        created_at timestamp with time zone NOT NULL,
        is_revoked boolean NOT NULL,
        revoked_at timestamp with time zone,
        CONSTRAINT pk_refresh_tokens PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE season_point_transactions (
        id uuid NOT NULL,
        event_id uuid NOT NULL,
        season_id uuid NOT NULL,
        player_id uuid NOT NULL,
        kind character varying(48) NOT NULL,
        delta integer NOT NULL,
        note text,
        created_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_season_point_transactions PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE season_rank_snapshot_rows (
        id uuid NOT NULL,
        season_id uuid NOT NULL,
        player_id uuid NOT NULL,
        rank_points integer NOT NULL,
        tier integer NOT NULL,
        tier_rank integer NOT NULL,
        season_rank integer NOT NULL,
        wins integer NOT NULL,
        losses integer NOT NULL,
        draws integer NOT NULL,
        matches_played integer NOT NULL,
        captured_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_season_rank_snapshot_rows PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE season_reward_claims (
        id uuid NOT NULL,
        season_id uuid NOT NULL,
        player_id uuid NOT NULL,
        event_id uuid NOT NULL,
        reward_day date NOT NULL,
        awarded_coins integer NOT NULL,
        awarded_xp integer NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_season_reward_claims PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE seasons (
        id uuid NOT NULL,
        season_number integer NOT NULL,
        name character varying(80) NOT NULL,
        status integer NOT NULL,
        closed_at_utc timestamp with time zone NOT NULL,
        starts_at_utc timestamp with time zone NOT NULL,
        ends_at_utc timestamp with time zone NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_seasons PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE skill_nodes (
        id uuid NOT NULL,
        key character varying(80) NOT NULL,
        branch integer NOT NULL,
        tier integer NOT NULL,
        title character varying(120) NOT NULL,
        description character varying(600) NOT NULL,
        prereq_keys_json text NOT NULL,
        costs_json text NOT NULL,
        effects_json text NOT NULL,
        updated_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_skill_nodes PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE territory_duels (
        id uuid NOT NULL,
        game_event_id uuid,
        season_id uuid NOT NULL,
        tier_number integer NOT NULL,
        category character varying(64) NOT NULL,
        challenger_id uuid NOT NULL,
        defender_id uuid,
        match_id uuid NOT NULL,
        outcome integer,
        created_at_utc timestamp with time zone NOT NULL,
        resolved_at_utc timestamp with time zone,
        CONSTRAINT pk_territory_duels PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE territory_tiles (
        id uuid NOT NULL,
        season_id uuid NOT NULL,
        tier_number integer NOT NULL,
        category character varying(64) NOT NULL,
        owner_id uuid,
        captured_at_utc timestamp with time zone,
        xp_multiplier_bps integer NOT NULL DEFAULT 0,
        CONSTRAINT pk_territory_tiles PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE tier_guardians (
        id uuid NOT NULL,
        season_id uuid NOT NULL,
        tier_number integer NOT NULL,
        player_id uuid NOT NULL,
        assigned_at_utc timestamp with time zone NOT NULL,
        expires_at_utc timestamp with time zone NOT NULL,
        passive_coins integer NOT NULL,
        passive_xp integer NOT NULL,
        defences_won integer NOT NULL,
        defences_lost integer NOT NULL,
        CONSTRAINT pk_tier_guardians PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE tiers (
        id uuid NOT NULL,
        name text NOT NULL,
        "order" integer NOT NULL,
        min_score integer NOT NULL,
        max_score integer NOT NULL,
        CONSTRAINT pk_tiers PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE users (
        id uuid NOT NULL,
        email character varying(256) NOT NULL,
        handle character varying(50) NOT NULL,
        password_hash text NOT NULL,
        country character varying(2),
        tier character varying(10),
        mmr integer NOT NULL,
        created_at timestamp with time zone NOT NULL,
        last_login_at timestamp with time zone,
        is_active boolean NOT NULL,
        CONSTRAINT pk_users PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE votes (
        id uuid NOT NULL,
        player_id uuid NOT NULL,
        option character varying(8) NOT NULL,
        topic character varying(128) NOT NULL,
        timestamp_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_votes PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE match_participant_results (
        id uuid NOT NULL,
        match_result_id uuid NOT NULL,
        player_id uuid NOT NULL,
        score integer NOT NULL,
        correct integer NOT NULL,
        wrong integer NOT NULL,
        avg_answer_time_ms double precision NOT NULL,
        CONSTRAINT pk_match_participant_results PRIMARY KEY (id),
        CONSTRAINT fk_match_participant_results_match_results_match_result_id FOREIGN KEY (match_result_id) REFERENCES match_results (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE match_rounds (
        id uuid NOT NULL,
        match_id uuid NOT NULL,
        index integer NOT NULL,
        correct boolean NOT NULL,
        answer_time_ms integer NOT NULL,
        points integer NOT NULL,
        CONSTRAINT pk_match_rounds PRIMARY KEY (id),
        CONSTRAINT fk_match_rounds_matches_match_id FOREIGN KEY (match_id) REFERENCES matches (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE economy_transactions (
        id uuid NOT NULL,
        reversal_of_transaction_id uuid,
        player_transaction_id uuid,
        event_id uuid NOT NULL,
        player_id uuid NOT NULL,
        kind character varying(64) NOT NULL,
        note character varying(512),
        created_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_economy_transactions PRIMARY KEY (id),
        CONSTRAINT fk_economy_transactions_player_transactions_player_transaction FOREIGN KEY (player_transaction_id) REFERENCES player_transactions (id) ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE player_transaction_actors (
        id uuid NOT NULL,
        player_transaction_id uuid NOT NULL,
        player_id uuid NOT NULL,
        role integer NOT NULL,
        allocation_percent integer NOT NULL,
        CONSTRAINT pk_player_transaction_actors PRIMARY KEY (id),
        CONSTRAINT fk_player_transaction_actors_player_transactions_player_transa FOREIGN KEY (player_transaction_id) REFERENCES player_transactions (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE player_transaction_items (
        id uuid NOT NULL,
        player_transaction_id uuid NOT NULL,
        item_type character varying(128) NOT NULL,
        quantity integer NOT NULL,
        operation integer NOT NULL,
        CONSTRAINT pk_player_transaction_items PRIMARY KEY (id),
        CONSTRAINT fk_player_transaction_items_player_transactions_player_transac FOREIGN KEY (player_transaction_id) REFERENCES player_transactions (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE question_options (
        id uuid NOT NULL,
        question_id uuid NOT NULL,
        option_id character varying(64) NOT NULL,
        text character varying(1000) NOT NULL,
        CONSTRAINT pk_question_options PRIMARY KEY (id),
        CONSTRAINT fk_question_options_questions_question_id FOREIGN KEY (question_id) REFERENCES questions (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE question_tags (
        id uuid NOT NULL,
        question_id uuid NOT NULL,
        tag character varying(64) NOT NULL,
        CONSTRAINT pk_question_tags PRIMARY KEY (id),
        CONSTRAINT fk_question_tags_questions_question_id FOREIGN KEY (question_id) REFERENCES questions (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE TABLE economy_transaction_lines (
        id uuid NOT NULL,
        economy_transaction_id uuid NOT NULL,
        currency integer NOT NULL,
        delta integer NOT NULL,
        CONSTRAINT pk_economy_transaction_lines PRIMARY KEY (id),
        CONSTRAINT fk_economy_transaction_lines_economy_transactions_economy_tran FOREIGN KEY (economy_transaction_id) REFERENCES economy_transactions (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_admin_email_acls_list_type ON admin_email_acls (list_type);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_admin_email_acls_normalized_email ON admin_email_acls (normalized_email);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_admin_notification_history_channel_key ON admin_notification_history (channel_key);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_admin_notification_history_created_at ON admin_notification_history (created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_admin_notification_schedules_channel_key ON admin_notification_schedules (channel_key);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_admin_notification_schedules_scheduled_at ON admin_notification_schedules (scheduled_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_admin_notification_schedules_status ON admin_notification_schedules (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_admin_notification_templates_name ON admin_notification_templates (name);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_anti_cheat_flags_created_at_utc ON anti_cheat_flags (created_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_anti_cheat_flags_match_id ON anti_cheat_flags (match_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_anti_cheat_flags_player_id ON anti_cheat_flags (player_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_anti_cheat_flags_reviewed_at_utc ON anti_cheat_flags (reviewed_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_anti_cheat_flags_severity_created_at_utc ON anti_cheat_flags (severity, created_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_anti_cheat_flags_severity_reviewed_at_utc_created_at_utc ON anti_cheat_flags (severity, reviewed_at_utc, created_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_economy_transaction_lines_economy_transaction_id ON economy_transaction_lines (economy_transaction_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_economy_transactions_event_id ON economy_transactions (event_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_economy_transactions_player_id ON economy_transactions (player_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_economy_transactions_player_transaction_id ON economy_transactions (player_transaction_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_economy_transactions_reversal_of_transaction_id ON economy_transactions (reversal_of_transaction_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_friend_edges_player_id ON friend_edges (player_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_friend_edges_player_id_friend_player_id ON friend_edges (player_id, friend_player_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_friend_requests_from_player_id_to_player_id_status ON friend_requests (from_player_id, to_player_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_friend_requests_to_player_id_status_created_at_utc ON friend_requests (to_player_id, status, created_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_game_event_participants_entry_event_id ON game_event_participants (entry_event_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_game_event_participants_game_event_id_player_id ON game_event_participants (game_event_id, player_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_game_event_prize_claims_event_id ON game_event_prize_claims (event_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_game_event_prize_claims_game_event_id_player_id ON game_event_prize_claims (game_event_id, player_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_game_events_kind_status ON game_events (kind, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_game_events_scheduled_at_utc ON game_events (scheduled_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_game_events_status ON game_events (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_guardian_challenges_match_id ON guardian_challenges (match_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_guardian_challenges_season_id_tier_number_challenger_id ON guardian_challenges (season_id, tier_number, challenger_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_match_participant_results_match_result_id ON match_participant_results (match_result_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_match_participant_results_match_result_id_player_id ON match_participant_results (match_result_id, player_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_match_results_ended_at_utc ON match_results (ended_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_match_results_match_id ON match_results (match_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_match_results_submit_event_id ON match_results (submit_event_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_match_rounds_match_id ON match_rounds (match_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_matches_host_player_id ON matches (host_player_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_matches_started_at ON matches (started_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_matchmaking_tickets_mode_tier_scope_status_created_at_utc ON matchmaking_tickets (mode, tier, scope, status, created_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_matchmaking_tickets_player_id_status ON matchmaking_tickets (player_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_matchmaking_tickets_status_expires_at_utc ON matchmaking_tickets (status, expires_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_mission_claims_player_id_mission_id ON mission_claims (player_id, mission_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_missions_type_key ON missions (type, key);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_moderation_action_logs_created_at_utc ON moderation_action_logs (created_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_moderation_action_logs_new_status_created_at_utc ON moderation_action_logs (new_status, created_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_moderation_action_logs_player_id ON moderation_action_logs (player_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_moderation_action_logs_related_flag_id ON moderation_action_logs (related_flag_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_parties_leader_player_id_status ON parties (leader_player_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_parties_status_created_at_utc ON parties (status, created_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_party_invites_from_player_id_party_id_status ON party_invites (from_player_id, party_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_party_invites_party_id_status ON party_invites (party_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_party_invites_to_player_id_status_created_at_utc ON party_invites (to_player_id, status, created_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_party_match_links_match_id_status ON party_match_links (match_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_party_match_links_party_id_match_id ON party_match_links (party_id, match_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_party_match_members_match_id ON party_match_members (match_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_party_match_members_party_id ON party_match_members (party_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_party_matchmaking_tickets_party_id_status ON party_matchmaking_tickets (party_id, status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_party_matchmaking_tickets_status_mode_scope_tier_party_size ON party_matchmaking_tickets (status, mode, scope, tier, party_size, created_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_party_members_party_id_player_id ON party_members (party_id, player_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_party_members_player_id ON party_members (player_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_player_economy_safeguard_states_player_id ON player_economy_safeguard_states (player_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_player_event_stats_season_id_current_tiles_owned ON player_event_stats (season_id, current_tiles_owned);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_player_event_stats_season_id_events_won ON player_event_stats (season_id, events_won);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_player_event_stats_season_id_guardian_defences_won ON player_event_stats (season_id, guardian_defences_won);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_player_event_stats_season_id_player_id ON player_event_stats (season_id, player_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_player_moderation_profiles_player_id ON player_moderation_profiles (player_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_player_moderation_profiles_set_at_utc ON player_moderation_profiles (set_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_player_moderation_profiles_status ON player_moderation_profiles (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_player_powerups_player_id_type ON player_powerups (player_id, type);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_player_season_profiles_season_id_player_id ON player_season_profiles (season_id, player_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_player_season_profiles_season_id_rank_points ON player_season_profiles (season_id, rank_points);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_player_season_profiles_season_id_season_rank ON player_season_profiles (season_id, season_rank);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_player_season_profiles_season_id_tier_tier_rank ON player_season_profiles (season_id, tier, tier_rank);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_player_skill_unlocks_player_id ON player_skill_unlocks (player_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_player_skill_unlocks_player_id_node_key ON player_skill_unlocks (player_id, node_key);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_player_transaction_actors_player_id ON player_transaction_actors (player_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_player_transaction_actors_player_transaction_id ON player_transaction_actors (player_transaction_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_player_transaction_items_player_transaction_id ON player_transaction_items (player_transaction_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_player_transactions_correlated_event_id ON player_transactions (correlated_event_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_player_transactions_event_id ON player_transactions (event_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_player_wallets_player_id ON player_wallets (player_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_players_score ON players (score);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_players_tier_id ON players (tier_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_players_username ON players (username);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_processed_gameplay_events_event_id ON processed_gameplay_events (event_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_qr_scan_events_event_id ON qr_scan_events (event_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_qr_scan_events_player_id ON qr_scan_events (player_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_qr_scan_events_player_id_type_occurred_at_utc ON qr_scan_events (player_id, type, occurred_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_question_answered_analytics_events_player_id_question_id_an ON question_answered_analytics_events (player_id, question_id, answered_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_question_options_question_id_option_id ON question_options (question_id, option_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_question_tags_question_id_tag ON question_tags (question_id, tag);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_question_tags_tag ON question_tags (tag);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_questions_category ON questions (category);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_questions_difficulty ON questions (difficulty);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_questions_updated_at_utc ON questions (updated_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_referral_codes_code ON referral_codes (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_referral_codes_owner_player_id ON referral_codes (owner_player_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_referral_redemptions_event_id ON referral_redemptions (event_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_referral_redemptions_owner_player_id_redeemer_player_id ON referral_redemptions (owner_player_id, redeemer_player_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_referral_redemptions_referral_code_id ON referral_redemptions (referral_code_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_refresh_tokens_expires_at ON refresh_tokens (expires_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_refresh_tokens_token ON refresh_tokens (token);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_refresh_tokens_user_id_client_type_is_revoked ON refresh_tokens (user_id, client_type, is_revoked);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_refresh_tokens_user_id_device_id_is_revoked ON refresh_tokens (user_id, device_id, is_revoked);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_season_point_transactions_created_at_utc ON season_point_transactions (created_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_season_point_transactions_event_id ON season_point_transactions (event_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_season_point_transactions_season_id_player_id ON season_point_transactions (season_id, player_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_season_rank_snapshot_rows_season_id_player_id ON season_rank_snapshot_rows (season_id, player_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_season_rank_snapshot_rows_season_id_season_rank ON season_rank_snapshot_rows (season_id, season_rank);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_season_rank_snapshot_rows_season_id_tier_tier_rank ON season_rank_snapshot_rows (season_id, tier, tier_rank);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_season_reward_claims_event_id ON season_reward_claims (event_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_season_reward_claims_season_id_player_id_reward_day ON season_reward_claims (season_id, player_id, reward_day);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_seasons_ends_at_utc ON seasons (ends_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_seasons_season_number ON seasons (season_number);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_seasons_starts_at_utc ON seasons (starts_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_seasons_status ON seasons (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_skill_nodes_branch ON skill_nodes (branch);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_skill_nodes_key ON skill_nodes (key);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_skill_nodes_tier ON skill_nodes (tier);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_territory_duels_match_id ON territory_duels (match_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_territory_duels_season_id_tier_number_category ON territory_duels (season_id, tier_number, category);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_territory_tiles_owner_id ON territory_tiles (owner_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_territory_tiles_season_id_tier_number_category ON territory_tiles (season_id, tier_number, category);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_tier_guardians_season_id_tier_number ON tier_guardians (season_id, tier_number);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_tier_guardians_season_id_tier_number_player_id ON tier_guardians (season_id, tier_number, player_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_users_created_at ON users (created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_users_email ON users (email);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_users_handle ON users (handle);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_votes_player_id_topic ON votes (player_id, topic);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    CREATE INDEX ix_votes_topic ON votes (topic);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260325180201_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260325180201_InitialCreate', '10.0.7');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260402133706_AddAlphaRelease') THEN
    ALTER TABLE question_answered_player_daily_rollups ADD audience_segment text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260402133706_AddAlphaRelease') THEN
    ALTER TABLE question_answered_player_daily_rollups ADD brand_version text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260402133706_AddAlphaRelease') THEN
    ALTER TABLE question_answered_player_daily_rollups ADD entry_point text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260402133706_AddAlphaRelease') THEN
    ALTER TABLE question_answered_player_daily_rollups ADD surface text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260402133706_AddAlphaRelease') THEN
    ALTER TABLE question_answered_player_daily_rollups ADD synaptix_mode text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260402133706_AddAlphaRelease') THEN
    ALTER TABLE question_answered_daily_rollups ADD audience_segment text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260402133706_AddAlphaRelease') THEN
    ALTER TABLE question_answered_daily_rollups ADD brand_version text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260402133706_AddAlphaRelease') THEN
    ALTER TABLE question_answered_daily_rollups ADD entry_point text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260402133706_AddAlphaRelease') THEN
    ALTER TABLE question_answered_daily_rollups ADD surface text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260402133706_AddAlphaRelease') THEN
    ALTER TABLE question_answered_daily_rollups ADD synaptix_mode text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260402133706_AddAlphaRelease') THEN
    ALTER TABLE question_answered_analytics_events ADD audience_segment text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260402133706_AddAlphaRelease') THEN
    ALTER TABLE question_answered_analytics_events ADD brand_version text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260402133706_AddAlphaRelease') THEN
    ALTER TABLE question_answered_analytics_events ADD entry_point text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260402133706_AddAlphaRelease') THEN
    ALTER TABLE question_answered_analytics_events ADD surface text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260402133706_AddAlphaRelease') THEN
    ALTER TABLE question_answered_analytics_events ADD synaptix_mode text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260402133706_AddAlphaRelease') THEN
    CREATE TABLE player_preferences (
        id uuid NOT NULL,
        player_id uuid NOT NULL,
        synaptix_mode character varying(32) NOT NULL,
        preferred_surface character varying(32) NOT NULL,
        reduced_motion boolean NOT NULL,
        tone_preference character varying(32) NOT NULL,
        updated_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_player_preferences PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260402133706_AddAlphaRelease') THEN
    CREATE TABLE store_items (
        id uuid NOT NULL,
        sku character varying(128) NOT NULL,
        name character varying(256) NOT NULL,
        description character varying(1024) NOT NULL,
        item_type character varying(64) NOT NULL,
        price_coins integer NOT NULL,
        price_diamonds integer NOT NULL,
        grant_quantity integer NOT NULL,
        max_per_player integer NOT NULL,
        is_active boolean NOT NULL,
        sort_order integer NOT NULL,
        media_key character varying(512),
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_store_items PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260402133706_AddAlphaRelease') THEN
    CREATE UNIQUE INDEX ix_player_preferences_player_id ON player_preferences (player_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260402133706_AddAlphaRelease') THEN
    CREATE UNIQUE INDEX ix_store_items_sku ON store_items (sku);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260402133706_AddAlphaRelease') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260402133706_AddAlphaRelease', '10.0.7');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260416013946_AddLearningModules') THEN
    CREATE TABLE learning_modules (
        id uuid NOT NULL,
        title character varying(200) NOT NULL,
        description character varying(2000) NOT NULL,
        category character varying(64) NOT NULL,
        difficulty integer NOT NULL,
        reward_xp integer NOT NULL,
        reward_coins integer NOT NULL,
        is_published boolean NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_learning_modules PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260416013946_AddLearningModules') THEN
    CREATE TABLE module_completions (
        id uuid NOT NULL,
        player_id uuid NOT NULL,
        module_id uuid NOT NULL,
        economy_event_id uuid NOT NULL,
        completed_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_module_completions PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260416013946_AddLearningModules') THEN
    CREATE TABLE module_lessons (
        id uuid NOT NULL,
        module_id uuid NOT NULL,
        question_id uuid NOT NULL,
        "order" integer NOT NULL,
        explanation character varying(2000),
        CONSTRAINT pk_module_lessons PRIMARY KEY (id),
        CONSTRAINT fk_module_lessons_learning_modules_module_id FOREIGN KEY (module_id) REFERENCES learning_modules (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260416013946_AddLearningModules') THEN
    CREATE INDEX ix_learning_modules_category ON learning_modules (category);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260416013946_AddLearningModules') THEN
    CREATE INDEX ix_learning_modules_difficulty ON learning_modules (difficulty);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260416013946_AddLearningModules') THEN
    CREATE INDEX ix_learning_modules_is_published ON learning_modules (is_published);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260416013946_AddLearningModules') THEN
    CREATE UNIQUE INDEX ix_module_completions_economy_event_id ON module_completions (economy_event_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260416013946_AddLearningModules') THEN
    CREATE INDEX ix_module_completions_module_id ON module_completions (module_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260416013946_AddLearningModules') THEN
    CREATE INDEX ix_module_completions_player_id ON module_completions (player_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260416013946_AddLearningModules') THEN
    CREATE UNIQUE INDEX ix_module_completions_player_id_module_id ON module_completions (player_id, module_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260416013946_AddLearningModules') THEN
    CREATE INDEX ix_module_lessons_module_id ON module_lessons (module_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260416013946_AddLearningModules') THEN
    CREATE UNIQUE INDEX ix_module_lessons_module_id_order ON module_lessons (module_id, "order");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260416013946_AddLearningModules') THEN
    CREATE INDEX ix_module_lessons_question_id ON module_lessons (question_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260416013946_AddLearningModules') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260416013946_AddLearningModules', '10.0.7');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260418123000_AddUserAvatarUrl') THEN
    ALTER TABLE users ADD avatar_url text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260418123000_AddUserAvatarUrl') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260418123000_AddUserAvatarUrl', '10.0.7');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260418170000_AddStudySessions') THEN
    CREATE TABLE study_sessions (
        id uuid NOT NULL,
        player_id uuid NOT NULL,
        study_set_id character varying(256) NOT NULL,
        title character varying(200) NOT NULL,
        kind character varying(32) NOT NULL,
        category character varying(64) NOT NULL,
        question_count integer NOT NULL,
        question_ids_json text NOT NULL,
        answer_key_json text NOT NULL,
        answered_results_json text NOT NULL,
        answered_count integer NOT NULL,
        correct_count integer NOT NULL,
        current_question_index integer NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone NOT NULL,
        completed_at_utc timestamp with time zone,
        CONSTRAINT pk_study_sessions PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260418170000_AddStudySessions') THEN
    CREATE INDEX ix_study_sessions_player_id ON study_sessions (player_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260418170000_AddStudySessions') THEN
    CREATE INDEX ix_study_sessions_player_id_created_at_utc ON study_sessions (player_id, created_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260418170000_AddStudySessions') THEN
    CREATE INDEX ix_study_sessions_player_id_study_set_id_completed_at_utc ON study_sessions (player_id, study_set_id, completed_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260418170000_AddStudySessions') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260418170000_AddStudySessions', '10.0.7');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260418183000_AddQuestionStudyFavoritesAndSessionMode') THEN
    ALTER TABLE study_sessions ADD mode character varying(32) NOT NULL DEFAULT 'SelfTest';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260418183000_AddQuestionStudyFavoritesAndSessionMode') THEN
    CREATE TABLE question_study_favorites (
        id uuid NOT NULL,
        player_id uuid NOT NULL,
        question_id uuid NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_question_study_favorites PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260418183000_AddQuestionStudyFavoritesAndSessionMode') THEN
    CREATE INDEX ix_question_study_favorites_player_id ON question_study_favorites (player_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260418183000_AddQuestionStudyFavoritesAndSessionMode') THEN
    CREATE UNIQUE INDEX ix_question_study_favorites_player_id_question_id ON question_study_favorites (player_id, question_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260418183000_AddQuestionStudyFavoritesAndSessionMode') THEN
    CREATE INDEX ix_question_study_favorites_question_id ON question_study_favorites (question_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260418183000_AddQuestionStudyFavoritesAndSessionMode') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260418183000_AddQuestionStudyFavoritesAndSessionMode', '10.0.7');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260418213000_DeepenStudySurface') THEN
    ALTER TABLE study_sessions ADD interaction_states_json text NOT NULL DEFAULT '{}';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260418213000_DeepenStudySurface') THEN
    CREATE TABLE study_card_states (
        id uuid NOT NULL,
        player_id uuid NOT NULL,
        question_id uuid NOT NULL,
        review_count integer NOT NULL,
        success_streak integer NOT NULL,
        ease_factor numeric(4,2) NOT NULL,
        last_reviewed_at_utc timestamp with time zone,
        next_review_at_utc timestamp with time zone,
        last_outcome character varying(32),
        last_mode character varying(32),
        last_confidence integer,
        CONSTRAINT pk_study_card_states PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260418213000_DeepenStudySurface') THEN
    CREATE TABLE study_sets (
        id uuid NOT NULL,
        player_id uuid NOT NULL,
        title character varying(200) NOT NULL,
        description character varying(2000),
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_study_sets PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260418213000_DeepenStudySurface') THEN
    CREATE TABLE study_set_items (
        id uuid NOT NULL,
        study_set_id uuid NOT NULL,
        question_id uuid NOT NULL,
        "order" integer NOT NULL,
        CONSTRAINT pk_study_set_items PRIMARY KEY (id),
        CONSTRAINT fk_study_set_items_study_sets_study_set_id FOREIGN KEY (study_set_id) REFERENCES study_sets (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260418213000_DeepenStudySurface') THEN
    CREATE INDEX ix_study_card_states_player_id ON study_card_states (player_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260418213000_DeepenStudySurface') THEN
    CREATE INDEX ix_study_card_states_question_id ON study_card_states (question_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260418213000_DeepenStudySurface') THEN
    CREATE INDEX ix_study_card_states_player_id_next_review_at_utc ON study_card_states (player_id, next_review_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260418213000_DeepenStudySurface') THEN
    CREATE UNIQUE INDEX ix_study_card_states_player_id_question_id ON study_card_states (player_id, question_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260418213000_DeepenStudySurface') THEN
    CREATE INDEX ix_study_set_items_study_set_id ON study_set_items (study_set_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260418213000_DeepenStudySurface') THEN
    CREATE UNIQUE INDEX ix_study_set_items_study_set_id_order ON study_set_items (study_set_id, "order");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260418213000_DeepenStudySurface') THEN
    CREATE INDEX ix_study_sets_player_id ON study_sets (player_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260418213000_DeepenStudySurface') THEN
    CREATE INDEX ix_study_sets_player_id_updated_at_utc ON study_sets (player_id, updated_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260418213000_DeepenStudySurface') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260418213000_DeepenStudySurface', '10.0.7');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260418220357_AddProfileUpdates') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260418220357_AddProfileUpdates', '10.0.7');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260420231724_AddNotificationMessageing') THEN
    CREATE TABLE direct_message_conversations (
        id uuid NOT NULL,
        type character varying(16) NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_direct_message_conversations PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260420231724_AddNotificationMessageing') THEN
    CREATE TABLE player_notifications (
        id uuid NOT NULL,
        player_id uuid NOT NULL,
        type character varying(32) NOT NULL,
        title character varying(160) NOT NULL,
        body character varying(1000) NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        unread boolean NOT NULL,
        action_route character varying(256),
        payload_json text NOT NULL,
        icon character varying(64),
        avatar_url character varying(512),
        read_at_utc timestamp with time zone,
        CONSTRAINT pk_player_notifications PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260420231724_AddNotificationMessageing') THEN
    CREATE TABLE direct_message_conversation_participants (
        id uuid NOT NULL,
        conversation_id uuid NOT NULL,
        player_id uuid NOT NULL,
        joined_at_utc timestamp with time zone NOT NULL,
        last_read_at_utc timestamp with time zone,
        last_read_message_id uuid,
        CONSTRAINT pk_direct_message_conversation_participants PRIMARY KEY (id),
        CONSTRAINT fk_direct_message_conversation_participants_direct_message_con FOREIGN KEY (conversation_id) REFERENCES direct_message_conversations (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260420231724_AddNotificationMessageing') THEN
    CREATE TABLE direct_messages (
        id uuid NOT NULL,
        conversation_id uuid NOT NULL,
        sender_id uuid NOT NULL,
        content character varying(4000) NOT NULL,
        type character varying(16) NOT NULL,
        status character varying(16) NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        client_message_id character varying(128),
        CONSTRAINT pk_direct_messages PRIMARY KEY (id),
        CONSTRAINT fk_direct_messages_direct_message_conversations_conversation_id FOREIGN KEY (conversation_id) REFERENCES direct_message_conversations (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260420231724_AddNotificationMessageing') THEN
    CREATE UNIQUE INDEX ix_direct_message_conversation_participants_conversation_id_pl ON direct_message_conversation_participants (conversation_id, player_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260420231724_AddNotificationMessageing') THEN
    CREATE INDEX ix_direct_message_conversation_participants_player_id_last_rea ON direct_message_conversation_participants (player_id, last_read_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260420231724_AddNotificationMessageing') THEN
    CREATE INDEX ix_direct_message_conversations_type_updated_at_utc ON direct_message_conversations (type, updated_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260420231724_AddNotificationMessageing') THEN
    CREATE INDEX ix_direct_messages_conversation_id_created_at_utc ON direct_messages (conversation_id, created_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260420231724_AddNotificationMessageing') THEN
    CREATE UNIQUE INDEX ix_direct_messages_conversation_id_sender_id_client_message_id ON direct_messages (conversation_id, sender_id, client_message_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260420231724_AddNotificationMessageing') THEN
    CREATE INDEX ix_player_notifications_player_id_created_at_utc ON player_notifications (player_id, created_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260420231724_AddNotificationMessageing') THEN
    CREATE INDEX ix_player_notifications_player_id_unread_created_at_utc ON player_notifications (player_id, unread, created_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260420231724_AddNotificationMessageing') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260420231724_AddNotificationMessageing', '10.0.7');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260422132225_AddStoreItemAvatarFields') THEN
    ALTER TABLE store_items ADD is_featured boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260422132225_AddStoreItemAvatarFields') THEN
    ALTER TABLE store_items ADD thumbnail_url character varying(500);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260422132225_AddStoreItemAvatarFields') THEN
    ALTER TABLE store_items ADD version character varying(20);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260422132225_AddStoreItemAvatarFields') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260422132225_AddStoreItemAvatarFields', '10.0.7');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260425130000_AddStoreStockSystem') THEN
    CREATE TABLE store_stock_policies (
        id uuid NOT NULL,
        sku character varying(128) NOT NULL,
        max_quantity_per_user integer NOT NULL,
        reset_interval character varying(20) NOT NULL,
        is_active boolean NOT NULL DEFAULT TRUE,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_store_stock_policies PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260425130000_AddStoreStockSystem') THEN
    CREATE TABLE player_store_stock_states (
        id uuid NOT NULL,
        player_id uuid NOT NULL,
        sku character varying(128) NOT NULL,
        quantity_used integer NOT NULL,
        last_reset_at_utc timestamp with time zone,
        next_reset_at_utc timestamp with time zone,
        updated_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_player_store_stock_states PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260425130000_AddStoreStockSystem') THEN
    CREATE UNIQUE INDEX ix_store_stock_policies_sku ON store_stock_policies (sku);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260425130000_AddStoreStockSystem') THEN
    CREATE INDEX ix_player_store_stock_states_player_id ON player_store_stock_states (player_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260425130000_AddStoreStockSystem') THEN
    CREATE UNIQUE INDEX ix_player_store_stock_states_player_id_sku ON player_store_stock_states (player_id, sku);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260425130000_AddStoreStockSystem') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260425130000_AddStoreStockSystem', '10.0.7');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260425140000_AddFlashSale') THEN
    CREATE TABLE flash_sales (
        id uuid NOT NULL,
        sku character varying(128) NOT NULL,
        discount_percent integer NOT NULL,
        starts_at_utc timestamp with time zone NOT NULL,
        ends_at_utc timestamp with time zone NOT NULL,
        is_active boolean NOT NULL DEFAULT TRUE,
        reason character varying(256),
        created_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_flash_sales PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260425140000_AddFlashSale') THEN
    CREATE INDEX ix_flash_sales_ends_at_utc ON flash_sales (ends_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260425140000_AddFlashSale') THEN
    CREATE INDEX ix_flash_sales_sku_window ON flash_sales (sku, starts_at_utc, ends_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260425140000_AddFlashSale') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260425140000_AddFlashSale', '10.0.7');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260425150120_AddSeasonRewardRules') THEN
    CREATE TABLE season_reward_rules (
        id uuid NOT NULL,
        tier integer NOT NULL,
        max_tier_rank integer NOT NULL,
        reward_xp integer NOT NULL,
        reward_coins integer NOT NULL,
        CONSTRAINT pk_season_reward_rules PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260425150120_AddSeasonRewardRules') THEN
    CREATE UNIQUE INDEX ix_season_reward_rules_tier_max_tier_rank ON season_reward_rules (tier, max_tier_rank);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260425150120_AddSeasonRewardRules') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260425150120_AddSeasonRewardRules', '10.0.7');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260426100000_AddRewardClaimRule') THEN
    CREATE TABLE reward_claim_rules (
        id uuid NOT NULL,
        reward_id character varying(128) NOT NULL,
        max_claims_per_interval integer NOT NULL,
        reset_interval character varying(20) NOT NULL,
        is_active boolean NOT NULL DEFAULT TRUE,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_reward_claim_rules PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260426100000_AddRewardClaimRule') THEN
    CREATE UNIQUE INDEX ix_reward_claim_rules_reward_id ON reward_claim_rules (reward_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260426100000_AddRewardClaimRule') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260426100000_AddRewardClaimRule', '10.0.7');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260426110000_AddEffectiveMaxQuantity') THEN
    ALTER TABLE player_store_stock_states ADD effective_max_quantity integer;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260426110000_AddEffectiveMaxQuantity') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260426110000_AddEffectiveMaxQuantity', '10.0.7');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260429100000_AddPersonalizationTables') THEN
    CREATE TABLE player_mind_profiles (
        id uuid NOT NULL,
        player_id uuid NOT NULL,
        confidence_level numeric(5,2) NOT NULL DEFAULT 0.5,
        risk_tolerance numeric(5,2) NOT NULL DEFAULT 0.5,
        preferred_pace character varying(64) NOT NULL DEFAULT 'balanced',
        learning_style character varying(64) NOT NULL DEFAULT 'mixed',
        competitive_preference character varying(64) NOT NULL DEFAULT 'balanced',
        social_preference character varying(64) NOT NULL DEFAULT 'solo',
        churn_risk_score numeric(5,2) NOT NULL DEFAULT 0.0,
        frustration_risk_score numeric(5,2) NOT NULL DEFAULT 0.0,
        reward_sensitivity_score numeric(5,2) NOT NULL DEFAULT 0.5,
        store_affinity_score numeric(5,2) NOT NULL DEFAULT 0.5,
        notification_fatigue_score numeric(5,2) NOT NULL DEFAULT 0.0,
        archetype character varying(96) NOT NULL DEFAULT 'new_player',
        category_strengths_json jsonb NOT NULL DEFAULT '{}',
        category_weaknesses_json jsonb NOT NULL DEFAULT '{}',
        preference_json jsonb NOT NULL DEFAULT '{}',
        guardrail_json jsonb NOT NULL DEFAULT '{}',
        sidecar_scores_json jsonb NOT NULL DEFAULT '{}',
        personalization_enabled boolean NOT NULL DEFAULT TRUE,
        sidecar_scoring_enabled boolean NOT NULL DEFAULT TRUE,
        last_calculated_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_player_mind_profiles PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260429100000_AddPersonalizationTables') THEN
    CREATE UNIQUE INDEX ix_player_mind_profiles_player_id ON player_mind_profiles (player_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260429100000_AddPersonalizationTables') THEN
    CREATE INDEX ix_player_mind_profiles_archetype ON player_mind_profiles (archetype);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260429100000_AddPersonalizationTables') THEN
    CREATE INDEX ix_player_mind_profiles_churn_risk ON player_mind_profiles (churn_risk_score);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260429100000_AddPersonalizationTables') THEN
    CREATE INDEX ix_player_mind_profiles_frustration_risk ON player_mind_profiles (frustration_risk_score);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260429100000_AddPersonalizationTables') THEN
    CREATE INDEX ix_player_mind_profiles_updated_at ON player_mind_profiles (updated_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260429100000_AddPersonalizationTables') THEN
    CREATE TABLE player_behavior_events (
        id uuid NOT NULL,
        player_id uuid NOT NULL,
        event_type character varying(128) NOT NULL,
        event_source character varying(128) NOT NULL,
        category character varying(128),
        difficulty character varying(64),
        mode character varying(64),
        metadata_json jsonb NOT NULL DEFAULT '{}',
        occurred_at timestamp with time zone NOT NULL,
        ingested_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_player_behavior_events PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260429100000_AddPersonalizationTables') THEN
    CREATE INDEX ix_player_behavior_events_player_time ON player_behavior_events (player_id, occurred_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260429100000_AddPersonalizationTables') THEN
    CREATE INDEX ix_player_behavior_events_type ON player_behavior_events (event_type);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260429100000_AddPersonalizationTables') THEN
    CREATE INDEX ix_player_behavior_events_source ON player_behavior_events (event_source);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260429100000_AddPersonalizationTables') THEN
    CREATE INDEX ix_player_behavior_events_category ON player_behavior_events (category);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260429100000_AddPersonalizationTables') THEN
    CREATE TABLE personalization_recommendations (
        id uuid NOT NULL,
        player_id uuid NOT NULL,
        recommendation_type character varying(128) NOT NULL,
        source character varying(64) NOT NULL DEFAULT 'backend',
        priority integer NOT NULL DEFAULT 0,
        score numeric(5,2) NOT NULL DEFAULT 0.5,
        payload_json jsonb NOT NULL DEFAULT '{}',
        guardrail_json jsonb NOT NULL DEFAULT '{}',
        expires_at timestamp with time zone,
        accepted_at timestamp with time zone,
        dismissed_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_personalization_recommendations PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260429100000_AddPersonalizationTables') THEN
    CREATE INDEX ix_personalization_recommendations_player_created ON personalization_recommendations (player_id, created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260429100000_AddPersonalizationTables') THEN
    CREATE INDEX ix_personalization_recommendations_type ON personalization_recommendations (recommendation_type);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260429100000_AddPersonalizationTables') THEN
    CREATE TABLE personalization_rules (
        id uuid NOT NULL,
        rule_key character varying(256) NOT NULL,
        description character varying(512) NOT NULL DEFAULT '',
        is_enabled boolean NOT NULL DEFAULT TRUE,
        rule_json jsonb NOT NULL DEFAULT '{}',
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_personalization_rules PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260429100000_AddPersonalizationTables') THEN
    CREATE UNIQUE INDEX ix_personalization_rules_rule_key ON personalization_rules (rule_key);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260429100000_AddPersonalizationTables') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260429100000_AddPersonalizationTables', '10.0.7');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260429120000_AddExperimentTables') THEN
    CREATE TABLE experiments (
        id uuid NOT NULL,
        key character varying(128) NOT NULL,
        name character varying(256) NOT NULL,
        description character varying(1000),
        status character varying(32) NOT NULL DEFAULT 'draft',
        allocation_percent numeric(5,2) NOT NULL DEFAULT 100.0,
        starts_at timestamp with time zone,
        ends_at timestamp with time zone,
        metadata_json jsonb NOT NULL DEFAULT '{}',
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT "PK_experiments" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260429120000_AddExperimentTables') THEN
    CREATE TABLE experiment_variants (
        id uuid NOT NULL,
        experiment_id uuid NOT NULL,
        key character varying(128) NOT NULL,
        name character varying(256) NOT NULL,
        weight numeric(6,2) NOT NULL DEFAULT 50.0,
        is_control boolean NOT NULL DEFAULT FALSE,
        config_json jsonb NOT NULL DEFAULT '{}',
        CONSTRAINT "PK_experiment_variants" PRIMARY KEY (id),
        CONSTRAINT "FK_experiment_variants_experiments_experiment_id" FOREIGN KEY (experiment_id) REFERENCES experiments (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260429120000_AddExperimentTables') THEN
    CREATE TABLE experiment_assignments (
        id uuid NOT NULL,
        player_id uuid NOT NULL,
        experiment_id uuid NOT NULL,
        experiment_key character varying(128) NOT NULL,
        variant_key character varying(128) NOT NULL,
        assigned_at timestamp with time zone NOT NULL,
        first_seen_at timestamp with time zone,
        impression_count integer NOT NULL DEFAULT 0,
        outcome_count integer NOT NULL DEFAULT 0,
        outcome_json jsonb NOT NULL DEFAULT '{}',
        CONSTRAINT "PK_experiment_assignments" PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260429120000_AddExperimentTables') THEN
    CREATE UNIQUE INDEX "IX_experiments_key" ON experiments (key);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260429120000_AddExperimentTables') THEN
    CREATE INDEX "IX_experiments_status" ON experiments (status);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260429120000_AddExperimentTables') THEN
    CREATE UNIQUE INDEX "IX_experiment_variants_experiment_id_key" ON experiment_variants (experiment_id, key);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260429120000_AddExperimentTables') THEN
    CREATE UNIQUE INDEX "IX_experiment_assignments_player_id_experiment_id" ON experiment_assignments (player_id, experiment_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260429120000_AddExperimentTables') THEN
    CREATE INDEX "IX_experiment_assignments_experiment_id_variant_key" ON experiment_assignments (experiment_id, variant_key);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260429120000_AddExperimentTables') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260429120000_AddExperimentTables', '10.0.7');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260430120000_AddPersonalizationAuditLog') THEN
    CREATE TABLE personalization_audit_logs (
        id uuid NOT NULL,
        player_id uuid NOT NULL,
        recommendation_id uuid,
        decision_type character varying(128) NOT NULL,
        source character varying(64) NOT NULL DEFAULT 'backend',
        reason character varying(512) NOT NULL DEFAULT '',
        input_signals_json jsonb NOT NULL DEFAULT '{}',
        candidate_json jsonb NOT NULL DEFAULT '{}',
        guardrails_applied_json jsonb NOT NULL DEFAULT '{}',
        final_decision_json jsonb NOT NULL DEFAULT '{}',
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_personalization_audit_logs PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260430120000_AddPersonalizationAuditLog') THEN
    CREATE INDEX ix_personalization_audit_logs_player_created ON personalization_audit_logs (player_id, created_at);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260430120000_AddPersonalizationAuditLog') THEN
    CREATE INDEX ix_personalization_audit_logs_decision_type ON personalization_audit_logs (decision_type);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260430120000_AddPersonalizationAuditLog') THEN
    CREATE INDEX ix_personalization_audit_logs_recommendation_id ON personalization_audit_logs (recommendation_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260430120000_AddPersonalizationAuditLog') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260430120000_AddPersonalizationAuditLog', '10.0.7');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260501090000_AddReasonToPersonalizationRecommendation') THEN
    ALTER TABLE personalization_recommendations ADD reason character varying(512) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260501090000_AddReasonToPersonalizationRecommendation') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260501090000_AddReasonToPersonalizationRecommendation', '10.0.7');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260502100000_AddArcadeSpinClaims') THEN
    CREATE TABLE arcade_spin_claims (
        id uuid NOT NULL,
        player_id uuid NOT NULL,
        segment_id character varying(100) NOT NULL,
        spin_id character varying(100) NOT NULL,
        coins_granted integer NOT NULL,
        claimed_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_arcade_spin_claims PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260502100000_AddArcadeSpinClaims') THEN
    CREATE UNIQUE INDEX ix_arcade_spin_claims_spin_id ON arcade_spin_claims (spin_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260502100000_AddArcadeSpinClaims') THEN
    CREATE INDEX ix_arcade_spin_claims_player_id_claimed_at_utc ON arcade_spin_claims (player_id, claimed_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260502100000_AddArcadeSpinClaims') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260502100000_AddArcadeSpinClaims', '10.0.7');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260505021733_FixExperimentNamingAndRemoveMindProfileDefaults') THEN
    ALTER TABLE experiment_variants DROP CONSTRAINT "FK_experiment_variants_experiments_experiment_id";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260505021733_FixExperimentNamingAndRemoveMindProfileDefaults') THEN
    ALTER TABLE experiments DROP CONSTRAINT "PK_experiments";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260505021733_FixExperimentNamingAndRemoveMindProfileDefaults') THEN
    ALTER TABLE experiment_variants DROP CONSTRAINT "PK_experiment_variants";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260505021733_FixExperimentNamingAndRemoveMindProfileDefaults') THEN
    ALTER TABLE experiment_assignments DROP CONSTRAINT "PK_experiment_assignments";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260505021733_FixExperimentNamingAndRemoveMindProfileDefaults') THEN
    ALTER INDEX "IX_experiments_status" RENAME TO ix_experiments_status;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260505021733_FixExperimentNamingAndRemoveMindProfileDefaults') THEN
    ALTER INDEX "IX_experiments_key" RENAME TO ix_experiments_key;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260505021733_FixExperimentNamingAndRemoveMindProfileDefaults') THEN
    ALTER INDEX "IX_experiment_variants_experiment_id_key" RENAME TO ix_experiment_variants_experiment_id_key;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260505021733_FixExperimentNamingAndRemoveMindProfileDefaults') THEN
    ALTER INDEX "IX_experiment_assignments_player_id_experiment_id" RENAME TO ix_experiment_assignments_player_id_experiment_id;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260505021733_FixExperimentNamingAndRemoveMindProfileDefaults') THEN
    ALTER INDEX "IX_experiment_assignments_experiment_id_variant_key" RENAME TO ix_experiment_assignments_experiment_id_variant_key;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260505021733_FixExperimentNamingAndRemoveMindProfileDefaults') THEN
    ALTER TABLE player_mind_profiles ALTER COLUMN store_affinity_score DROP DEFAULT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260505021733_FixExperimentNamingAndRemoveMindProfileDefaults') THEN
    ALTER TABLE player_mind_profiles ALTER COLUMN social_preference DROP DEFAULT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260505021733_FixExperimentNamingAndRemoveMindProfileDefaults') THEN
    ALTER TABLE player_mind_profiles ALTER COLUMN sidecar_scoring_enabled DROP DEFAULT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260505021733_FixExperimentNamingAndRemoveMindProfileDefaults') THEN
    ALTER TABLE player_mind_profiles ALTER COLUMN sidecar_scores_json DROP DEFAULT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260505021733_FixExperimentNamingAndRemoveMindProfileDefaults') THEN
    ALTER TABLE player_mind_profiles ALTER COLUMN risk_tolerance DROP DEFAULT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260505021733_FixExperimentNamingAndRemoveMindProfileDefaults') THEN
    ALTER TABLE player_mind_profiles ALTER COLUMN reward_sensitivity_score DROP DEFAULT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260505021733_FixExperimentNamingAndRemoveMindProfileDefaults') THEN
    ALTER TABLE player_mind_profiles ALTER COLUMN preferred_pace DROP DEFAULT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260505021733_FixExperimentNamingAndRemoveMindProfileDefaults') THEN
    ALTER TABLE player_mind_profiles ALTER COLUMN preference_json DROP DEFAULT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260505021733_FixExperimentNamingAndRemoveMindProfileDefaults') THEN
    ALTER TABLE player_mind_profiles ALTER COLUMN personalization_enabled DROP DEFAULT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260505021733_FixExperimentNamingAndRemoveMindProfileDefaults') THEN
    ALTER TABLE player_mind_profiles ALTER COLUMN notification_fatigue_score DROP DEFAULT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260505021733_FixExperimentNamingAndRemoveMindProfileDefaults') THEN
    ALTER TABLE player_mind_profiles ALTER COLUMN learning_style DROP DEFAULT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260505021733_FixExperimentNamingAndRemoveMindProfileDefaults') THEN
    ALTER TABLE player_mind_profiles ALTER COLUMN guardrail_json DROP DEFAULT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260505021733_FixExperimentNamingAndRemoveMindProfileDefaults') THEN
    ALTER TABLE player_mind_profiles ALTER COLUMN frustration_risk_score DROP DEFAULT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260505021733_FixExperimentNamingAndRemoveMindProfileDefaults') THEN
    ALTER TABLE player_mind_profiles ALTER COLUMN confidence_level DROP DEFAULT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260505021733_FixExperimentNamingAndRemoveMindProfileDefaults') THEN
    ALTER TABLE player_mind_profiles ALTER COLUMN competitive_preference DROP DEFAULT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260505021733_FixExperimentNamingAndRemoveMindProfileDefaults') THEN
    ALTER TABLE player_mind_profiles ALTER COLUMN churn_risk_score DROP DEFAULT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260505021733_FixExperimentNamingAndRemoveMindProfileDefaults') THEN
    ALTER TABLE player_mind_profiles ALTER COLUMN category_weaknesses_json DROP DEFAULT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260505021733_FixExperimentNamingAndRemoveMindProfileDefaults') THEN
    ALTER TABLE player_mind_profiles ALTER COLUMN category_strengths_json DROP DEFAULT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260505021733_FixExperimentNamingAndRemoveMindProfileDefaults') THEN
    ALTER TABLE player_mind_profiles ALTER COLUMN archetype DROP DEFAULT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260505021733_FixExperimentNamingAndRemoveMindProfileDefaults') THEN
    ALTER TABLE player_behavior_events ALTER COLUMN metadata_json DROP DEFAULT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260505021733_FixExperimentNamingAndRemoveMindProfileDefaults') THEN
    ALTER TABLE personalization_rules ALTER COLUMN rule_json DROP DEFAULT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260505021733_FixExperimentNamingAndRemoveMindProfileDefaults') THEN
    ALTER TABLE personalization_rules ALTER COLUMN is_enabled DROP DEFAULT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260505021733_FixExperimentNamingAndRemoveMindProfileDefaults') THEN
    ALTER TABLE personalization_rules ALTER COLUMN description DROP DEFAULT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260505021733_FixExperimentNamingAndRemoveMindProfileDefaults') THEN
    ALTER TABLE personalization_recommendations ALTER COLUMN source DROP DEFAULT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260505021733_FixExperimentNamingAndRemoveMindProfileDefaults') THEN
    ALTER TABLE personalization_recommendations ALTER COLUMN score DROP DEFAULT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260505021733_FixExperimentNamingAndRemoveMindProfileDefaults') THEN
    ALTER TABLE personalization_recommendations ALTER COLUMN priority DROP DEFAULT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260505021733_FixExperimentNamingAndRemoveMindProfileDefaults') THEN
    ALTER TABLE personalization_recommendations ALTER COLUMN payload_json DROP DEFAULT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260505021733_FixExperimentNamingAndRemoveMindProfileDefaults') THEN
    ALTER TABLE personalization_recommendations ALTER COLUMN guardrail_json DROP DEFAULT;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260505021733_FixExperimentNamingAndRemoveMindProfileDefaults') THEN
    ALTER TABLE experiments ADD CONSTRAINT pk_experiments PRIMARY KEY (id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260505021733_FixExperimentNamingAndRemoveMindProfileDefaults') THEN
    ALTER TABLE experiment_variants ADD CONSTRAINT pk_experiment_variants PRIMARY KEY (id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260505021733_FixExperimentNamingAndRemoveMindProfileDefaults') THEN
    ALTER TABLE experiment_assignments ADD CONSTRAINT pk_experiment_assignments PRIMARY KEY (id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260505021733_FixExperimentNamingAndRemoveMindProfileDefaults') THEN
    ALTER TABLE experiment_variants ADD CONSTRAINT fk_experiment_variants_experiments_experiment_id FOREIGN KEY (experiment_id) REFERENCES experiments (id) ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260505021733_FixExperimentNamingAndRemoveMindProfileDefaults') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260505021733_FixExperimentNamingAndRemoveMindProfileDefaults', '10.0.7');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260509120000_AddDailyAndWeeklyRewards') THEN
    CREATE TABLE daily_reward_claims (
        id uuid NOT NULL,
        player_id uuid NOT NULL,
        claim_date date NOT NULL,
        coins_granted integer NOT NULL,
        claimed_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_daily_reward_claims PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260509120000_AddDailyAndWeeklyRewards') THEN
    CREATE UNIQUE INDEX ix_daily_reward_claims_player_id_claim_date ON daily_reward_claims (player_id, claim_date);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260509120000_AddDailyAndWeeklyRewards') THEN
    CREATE TABLE weekly_streak_states (
        id uuid NOT NULL,
        player_id uuid NOT NULL,
        cycle_start_date date NOT NULL,
        current_day integer NOT NULL,
        claimed_days_json character varying(50) NOT NULL,
        updated_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_weekly_streak_states PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260509120000_AddDailyAndWeeklyRewards') THEN
    CREATE UNIQUE INDEX ix_weekly_streak_states_player_id ON weekly_streak_states (player_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260509120000_AddDailyAndWeeklyRewards') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260509120000_AddDailyAndWeeklyRewards', '10.0.7');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260512150000_AddQuestionStatusColumns') THEN
    ALTER TABLE questions ADD status text NOT NULL DEFAULT 'Draft';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260512150000_AddQuestionStatusColumns') THEN
    ALTER TABLE questions ADD status_changed_at_utc timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260512150000_AddQuestionStatusColumns') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260512150000_AddQuestionStatusColumns', '10.0.7');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260515102821_AddMayCutoverSchemaSync') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260515102821_AddMayCutoverSchemaSync', '10.0.7');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260522120000_AddUserSystemRole') THEN
    ALTER TABLE users ADD system_role character varying(32);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260522120000_AddUserSystemRole') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260522120000_AddUserSystemRole', '10.0.7');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260612034523_AddUserIsAnonymous') THEN
    ALTER TABLE users ADD is_anonymous boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260612034523_AddUserIsAnonymous') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260612034523_AddUserIsAnonymous', '10.0.7');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260612193650_AddPlayerLookupAndRewardChainTables') THEN
    CREATE TABLE player_lookup_codes (
        id uuid NOT NULL,
        player_id uuid NOT NULL,
        user_id uuid,
        short_code character varying(6) NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        updated_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_player_lookup_codes PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260612193650_AddPlayerLookupAndRewardChainTables') THEN
    CREATE TABLE reward_chain_tickets (
        id uuid NOT NULL,
        chained_spin_id character varying(64) NOT NULL,
        player_id uuid NOT NULL,
        source_spin_id character varying(64) NOT NULL,
        reward_id character varying(100) NOT NULL,
        reward_lines_json text NOT NULL,
        animation_json text NOT NULL,
        status character varying(32) NOT NULL,
        expires_at_utc timestamp with time zone NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        activated_at_utc timestamp with time zone,
        generated_spin_id character varying(64),
        generated_claim_token character varying(200),
        CONSTRAINT pk_reward_chain_tickets PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260612193650_AddPlayerLookupAndRewardChainTables') THEN
    CREATE UNIQUE INDEX ix_player_lookup_codes_player_id ON player_lookup_codes (player_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260612193650_AddPlayerLookupAndRewardChainTables') THEN
    CREATE UNIQUE INDEX ix_player_lookup_codes_short_code ON player_lookup_codes (short_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260612193650_AddPlayerLookupAndRewardChainTables') THEN
    CREATE INDEX ix_player_lookup_codes_user_id ON player_lookup_codes (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260612193650_AddPlayerLookupAndRewardChainTables') THEN
    CREATE UNIQUE INDEX ix_reward_chain_tickets_chained_spin_id ON reward_chain_tickets (chained_spin_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260612193650_AddPlayerLookupAndRewardChainTables') THEN
    CREATE UNIQUE INDEX ix_reward_chain_tickets_player_id_source_spin_id ON reward_chain_tickets (player_id, source_spin_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260612193650_AddPlayerLookupAndRewardChainTables') THEN
    CREATE INDEX ix_reward_chain_tickets_player_id_status_expires_at_utc ON reward_chain_tickets (player_id, status, expires_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20260612193650_AddPlayerLookupAndRewardChainTables') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20260612193650_AddPlayerLookupAndRewardChainTables', '10.0.7');
    END IF;
END $EF$;
COMMIT;


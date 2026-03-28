using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tycoon.Backend.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "admin_app_config",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    api_base_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    enable_logging = table.Column<bool>(type: "boolean", nullable: false),
                    feature_flags_json = table.Column<string>(type: "jsonb", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admin_app_config", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "admin_email_acls",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    normalized_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    list_type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    added_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admin_email_acls", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "admin_notification_channels",
                columns: table => new
                {
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    importance = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    enabled = table.Column<bool>(type: "boolean", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admin_notification_channels", x => x.key);
                });

            migrationBuilder.CreateTable(
                name: "admin_notification_history",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    channel_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admin_notification_history", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "admin_notification_schedules",
                columns: table => new
                {
                    schedule_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    channel_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    scheduled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    retry_count = table.Column<int>(type: "integer", nullable: false),
                    max_retries = table.Column<int>(type: "integer", nullable: false),
                    last_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    processed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admin_notification_schedules", x => x.schedule_id);
                });

            migrationBuilder.CreateTable(
                name: "admin_notification_templates",
                columns: table => new
                {
                    template_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    channel_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    variables_json = table.Column<string>(type: "jsonb", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admin_notification_templates", x => x.template_id);
                });

            migrationBuilder.CreateTable(
                name: "anti_cheat_flags",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    match_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rule_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    severity = table.Column<int>(type: "integer", nullable: false),
                    action = table.Column<int>(type: "integer", nullable: false),
                    message = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    evidence_json = table.Column<string>(type: "text", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reviewed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reviewed_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    review_note = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_anti_cheat_flags", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "friend_edges",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    friend_player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_friend_edges", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "friend_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    responded_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_friend_requests", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "game_balance_configs",
                columns: table => new
                {
                    id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    config_json = table.Column<string>(type: "jsonb", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_game_balance_configs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "game_event_participants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entry_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    eliminated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    final_rank = table.Column<int>(type: "integer", nullable: true),
                    revives_used = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_game_event_participants", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "game_event_prize_claims",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    awarded_xp = table.Column<int>(type: "integer", nullable: false),
                    awarded_coins = table.Column<int>(type: "integer", nullable: false),
                    rank = table.Column<int>(type: "integer", nullable: false),
                    claimed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_game_event_prize_claims", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "game_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    tier_id = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    scheduled_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    open_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    entry_fee_coins = table.Column<int>(type: "integer", nullable: false),
                    revive_cost_gems = table.Column<int>(type: "integer", nullable: false),
                    jackpot_pool = table.Column<int>(type: "integer", nullable: false),
                    max_participants = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_game_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "guardian_challenges",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    season_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tier_number = table.Column<int>(type: "integer", nullable: false),
                    challenger_id = table.Column<Guid>(type: "uuid", nullable: false),
                    guardian_id = table.Column<Guid>(type: "uuid", nullable: false),
                    match_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    resolved_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_guardian_challenges", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "leaderboard_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    global_rank = table.Column<int>(type: "integer", nullable: false),
                    tier_rank = table.Column<int>(type: "integer", nullable: false),
                    tier_id = table.Column<int>(type: "integer", nullable: false),
                    score = table.Column<int>(type: "integer", nullable: false),
                    xp_progress = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_leaderboard_entries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "match_results",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    match_id = table.Column<Guid>(type: "uuid", nullable: false),
                    submit_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    question_count = table.Column<int>(type: "integer", nullable: false),
                    ended_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_match_results", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "matches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    host_player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_matches", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "matchmaking_tickets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    row_version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    tier = table.Column<int>(type: "integer", nullable: false),
                    scope = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_matchmaking_tickets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mission_claims",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    progress = table.Column<int>(type: "integer", nullable: false),
                    completed = table.Column<bool>(type: "boolean", nullable: false),
                    completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    claimed = table.Column<bool>(type: "boolean", nullable: false),
                    claimed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_reset_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mission_claims", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "missions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    key = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    goal = table.Column<int>(type: "integer", nullable: false),
                    reward_xp = table.Column<int>(type: "integer", nullable: false),
                    reward_coins = table.Column<int>(type: "integer", nullable: false),
                    reward_diamonds = table.Column<int>(type: "integer", nullable: false),
                    active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_missions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "moderation_action_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    new_status = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    set_by_admin = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    related_flag_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_moderation_action_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "parties",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    leader_player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_parties", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "party_invites",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    responded_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_party_invites", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "party_match_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    match_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    closed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_party_match_links", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "party_match_members",
                columns: table => new
                {
                    party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    match_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    captured_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_party_match_members", x => new { x.party_id, x.match_id, x.player_id });
                });

            migrationBuilder.CreateTable(
                name: "party_matchmaking_tickets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    leader_player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mode = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    tier = table.Column<int>(type: "integer", nullable: false),
                    scope = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    party_size = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    row_version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_party_matchmaking_tickets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "party_members",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    joined_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_party_members", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "player_economy_safeguard_states",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sessions_started = table.Column<int>(type: "integer", nullable: false),
                    loss_streak = table.Column<int>(type: "integer", nullable: false),
                    current_energy = table.Column<int>(type: "integer", nullable: false),
                    last_energy_regen_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_free_ticket_claim_date = table.Column<DateOnly>(type: "date", nullable: true),
                    free_tickets_claimed_today = table.Column<int>(type: "integer", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_player_economy_safeguard_states", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "player_event_stats",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    season_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    events_entered = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    events_top20 = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    events_won = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    total_event_xp_earned = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    total_event_coins_earned = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    champion_battle_eliminations = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    guardian_promotions = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    guardian_defences_won = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    guardian_defences_lost = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    guardian_days_total = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    tiles_ever_captured = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    current_tiles_owned = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    peak_xp_multiplier_bps = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_player_event_stats", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "player_moderation_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    set_by_admin = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    set_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_player_moderation_profiles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "player_powerups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    cooldown_until_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_player_powerups", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "player_season_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    season_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rank_points = table.Column<int>(type: "integer", nullable: false),
                    wins = table.Column<int>(type: "integer", nullable: false),
                    losses = table.Column<int>(type: "integer", nullable: false),
                    draws = table.Column<int>(type: "integer", nullable: false),
                    matches_played = table.Column<int>(type: "integer", nullable: false),
                    tier = table.Column<int>(type: "integer", nullable: false),
                    tier_rank = table.Column<int>(type: "integer", nullable: false),
                    season_rank = table.Column<int>(type: "integer", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    placement_matches_completed = table.Column<int>(type: "integer", nullable: false),
                    last_promotion_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_demotion_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_player_season_profiles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "player_skill_unlocks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    node_key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    unlocked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_player_skill_unlocks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "player_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    correlated_event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    receipt = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    dispute_reason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    dispute_linked_to_transaction_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_player_transactions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "player_wallets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    xp = table.Column<int>(type: "integer", nullable: false),
                    coins = table.Column<int>(type: "integer", nullable: false),
                    diamonds = table.Column<int>(type: "integer", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_player_wallets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "players",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    username = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    country_code = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    score = table.Column<int>(type: "integer", nullable: false),
                    tier_id = table.Column<Guid>(type: "uuid", nullable: true),
                    level = table.Column<int>(type: "integer", nullable: false),
                    xp = table.Column<double>(type: "double precision", precision: 12, scale: 2, nullable: false),
                    coins = table.Column<int>(type: "integer", nullable: false),
                    diamonds = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_players", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "processed_gameplay_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_processed_gameplay_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "qr_scan_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    value = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    occurred_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    stored_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_qr_scan_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "question_answered_analytics_events",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    match_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<string>(type: "text", nullable: false),
                    mode = table.Column<string>(type: "text", nullable: false),
                    category = table.Column<string>(type: "text", nullable: false),
                    difficulty = table.Column<int>(type: "integer", nullable: false),
                    is_correct = table.Column<bool>(type: "boolean", nullable: false),
                    points_awarded = table.Column<int>(type: "integer", nullable: false),
                    answer_time_ms = table.Column<int>(type: "integer", nullable: false),
                    answered_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_question_answered_analytics_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "question_answered_daily_rollups",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    day = table.Column<DateOnly>(type: "date", nullable: false),
                    mode = table.Column<string>(type: "text", nullable: false),
                    category = table.Column<string>(type: "text", nullable: false),
                    difficulty = table.Column<int>(type: "integer", nullable: false),
                    total_answers = table.Column<int>(type: "integer", nullable: false),
                    correct_answers = table.Column<int>(type: "integer", nullable: false),
                    wrong_answers = table.Column<int>(type: "integer", nullable: false),
                    sum_answer_time_ms = table.Column<long>(type: "bigint", nullable: false),
                    min_answer_time_ms = table.Column<int>(type: "integer", nullable: false),
                    max_answer_time_ms = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_question_answered_daily_rollups", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "question_answered_player_daily_rollups",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    day = table.Column<DateOnly>(type: "date", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mode = table.Column<string>(type: "text", nullable: false),
                    category = table.Column<string>(type: "text", nullable: false),
                    difficulty = table.Column<int>(type: "integer", nullable: false),
                    total_answers = table.Column<int>(type: "integer", nullable: false),
                    correct_answers = table.Column<int>(type: "integer", nullable: false),
                    wrong_answers = table.Column<int>(type: "integer", nullable: false),
                    sum_answer_time_ms = table.Column<long>(type: "bigint", nullable: false),
                    min_answer_time_ms = table.Column<int>(type: "integer", nullable: false),
                    max_answer_time_ms = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_question_answered_player_daily_rollups", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "questions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    difficulty = table.Column<int>(type: "integer", nullable: false),
                    correct_option_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    media_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_questions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "referral_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    owner_player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_referral_codes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "referral_redemptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    referral_code_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    redeemer_player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    award_xp_to_owner = table.Column<int>(type: "integer", nullable: false),
                    award_coins_to_owner = table.Column<int>(type: "integer", nullable: false),
                    award_xp_to_redeemer = table.Column<int>(type: "integer", nullable: false),
                    award_coins_to_redeemer = table.Column<int>(type: "integer", nullable: false),
                    redeemed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_referral_redemptions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    device_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    client_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_revoked = table.Column<bool>(type: "boolean", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_tokens", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "season_point_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    season_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    delta = table.Column<int>(type: "integer", nullable: false),
                    note = table.Column<string>(type: "text", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_season_point_transactions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "season_rank_snapshot_rows",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    season_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rank_points = table.Column<int>(type: "integer", nullable: false),
                    tier = table.Column<int>(type: "integer", nullable: false),
                    tier_rank = table.Column<int>(type: "integer", nullable: false),
                    season_rank = table.Column<int>(type: "integer", nullable: false),
                    wins = table.Column<int>(type: "integer", nullable: false),
                    losses = table.Column<int>(type: "integer", nullable: false),
                    draws = table.Column<int>(type: "integer", nullable: false),
                    matches_played = table.Column<int>(type: "integer", nullable: false),
                    captured_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_season_rank_snapshot_rows", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "season_reward_claims",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    season_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reward_day = table.Column<DateOnly>(type: "date", nullable: false),
                    awarded_coins = table.Column<int>(type: "integer", nullable: false),
                    awarded_xp = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_season_reward_claims", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "seasons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    season_number = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    closed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    starts_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ends_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_seasons", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "skill_nodes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    branch = table.Column<int>(type: "integer", nullable: false),
                    tier = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    description = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    prereq_keys_json = table.Column<string>(type: "text", nullable: false),
                    costs_json = table.Column<string>(type: "text", nullable: false),
                    effects_json = table.Column<string>(type: "text", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_skill_nodes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "territory_duels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    season_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tier_number = table.Column<int>(type: "integer", nullable: false),
                    category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    challenger_id = table.Column<Guid>(type: "uuid", nullable: false),
                    defender_id = table.Column<Guid>(type: "uuid", nullable: true),
                    match_id = table.Column<Guid>(type: "uuid", nullable: false),
                    outcome = table.Column<int>(type: "integer", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    resolved_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_territory_duels", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "territory_tiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    season_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tier_number = table.Column<int>(type: "integer", nullable: false),
                    category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: true),
                    captured_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xp_multiplier_bps = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_territory_tiles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tier_guardians",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    season_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tier_number = table.Column<int>(type: "integer", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    assigned_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    passive_coins = table.Column<int>(type: "integer", nullable: false),
                    passive_xp = table.Column<int>(type: "integer", nullable: false),
                    defences_won = table.Column<int>(type: "integer", nullable: false),
                    defences_lost = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tier_guardians", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tiers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false),
                    min_score = table.Column<int>(type: "integer", nullable: false),
                    max_score = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tiers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    handle = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    country = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    tier = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    mmr = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "votes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    option = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    topic = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    timestamp_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_votes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "match_participant_results",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    match_result_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    score = table.Column<int>(type: "integer", nullable: false),
                    correct = table.Column<int>(type: "integer", nullable: false),
                    wrong = table.Column<int>(type: "integer", nullable: false),
                    avg_answer_time_ms = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_match_participant_results", x => x.id);
                    table.ForeignKey(
                        name: "fk_match_participant_results_match_results_match_result_id",
                        column: x => x.match_result_id,
                        principalTable: "match_results",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "match_rounds",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    match_id = table.Column<Guid>(type: "uuid", nullable: false),
                    index = table.Column<int>(type: "integer", nullable: false),
                    correct = table.Column<bool>(type: "boolean", nullable: false),
                    answer_time_ms = table.Column<int>(type: "integer", nullable: false),
                    points = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_match_rounds", x => x.id);
                    table.ForeignKey(
                        name: "fk_match_rounds_matches_match_id",
                        column: x => x.match_id,
                        principalTable: "matches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "economy_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reversal_of_transaction_id = table.Column<Guid>(type: "uuid", nullable: true),
                    player_transaction_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    note = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_economy_transactions", x => x.id);
                    table.ForeignKey(
                        name: "fk_economy_transactions_player_transactions_player_transaction",
                        column: x => x.player_transaction_id,
                        principalTable: "player_transactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "player_transaction_actors",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_transaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<int>(type: "integer", nullable: false),
                    allocation_percent = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_player_transaction_actors", x => x.id);
                    table.ForeignKey(
                        name: "fk_player_transaction_actors_player_transactions_player_transa",
                        column: x => x.player_transaction_id,
                        principalTable: "player_transactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "player_transaction_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_transaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    operation = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_player_transaction_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_player_transaction_items_player_transactions_player_transac",
                        column: x => x.player_transaction_id,
                        principalTable: "player_transactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "question_options",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    option_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_question_options", x => x.id);
                    table.ForeignKey(
                        name: "fk_question_options_questions_question_id",
                        column: x => x.question_id,
                        principalTable: "questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "question_tags",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_question_tags", x => x.id);
                    table.ForeignKey(
                        name: "fk_question_tags_questions_question_id",
                        column: x => x.question_id,
                        principalTable: "questions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "economy_transaction_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    economy_transaction_id = table.Column<Guid>(type: "uuid", nullable: false),
                    currency = table.Column<int>(type: "integer", nullable: false),
                    delta = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_economy_transaction_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_economy_transaction_lines_economy_transactions_economy_tran",
                        column: x => x.economy_transaction_id,
                        principalTable: "economy_transactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_admin_email_acls_list_type",
                table: "admin_email_acls",
                column: "list_type");

            migrationBuilder.CreateIndex(
                name: "ix_admin_email_acls_normalized_email",
                table: "admin_email_acls",
                column: "normalized_email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_admin_notification_history_channel_key",
                table: "admin_notification_history",
                column: "channel_key");

            migrationBuilder.CreateIndex(
                name: "ix_admin_notification_history_created_at",
                table: "admin_notification_history",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_admin_notification_schedules_channel_key",
                table: "admin_notification_schedules",
                column: "channel_key");

            migrationBuilder.CreateIndex(
                name: "ix_admin_notification_schedules_scheduled_at",
                table: "admin_notification_schedules",
                column: "scheduled_at");

            migrationBuilder.CreateIndex(
                name: "ix_admin_notification_schedules_status",
                table: "admin_notification_schedules",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_admin_notification_templates_name",
                table: "admin_notification_templates",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_anti_cheat_flags_created_at_utc",
                table: "anti_cheat_flags",
                column: "created_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_anti_cheat_flags_match_id",
                table: "anti_cheat_flags",
                column: "match_id");

            migrationBuilder.CreateIndex(
                name: "ix_anti_cheat_flags_player_id",
                table: "anti_cheat_flags",
                column: "player_id");

            migrationBuilder.CreateIndex(
                name: "ix_anti_cheat_flags_reviewed_at_utc",
                table: "anti_cheat_flags",
                column: "reviewed_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_anti_cheat_flags_severity_created_at_utc",
                table: "anti_cheat_flags",
                columns: new[] { "severity", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_anti_cheat_flags_severity_reviewed_at_utc_created_at_utc",
                table: "anti_cheat_flags",
                columns: new[] { "severity", "reviewed_at_utc", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_economy_transaction_lines_economy_transaction_id",
                table: "economy_transaction_lines",
                column: "economy_transaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_economy_transactions_event_id",
                table: "economy_transactions",
                column: "event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_economy_transactions_player_id",
                table: "economy_transactions",
                column: "player_id");

            migrationBuilder.CreateIndex(
                name: "ix_economy_transactions_player_transaction_id",
                table: "economy_transactions",
                column: "player_transaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_economy_transactions_reversal_of_transaction_id",
                table: "economy_transactions",
                column: "reversal_of_transaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_friend_edges_player_id",
                table: "friend_edges",
                column: "player_id");

            migrationBuilder.CreateIndex(
                name: "ix_friend_edges_player_id_friend_player_id",
                table: "friend_edges",
                columns: new[] { "player_id", "friend_player_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_friend_requests_from_player_id_to_player_id_status",
                table: "friend_requests",
                columns: new[] { "from_player_id", "to_player_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_friend_requests_to_player_id_status_created_at_utc",
                table: "friend_requests",
                columns: new[] { "to_player_id", "status", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_game_event_participants_entry_event_id",
                table: "game_event_participants",
                column: "entry_event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_game_event_participants_game_event_id_player_id",
                table: "game_event_participants",
                columns: new[] { "game_event_id", "player_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_game_event_prize_claims_event_id",
                table: "game_event_prize_claims",
                column: "event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_game_event_prize_claims_game_event_id_player_id",
                table: "game_event_prize_claims",
                columns: new[] { "game_event_id", "player_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_game_events_kind_status",
                table: "game_events",
                columns: new[] { "kind", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_game_events_scheduled_at_utc",
                table: "game_events",
                column: "scheduled_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_game_events_status",
                table: "game_events",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_guardian_challenges_match_id",
                table: "guardian_challenges",
                column: "match_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_guardian_challenges_season_id_tier_number_challenger_id",
                table: "guardian_challenges",
                columns: new[] { "season_id", "tier_number", "challenger_id" });

            migrationBuilder.CreateIndex(
                name: "ix_match_participant_results_match_result_id",
                table: "match_participant_results",
                column: "match_result_id");

            migrationBuilder.CreateIndex(
                name: "ix_match_participant_results_match_result_id_player_id",
                table: "match_participant_results",
                columns: new[] { "match_result_id", "player_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_match_results_ended_at_utc",
                table: "match_results",
                column: "ended_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_match_results_match_id",
                table: "match_results",
                column: "match_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_match_results_submit_event_id",
                table: "match_results",
                column: "submit_event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_match_rounds_match_id",
                table: "match_rounds",
                column: "match_id");

            migrationBuilder.CreateIndex(
                name: "ix_matches_host_player_id",
                table: "matches",
                column: "host_player_id");

            migrationBuilder.CreateIndex(
                name: "ix_matches_started_at",
                table: "matches",
                column: "started_at");

            migrationBuilder.CreateIndex(
                name: "ix_matchmaking_tickets_mode_tier_scope_status_created_at_utc",
                table: "matchmaking_tickets",
                columns: new[] { "mode", "tier", "scope", "status", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_matchmaking_tickets_player_id_status",
                table: "matchmaking_tickets",
                columns: new[] { "player_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_matchmaking_tickets_status_expires_at_utc",
                table: "matchmaking_tickets",
                columns: new[] { "status", "expires_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_mission_claims_player_id_mission_id",
                table: "mission_claims",
                columns: new[] { "player_id", "mission_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_missions_type_key",
                table: "missions",
                columns: new[] { "type", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_moderation_action_logs_created_at_utc",
                table: "moderation_action_logs",
                column: "created_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_moderation_action_logs_new_status_created_at_utc",
                table: "moderation_action_logs",
                columns: new[] { "new_status", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_moderation_action_logs_player_id",
                table: "moderation_action_logs",
                column: "player_id");

            migrationBuilder.CreateIndex(
                name: "ix_moderation_action_logs_related_flag_id",
                table: "moderation_action_logs",
                column: "related_flag_id");

            migrationBuilder.CreateIndex(
                name: "ix_parties_leader_player_id_status",
                table: "parties",
                columns: new[] { "leader_player_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_parties_status_created_at_utc",
                table: "parties",
                columns: new[] { "status", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_party_invites_from_player_id_party_id_status",
                table: "party_invites",
                columns: new[] { "from_player_id", "party_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_party_invites_party_id_status",
                table: "party_invites",
                columns: new[] { "party_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_party_invites_to_player_id_status_created_at_utc",
                table: "party_invites",
                columns: new[] { "to_player_id", "status", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_party_match_links_match_id_status",
                table: "party_match_links",
                columns: new[] { "match_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_party_match_links_party_id_match_id",
                table: "party_match_links",
                columns: new[] { "party_id", "match_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_party_match_members_match_id",
                table: "party_match_members",
                column: "match_id");

            migrationBuilder.CreateIndex(
                name: "ix_party_match_members_party_id",
                table: "party_match_members",
                column: "party_id");

            migrationBuilder.CreateIndex(
                name: "ix_party_matchmaking_tickets_party_id_status",
                table: "party_matchmaking_tickets",
                columns: new[] { "party_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_party_matchmaking_tickets_status_mode_scope_tier_party_size",
                table: "party_matchmaking_tickets",
                columns: new[] { "status", "mode", "scope", "tier", "party_size", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_party_members_party_id_player_id",
                table: "party_members",
                columns: new[] { "party_id", "player_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_party_members_player_id",
                table: "party_members",
                column: "player_id");

            migrationBuilder.CreateIndex(
                name: "ix_player_economy_safeguard_states_player_id",
                table: "player_economy_safeguard_states",
                column: "player_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_player_event_stats_season_id_current_tiles_owned",
                table: "player_event_stats",
                columns: new[] { "season_id", "current_tiles_owned" });

            migrationBuilder.CreateIndex(
                name: "ix_player_event_stats_season_id_events_won",
                table: "player_event_stats",
                columns: new[] { "season_id", "events_won" });

            migrationBuilder.CreateIndex(
                name: "ix_player_event_stats_season_id_guardian_defences_won",
                table: "player_event_stats",
                columns: new[] { "season_id", "guardian_defences_won" });

            migrationBuilder.CreateIndex(
                name: "ix_player_event_stats_season_id_player_id",
                table: "player_event_stats",
                columns: new[] { "season_id", "player_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_player_moderation_profiles_player_id",
                table: "player_moderation_profiles",
                column: "player_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_player_moderation_profiles_set_at_utc",
                table: "player_moderation_profiles",
                column: "set_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_player_moderation_profiles_status",
                table: "player_moderation_profiles",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_player_powerups_player_id_type",
                table: "player_powerups",
                columns: new[] { "player_id", "type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_player_season_profiles_season_id_player_id",
                table: "player_season_profiles",
                columns: new[] { "season_id", "player_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_player_season_profiles_season_id_rank_points",
                table: "player_season_profiles",
                columns: new[] { "season_id", "rank_points" });

            migrationBuilder.CreateIndex(
                name: "ix_player_season_profiles_season_id_season_rank",
                table: "player_season_profiles",
                columns: new[] { "season_id", "season_rank" });

            migrationBuilder.CreateIndex(
                name: "ix_player_season_profiles_season_id_tier_tier_rank",
                table: "player_season_profiles",
                columns: new[] { "season_id", "tier", "tier_rank" });

            migrationBuilder.CreateIndex(
                name: "ix_player_skill_unlocks_player_id",
                table: "player_skill_unlocks",
                column: "player_id");

            migrationBuilder.CreateIndex(
                name: "ix_player_skill_unlocks_player_id_node_key",
                table: "player_skill_unlocks",
                columns: new[] { "player_id", "node_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_player_transaction_actors_player_id",
                table: "player_transaction_actors",
                column: "player_id");

            migrationBuilder.CreateIndex(
                name: "ix_player_transaction_actors_player_transaction_id",
                table: "player_transaction_actors",
                column: "player_transaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_player_transaction_items_player_transaction_id",
                table: "player_transaction_items",
                column: "player_transaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_player_transactions_correlated_event_id",
                table: "player_transactions",
                column: "correlated_event_id");

            migrationBuilder.CreateIndex(
                name: "ix_player_transactions_event_id",
                table: "player_transactions",
                column: "event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_player_wallets_player_id",
                table: "player_wallets",
                column: "player_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_players_score",
                table: "players",
                column: "score");

            migrationBuilder.CreateIndex(
                name: "ix_players_tier_id",
                table: "players",
                column: "tier_id");

            migrationBuilder.CreateIndex(
                name: "ix_players_username",
                table: "players",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_processed_gameplay_events_event_id",
                table: "processed_gameplay_events",
                column: "event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_qr_scan_events_event_id",
                table: "qr_scan_events",
                column: "event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_qr_scan_events_player_id",
                table: "qr_scan_events",
                column: "player_id");

            migrationBuilder.CreateIndex(
                name: "ix_qr_scan_events_player_id_type_occurred_at_utc",
                table: "qr_scan_events",
                columns: new[] { "player_id", "type", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_question_answered_analytics_events_player_id_question_id_an",
                table: "question_answered_analytics_events",
                columns: new[] { "player_id", "question_id", "answered_at_utc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_question_options_question_id_option_id",
                table: "question_options",
                columns: new[] { "question_id", "option_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_question_tags_question_id_tag",
                table: "question_tags",
                columns: new[] { "question_id", "tag" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_question_tags_tag",
                table: "question_tags",
                column: "tag");

            migrationBuilder.CreateIndex(
                name: "ix_questions_category",
                table: "questions",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "ix_questions_difficulty",
                table: "questions",
                column: "difficulty");

            migrationBuilder.CreateIndex(
                name: "ix_questions_updated_at_utc",
                table: "questions",
                column: "updated_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_referral_codes_code",
                table: "referral_codes",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_referral_codes_owner_player_id",
                table: "referral_codes",
                column: "owner_player_id");

            migrationBuilder.CreateIndex(
                name: "ix_referral_redemptions_event_id",
                table: "referral_redemptions",
                column: "event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_referral_redemptions_owner_player_id_redeemer_player_id",
                table: "referral_redemptions",
                columns: new[] { "owner_player_id", "redeemer_player_id" });

            migrationBuilder.CreateIndex(
                name: "ix_referral_redemptions_referral_code_id",
                table: "referral_redemptions",
                column: "referral_code_id");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_expires_at",
                table: "refresh_tokens",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_token",
                table: "refresh_tokens",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_user_id_client_type_is_revoked",
                table: "refresh_tokens",
                columns: new[] { "user_id", "client_type", "is_revoked" });

            migrationBuilder.CreateIndex(
                name: "ix_refresh_tokens_user_id_device_id_is_revoked",
                table: "refresh_tokens",
                columns: new[] { "user_id", "device_id", "is_revoked" });

            migrationBuilder.CreateIndex(
                name: "ix_season_point_transactions_created_at_utc",
                table: "season_point_transactions",
                column: "created_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_season_point_transactions_event_id",
                table: "season_point_transactions",
                column: "event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_season_point_transactions_season_id_player_id",
                table: "season_point_transactions",
                columns: new[] { "season_id", "player_id" });

            migrationBuilder.CreateIndex(
                name: "ix_season_rank_snapshot_rows_season_id_player_id",
                table: "season_rank_snapshot_rows",
                columns: new[] { "season_id", "player_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_season_rank_snapshot_rows_season_id_season_rank",
                table: "season_rank_snapshot_rows",
                columns: new[] { "season_id", "season_rank" });

            migrationBuilder.CreateIndex(
                name: "ix_season_rank_snapshot_rows_season_id_tier_tier_rank",
                table: "season_rank_snapshot_rows",
                columns: new[] { "season_id", "tier", "tier_rank" });

            migrationBuilder.CreateIndex(
                name: "ix_season_reward_claims_event_id",
                table: "season_reward_claims",
                column: "event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_season_reward_claims_season_id_player_id_reward_day",
                table: "season_reward_claims",
                columns: new[] { "season_id", "player_id", "reward_day" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_seasons_ends_at_utc",
                table: "seasons",
                column: "ends_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_seasons_season_number",
                table: "seasons",
                column: "season_number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_seasons_starts_at_utc",
                table: "seasons",
                column: "starts_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_seasons_status",
                table: "seasons",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_skill_nodes_branch",
                table: "skill_nodes",
                column: "branch");

            migrationBuilder.CreateIndex(
                name: "ix_skill_nodes_key",
                table: "skill_nodes",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_skill_nodes_tier",
                table: "skill_nodes",
                column: "tier");

            migrationBuilder.CreateIndex(
                name: "ix_territory_duels_match_id",
                table: "territory_duels",
                column: "match_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_territory_duels_season_id_tier_number_category",
                table: "territory_duels",
                columns: new[] { "season_id", "tier_number", "category" });

            migrationBuilder.CreateIndex(
                name: "ix_territory_tiles_owner_id",
                table: "territory_tiles",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "ix_territory_tiles_season_id_tier_number_category",
                table: "territory_tiles",
                columns: new[] { "season_id", "tier_number", "category" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tier_guardians_season_id_tier_number",
                table: "tier_guardians",
                columns: new[] { "season_id", "tier_number" });

            migrationBuilder.CreateIndex(
                name: "ix_tier_guardians_season_id_tier_number_player_id",
                table: "tier_guardians",
                columns: new[] { "season_id", "tier_number", "player_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_created_at",
                table: "users",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_handle",
                table: "users",
                column: "handle",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_votes_player_id_topic",
                table: "votes",
                columns: new[] { "player_id", "topic" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_votes_topic",
                table: "votes",
                column: "topic");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_app_config");

            migrationBuilder.DropTable(
                name: "admin_email_acls");

            migrationBuilder.DropTable(
                name: "admin_notification_channels");

            migrationBuilder.DropTable(
                name: "admin_notification_history");

            migrationBuilder.DropTable(
                name: "admin_notification_schedules");

            migrationBuilder.DropTable(
                name: "admin_notification_templates");

            migrationBuilder.DropTable(
                name: "anti_cheat_flags");

            migrationBuilder.DropTable(
                name: "economy_transaction_lines");

            migrationBuilder.DropTable(
                name: "friend_edges");

            migrationBuilder.DropTable(
                name: "friend_requests");

            migrationBuilder.DropTable(
                name: "game_balance_configs");

            migrationBuilder.DropTable(
                name: "game_event_participants");

            migrationBuilder.DropTable(
                name: "game_event_prize_claims");

            migrationBuilder.DropTable(
                name: "game_events");

            migrationBuilder.DropTable(
                name: "guardian_challenges");

            migrationBuilder.DropTable(
                name: "leaderboard_entries");

            migrationBuilder.DropTable(
                name: "match_participant_results");

            migrationBuilder.DropTable(
                name: "match_rounds");

            migrationBuilder.DropTable(
                name: "matchmaking_tickets");

            migrationBuilder.DropTable(
                name: "mission_claims");

            migrationBuilder.DropTable(
                name: "missions");

            migrationBuilder.DropTable(
                name: "moderation_action_logs");

            migrationBuilder.DropTable(
                name: "parties");

            migrationBuilder.DropTable(
                name: "party_invites");

            migrationBuilder.DropTable(
                name: "party_match_links");

            migrationBuilder.DropTable(
                name: "party_match_members");

            migrationBuilder.DropTable(
                name: "party_matchmaking_tickets");

            migrationBuilder.DropTable(
                name: "party_members");

            migrationBuilder.DropTable(
                name: "player_economy_safeguard_states");

            migrationBuilder.DropTable(
                name: "player_event_stats");

            migrationBuilder.DropTable(
                name: "player_moderation_profiles");

            migrationBuilder.DropTable(
                name: "player_powerups");

            migrationBuilder.DropTable(
                name: "player_season_profiles");

            migrationBuilder.DropTable(
                name: "player_skill_unlocks");

            migrationBuilder.DropTable(
                name: "player_transaction_actors");

            migrationBuilder.DropTable(
                name: "player_transaction_items");

            migrationBuilder.DropTable(
                name: "player_wallets");

            migrationBuilder.DropTable(
                name: "players");

            migrationBuilder.DropTable(
                name: "processed_gameplay_events");

            migrationBuilder.DropTable(
                name: "qr_scan_events");

            migrationBuilder.DropTable(
                name: "question_answered_analytics_events");

            migrationBuilder.DropTable(
                name: "question_answered_daily_rollups");

            migrationBuilder.DropTable(
                name: "question_answered_player_daily_rollups");

            migrationBuilder.DropTable(
                name: "question_options");

            migrationBuilder.DropTable(
                name: "question_tags");

            migrationBuilder.DropTable(
                name: "referral_codes");

            migrationBuilder.DropTable(
                name: "referral_redemptions");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "season_point_transactions");

            migrationBuilder.DropTable(
                name: "season_rank_snapshot_rows");

            migrationBuilder.DropTable(
                name: "season_reward_claims");

            migrationBuilder.DropTable(
                name: "seasons");

            migrationBuilder.DropTable(
                name: "skill_nodes");

            migrationBuilder.DropTable(
                name: "territory_duels");

            migrationBuilder.DropTable(
                name: "territory_tiles");

            migrationBuilder.DropTable(
                name: "tier_guardians");

            migrationBuilder.DropTable(
                name: "tiers");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "votes");

            migrationBuilder.DropTable(
                name: "economy_transactions");

            migrationBuilder.DropTable(
                name: "match_results");

            migrationBuilder.DropTable(
                name: "matches");

            migrationBuilder.DropTable(
                name: "questions");

            migrationBuilder.DropTable(
                name: "player_transactions");
        }
    }
}

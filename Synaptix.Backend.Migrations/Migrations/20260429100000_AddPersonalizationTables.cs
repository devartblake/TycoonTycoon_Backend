using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synaptix.Backend.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonalizationTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "player_mind_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    confidence_level = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0.50m),
                    risk_tolerance = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0.50m),
                    preferred_pace = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "balanced"),
                    learning_style = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "mixed"),
                    competitive_preference = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "balanced"),
                    social_preference = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "solo"),
                    churn_risk_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0.00m),
                    frustration_risk_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0.00m),
                    reward_sensitivity_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0.50m),
                    store_affinity_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0.50m),
                    notification_fatigue_score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0.00m),
                    archetype = table.Column<string>(type: "character varying(96)", maxLength: 96, nullable: false, defaultValue: "new_player"),
                    category_strengths_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    category_weaknesses_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    preference_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    guardrail_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    sidecar_scores_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    personalization_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    sidecar_scoring_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    last_calculated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_player_mind_profiles", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_player_mind_profiles_player_id",
                table: "player_mind_profiles",
                column: "player_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_player_mind_profiles_archetype",
                table: "player_mind_profiles",
                column: "archetype");

            migrationBuilder.CreateIndex(
                name: "ix_player_mind_profiles_churn_risk",
                table: "player_mind_profiles",
                column: "churn_risk_score");

            migrationBuilder.CreateIndex(
                name: "ix_player_mind_profiles_frustration_risk",
                table: "player_mind_profiles",
                column: "frustration_risk_score");

            migrationBuilder.CreateIndex(
                name: "ix_player_mind_profiles_updated_at",
                table: "player_mind_profiles",
                column: "updated_at");

            migrationBuilder.CreateTable(
                name: "player_behavior_events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    event_source = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    category = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    difficulty = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    mode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    metadata_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ingested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_player_behavior_events", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_player_behavior_events_player_time",
                table: "player_behavior_events",
                columns: new[] { "player_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_player_behavior_events_type",
                table: "player_behavior_events",
                column: "event_type");

            migrationBuilder.CreateIndex(
                name: "ix_player_behavior_events_source",
                table: "player_behavior_events",
                column: "event_source");

            migrationBuilder.CreateIndex(
                name: "ix_player_behavior_events_category",
                table: "player_behavior_events",
                column: "category");

            migrationBuilder.CreateTable(
                name: "personalization_recommendations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recommendation_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "backend"),
                    priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    score = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0.50m),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    guardrail_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    accepted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    dismissed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_personalization_recommendations", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_personalization_recommendations_player_created",
                table: "personalization_recommendations",
                columns: new[] { "player_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_personalization_recommendations_type",
                table: "personalization_recommendations",
                column: "recommendation_type");

            migrationBuilder.CreateTable(
                name: "personalization_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_key = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false, defaultValue: ""),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    rule_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_personalization_rules", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_personalization_rules_rule_key",
                table: "personalization_rules",
                column: "rule_key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "personalization_rules");
            migrationBuilder.DropTable(name: "personalization_recommendations");
            migrationBuilder.DropTable(name: "player_behavior_events");
            migrationBuilder.DropTable(name: "player_mind_profiles");
        }
    }
}

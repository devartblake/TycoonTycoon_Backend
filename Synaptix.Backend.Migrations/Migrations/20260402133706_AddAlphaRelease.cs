using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synaptix.Backend.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddAlphaRelease : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "audience_segment",
                table: "question_answered_player_daily_rollups",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "brand_version",
                table: "question_answered_player_daily_rollups",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "entry_point",
                table: "question_answered_player_daily_rollups",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "surface",
                table: "question_answered_player_daily_rollups",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "synaptix_mode",
                table: "question_answered_player_daily_rollups",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "audience_segment",
                table: "question_answered_daily_rollups",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "brand_version",
                table: "question_answered_daily_rollups",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "entry_point",
                table: "question_answered_daily_rollups",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "surface",
                table: "question_answered_daily_rollups",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "synaptix_mode",
                table: "question_answered_daily_rollups",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "audience_segment",
                table: "question_answered_analytics_events",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "brand_version",
                table: "question_answered_analytics_events",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "entry_point",
                table: "question_answered_analytics_events",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "surface",
                table: "question_answered_analytics_events",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "synaptix_mode",
                table: "question_answered_analytics_events",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "player_preferences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    synaptix_mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    preferred_surface = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    reduced_motion = table.Column<bool>(type: "boolean", nullable: false),
                    tone_preference = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_player_preferences", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "store_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sku = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    item_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    price_coins = table.Column<int>(type: "integer", nullable: false),
                    price_diamonds = table.Column<int>(type: "integer", nullable: false),
                    grant_quantity = table.Column<int>(type: "integer", nullable: false),
                    max_per_player = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    media_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_store_items", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_player_preferences_player_id",
                table: "player_preferences",
                column: "player_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_store_items_sku",
                table: "store_items",
                column: "sku",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "player_preferences");

            migrationBuilder.DropTable(
                name: "store_items");

            migrationBuilder.DropColumn(
                name: "audience_segment",
                table: "question_answered_player_daily_rollups");

            migrationBuilder.DropColumn(
                name: "brand_version",
                table: "question_answered_player_daily_rollups");

            migrationBuilder.DropColumn(
                name: "entry_point",
                table: "question_answered_player_daily_rollups");

            migrationBuilder.DropColumn(
                name: "surface",
                table: "question_answered_player_daily_rollups");

            migrationBuilder.DropColumn(
                name: "synaptix_mode",
                table: "question_answered_player_daily_rollups");

            migrationBuilder.DropColumn(
                name: "audience_segment",
                table: "question_answered_daily_rollups");

            migrationBuilder.DropColumn(
                name: "brand_version",
                table: "question_answered_daily_rollups");

            migrationBuilder.DropColumn(
                name: "entry_point",
                table: "question_answered_daily_rollups");

            migrationBuilder.DropColumn(
                name: "surface",
                table: "question_answered_daily_rollups");

            migrationBuilder.DropColumn(
                name: "synaptix_mode",
                table: "question_answered_daily_rollups");

            migrationBuilder.DropColumn(
                name: "audience_segment",
                table: "question_answered_analytics_events");

            migrationBuilder.DropColumn(
                name: "brand_version",
                table: "question_answered_analytics_events");

            migrationBuilder.DropColumn(
                name: "entry_point",
                table: "question_answered_analytics_events");

            migrationBuilder.DropColumn(
                name: "surface",
                table: "question_answered_analytics_events");

            migrationBuilder.DropColumn(
                name: "synaptix_mode",
                table: "question_answered_analytics_events");
        }
    }
}

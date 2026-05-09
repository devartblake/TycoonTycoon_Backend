using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tycoon.Backend.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddDailyAndWeeklyRewards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "daily_reward_claims",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_date = table.Column<DateOnly>(type: "date", nullable: false),
                    coins_granted = table.Column<int>(type: "integer", nullable: false),
                    claimed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_daily_reward_claims", x => x.id);
                });

            // One claim per player per calendar day
            migrationBuilder.CreateIndex(
                name: "ix_daily_reward_claims_player_id_claim_date",
                table: "daily_reward_claims",
                columns: ["player_id", "claim_date"],
                unique: true);

            migrationBuilder.CreateTable(
                name: "weekly_streak_states",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cycle_start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    current_day = table.Column<int>(type: "integer", nullable: false),
                    claimed_days_json = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_weekly_streak_states", x => x.id);
                });

            // One streak record per player
            migrationBuilder.CreateIndex(
                name: "ix_weekly_streak_states_player_id",
                table: "weekly_streak_states",
                column: "player_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "daily_reward_claims");
            migrationBuilder.DropTable(name: "weekly_streak_states");
        }
    }
}

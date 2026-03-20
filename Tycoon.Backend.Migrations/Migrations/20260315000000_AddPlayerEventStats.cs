using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tycoon.Backend.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddPlayerEventStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "player_event_stats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SeasonId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventsEntered = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    EventsTop20 = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    EventsWon = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TotalEventXpEarned = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TotalEventCoinsEarned = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ChampionBattleEliminations = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    GuardianPromotions = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    GuardianDefencesWon = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    GuardianDefencesLost = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    GuardianDaysTotal = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TilesEverCaptured = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CurrentTilesOwned = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    PeakXpMultiplierBps = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_event_stats", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_player_event_stats_SeasonId_PlayerId",
                table: "player_event_stats",
                columns: new[] { "SeasonId", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_player_event_stats_SeasonId_EventsWon",
                table: "player_event_stats",
                columns: new[] { "SeasonId", "EventsWon" });

            migrationBuilder.CreateIndex(
                name: "IX_player_event_stats_SeasonId_GuardianDefencesWon",
                table: "player_event_stats",
                columns: new[] { "SeasonId", "GuardianDefencesWon" });

            migrationBuilder.CreateIndex(
                name: "IX_player_event_stats_SeasonId_CurrentTilesOwned",
                table: "player_event_stats",
                columns: new[] { "SeasonId", "CurrentTilesOwned" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "player_event_stats");
        }
    }
}

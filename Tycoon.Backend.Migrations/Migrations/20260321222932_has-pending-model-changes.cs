using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tycoon.Backend.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class haspendingmodelchanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "game_balance_configs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ConfigJson = table.Column<string>(type: "jsonb", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_balance_configs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "player_economy_safeguard_states",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionsStarted = table.Column<int>(type: "integer", nullable: false),
                    LossStreak = table.Column<int>(type: "integer", nullable: false),
                    CurrentEnergy = table.Column<int>(type: "integer", nullable: false),
                    LastEnergyRegenAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastFreeTicketClaimDate = table.Column<DateOnly>(type: "date", nullable: true),
                    FreeTicketsClaimedToday = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_economy_safeguard_states", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_player_economy_safeguard_states_PlayerId",
                table: "player_economy_safeguard_states",
                column: "PlayerId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "game_balance_configs");

            migrationBuilder.DropTable(
                name: "player_economy_safeguard_states");
        }
    }
}

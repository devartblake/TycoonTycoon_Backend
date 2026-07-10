using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synaptix.Backend.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddChampionVsTierEventFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "champion_player_id",
                table: "game_events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "jackpot_multiplier",
                table: "game_events",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 1.0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "champion_player_id",
                table: "game_events");

            migrationBuilder.DropColumn(
                name: "jackpot_multiplier",
                table: "game_events");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synaptix.Backend.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddSponsorAttribution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "sponsor_name",
                table: "game_events",
                type: "character varying(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "game_event_sponsor_attributions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sponsor_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    base_jackpot = table.Column<int>(type: "integer", nullable: false),
                    multiplier = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    effective_jackpot = table.Column<int>(type: "integer", nullable: false),
                    boost_amount = table.Column<int>(type: "integer", nullable: false),
                    beneficiary_player_id = table.Column<Guid>(type: "uuid", nullable: true),
                    season_id = table.Column<Guid>(type: "uuid", nullable: true),
                    recorded_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_game_event_sponsor_attributions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_game_event_sponsor_attributions_game_event_id",
                table: "game_event_sponsor_attributions",
                column: "game_event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_game_event_sponsor_attributions_sponsor_name_season_id",
                table: "game_event_sponsor_attributions",
                columns: new[] { "sponsor_name", "season_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "game_event_sponsor_attributions");

            migrationBuilder.DropColumn(
                name: "sponsor_name",
                table: "game_events");
        }
    }
}

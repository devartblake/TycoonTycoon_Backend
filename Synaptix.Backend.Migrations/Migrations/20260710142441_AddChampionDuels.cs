using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synaptix.Backend.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddChampionDuels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "champion_duels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    champion_player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    challenger_player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    correct_option_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deadline_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    champion_option_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    champion_answered_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    challenger_option_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    challenger_answered_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    winner_player_id = table.Column<Guid>(type: "uuid", nullable: true),
                    loser_player_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resolved_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_champion_duels", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_champion_duels_game_event_id_status",
                table: "champion_duels",
                columns: new[] { "game_event_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "champion_duels");
        }
    }
}

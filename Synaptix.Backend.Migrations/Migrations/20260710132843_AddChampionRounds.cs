using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synaptix.Backend.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddChampionRounds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "champion_round_answers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    round_id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    selected_option_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    is_correct = table.Column<bool>(type: "boolean", nullable: false),
                    submitted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_champion_round_answers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "champion_rounds",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    round_number = table.Column<int>(type: "integer", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    correct_option_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    started_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deadline_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    resolved_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_champion_rounds", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_champion_round_answers_round_id_player_id",
                table: "champion_round_answers",
                columns: new[] { "round_id", "player_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_champion_rounds_game_event_id_round_number",
                table: "champion_rounds",
                columns: new[] { "game_event_id", "round_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_champion_rounds_game_event_id_status",
                table: "champion_rounds",
                columns: new[] { "game_event_id", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "champion_round_answers");

            migrationBuilder.DropTable(
                name: "champion_rounds");
        }
    }
}

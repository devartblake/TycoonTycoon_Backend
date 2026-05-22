using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synaptix.Backend.Migrations.Migrations
{
    public partial class AddStudySessions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "study_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    study_set_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    question_count = table.Column<int>(type: "integer", nullable: false),
                    question_ids_json = table.Column<string>(type: "text", nullable: false),
                    answer_key_json = table.Column<string>(type: "text", nullable: false),
                    answered_results_json = table.Column<string>(type: "text", nullable: false),
                    answered_count = table.Column<int>(type: "integer", nullable: false),
                    correct_count = table.Column<int>(type: "integer", nullable: false),
                    current_question_index = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_study_sessions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_study_sessions_player_id",
                table: "study_sessions",
                column: "player_id");

            migrationBuilder.CreateIndex(
                name: "ix_study_sessions_player_id_created_at_utc",
                table: "study_sessions",
                columns: new[] { "player_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_study_sessions_player_id_study_set_id_completed_at_utc",
                table: "study_sessions",
                columns: new[] { "player_id", "study_set_id", "completed_at_utc" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "study_sessions");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synaptix.Backend.Migrations.Migrations
{
    public partial class AddQuestionStudyFavoritesAndSessionMode : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "mode",
                table: "study_sessions",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "SelfTest");

            migrationBuilder.CreateTable(
                name: "question_study_favorites",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_question_study_favorites", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_question_study_favorites_player_id",
                table: "question_study_favorites",
                column: "player_id");

            migrationBuilder.CreateIndex(
                name: "ix_question_study_favorites_player_id_question_id",
                table: "question_study_favorites",
                columns: new[] { "player_id", "question_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_question_study_favorites_question_id",
                table: "question_study_favorites",
                column: "question_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "question_study_favorites");

            migrationBuilder.DropColumn(
                name: "mode",
                table: "study_sessions");
        }
    }
}

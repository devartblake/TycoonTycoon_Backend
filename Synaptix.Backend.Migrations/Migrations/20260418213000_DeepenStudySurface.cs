using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synaptix.Backend.Migrations.Migrations
{
    public partial class DeepenStudySurface : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "interaction_states_json",
                table: "study_sessions",
                type: "text",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.CreateTable(
                name: "study_card_states",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    review_count = table.Column<int>(type: "integer", nullable: false),
                    success_streak = table.Column<int>(type: "integer", nullable: false),
                    ease_factor = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false),
                    last_reviewed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    next_review_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_outcome = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    last_mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    last_confidence = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_study_card_states", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "study_sets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_study_sets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "study_set_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    study_set_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_study_set_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_study_set_items_study_sets_study_set_id",
                        column: x => x.study_set_id,
                        principalTable: "study_sets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_study_card_states_player_id",
                table: "study_card_states",
                column: "player_id");

            migrationBuilder.CreateIndex(
                name: "ix_study_card_states_question_id",
                table: "study_card_states",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "ix_study_card_states_player_id_next_review_at_utc",
                table: "study_card_states",
                columns: new[] { "player_id", "next_review_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_study_card_states_player_id_question_id",
                table: "study_card_states",
                columns: new[] { "player_id", "question_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_study_set_items_study_set_id",
                table: "study_set_items",
                column: "study_set_id");

            migrationBuilder.CreateIndex(
                name: "ix_study_set_items_study_set_id_order",
                table: "study_set_items",
                columns: new[] { "study_set_id", "order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_study_sets_player_id",
                table: "study_sets",
                column: "player_id");

            migrationBuilder.CreateIndex(
                name: "ix_study_sets_player_id_updated_at_utc",
                table: "study_sets",
                columns: new[] { "player_id", "updated_at_utc" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "study_card_states");

            migrationBuilder.DropTable(
                name: "study_set_items");

            migrationBuilder.DropTable(
                name: "study_sets");

            migrationBuilder.DropColumn(
                name: "interaction_states_json",
                table: "study_sessions");
        }
    }
}

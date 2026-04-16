using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tycoon.Backend.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddLearningModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "learning_modules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    difficulty = table.Column<int>(type: "integer", nullable: false),
                    reward_xp = table.Column<int>(type: "integer", nullable: false),
                    reward_coins = table.Column<int>(type: "integer", nullable: false),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_learning_modules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "module_completions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    module_id = table.Column<Guid>(type: "uuid", nullable: false),
                    economy_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    completed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_module_completions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "module_lessons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    module_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order = table.Column<int>(type: "integer", nullable: false),
                    explanation = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_module_lessons", x => x.id);
                    table.ForeignKey(
                        name: "fk_module_lessons_learning_modules_module_id",
                        column: x => x.module_id,
                        principalTable: "learning_modules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_learning_modules_category",
                table: "learning_modules",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "ix_learning_modules_difficulty",
                table: "learning_modules",
                column: "difficulty");

            migrationBuilder.CreateIndex(
                name: "ix_learning_modules_is_published",
                table: "learning_modules",
                column: "is_published");

            migrationBuilder.CreateIndex(
                name: "ix_module_completions_economy_event_id",
                table: "module_completions",
                column: "economy_event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_module_completions_module_id",
                table: "module_completions",
                column: "module_id");

            migrationBuilder.CreateIndex(
                name: "ix_module_completions_player_id",
                table: "module_completions",
                column: "player_id");

            migrationBuilder.CreateIndex(
                name: "ix_module_completions_player_id_module_id",
                table: "module_completions",
                columns: new[] { "player_id", "module_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_module_lessons_module_id",
                table: "module_lessons",
                column: "module_id");

            migrationBuilder.CreateIndex(
                name: "ix_module_lessons_module_id_order",
                table: "module_lessons",
                columns: new[] { "module_id", "order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_module_lessons_question_id",
                table: "module_lessons",
                column: "question_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "module_completions");

            migrationBuilder.DropTable(
                name: "module_lessons");

            migrationBuilder.DropTable(
                name: "learning_modules");
        }
    }
}

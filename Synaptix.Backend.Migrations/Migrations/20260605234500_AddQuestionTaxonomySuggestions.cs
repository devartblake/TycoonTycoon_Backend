using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synaptix.Backend.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddQuestionTaxonomySuggestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "question_taxonomy_suggestions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_dataset = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    source_question_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    suggested_taxonomy_json = table.Column<string>(type: "jsonb", nullable: false),
                    confidence_json = table.Column<string>(type: "jsonb", nullable: false),
                    warnings_json = table.Column<string>(type: "jsonb", nullable: false),
                    overall_confidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    model_version = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    applied_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reviewed_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    review_note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_question_taxonomy_suggestions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_question_taxonomy_suggestions_question_id",
                table: "question_taxonomy_suggestions",
                column: "question_id");

            migrationBuilder.CreateIndex(
                name: "ix_question_taxonomy_suggestions_source",
                table: "question_taxonomy_suggestions",
                columns: new[] { "source_dataset", "source_question_id" });

            migrationBuilder.CreateIndex(
                name: "ix_question_taxonomy_suggestions_status_created_at_utc",
                table: "question_taxonomy_suggestions",
                columns: new[] { "status", "created_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "question_taxonomy_suggestions");
        }
    }
}

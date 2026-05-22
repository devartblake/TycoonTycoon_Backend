using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synaptix.Backend.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonalizationAuditLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "personalization_audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recommendation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    decision_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "backend"),
                    reason = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false, defaultValue: ""),
                    input_signals_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    candidate_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    guardrails_applied_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    final_decision_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_personalization_audit_logs", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_personalization_audit_logs_player_created",
                table: "personalization_audit_logs",
                columns: new[] { "player_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_personalization_audit_logs_decision_type",
                table: "personalization_audit_logs",
                column: "decision_type");

            migrationBuilder.CreateIndex(
                name: "ix_personalization_audit_logs_recommendation_id",
                table: "personalization_audit_logs",
                column: "recommendation_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "personalization_audit_logs");
        }
    }
}

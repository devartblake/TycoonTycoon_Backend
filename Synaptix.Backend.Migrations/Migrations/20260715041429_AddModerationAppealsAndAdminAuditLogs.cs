using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synaptix.Backend.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddModerationAppealsAndAdminAuditLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "admin_audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    resource_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    resource_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    changes_before_json = table.Column<string>(type: "text", nullable: true),
                    changes_after_json = table.Column<string>(type: "text", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admin_audit_logs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "moderation_appeals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    reviewer_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    reviewed_by = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    submitted_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reviewed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_moderation_appeals", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_admin_audit_logs_action",
                table: "admin_audit_logs",
                column: "action");

            migrationBuilder.CreateIndex(
                name: "ix_admin_audit_logs_actor",
                table: "admin_audit_logs",
                column: "actor");

            migrationBuilder.CreateIndex(
                name: "ix_admin_audit_logs_created_at_utc",
                table: "admin_audit_logs",
                column: "created_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_admin_audit_logs_resource_type_resource_id",
                table: "admin_audit_logs",
                columns: new[] { "resource_type", "resource_id" });

            migrationBuilder.CreateIndex(
                name: "ix_moderation_appeals_player_id",
                table: "moderation_appeals",
                column: "player_id");

            migrationBuilder.CreateIndex(
                name: "ix_moderation_appeals_player_id_status",
                table: "moderation_appeals",
                columns: new[] { "player_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_moderation_appeals_status_submitted_at_utc",
                table: "moderation_appeals",
                columns: new[] { "status", "submitted_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_audit_logs");

            migrationBuilder.DropTable(
                name: "moderation_appeals");
        }
    }
}

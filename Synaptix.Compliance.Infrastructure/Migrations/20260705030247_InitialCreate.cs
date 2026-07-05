using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synaptix.Compliance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "compliance");

            migrationBuilder.CreateTable(
                name: "age_verifications",
                schema: "compliance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    declared_age = table.Column<int>(type: "integer", nullable: false),
                    is_minor = table.Column<bool>(type: "boolean", nullable: false),
                    verification_method = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_age_verifications", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_events",
                schema: "compliance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    event_data = table.Column<string>(type: "text", nullable: true),
                    source = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_events", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "consent_records",
                schema: "compliance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    consent_type = table.Column<int>(type: "integer", nullable: false),
                    consent_given = table.Column<bool>(type: "boolean", nullable: false),
                    policy_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_consent_records", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "parental_consents",
                schema: "compliance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_email_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    requested_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    granted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_parental_consents", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "privacy_requests",
                schema: "compliance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_type = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    submitted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    notes = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_privacy_requests", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_age_verifications_user_id",
                schema: "compliance",
                table: "age_verifications",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_age_verifications_user_id_verified_at",
                schema: "compliance",
                table: "age_verifications",
                columns: new[] { "user_id", "verified_at" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_event_type_occurred_at",
                schema: "compliance",
                table: "audit_events",
                columns: new[] { "event_type", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_events_user_id",
                schema: "compliance",
                table: "audit_events",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_consent_records_user_id",
                schema: "compliance",
                table: "consent_records",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_consent_records_user_id_consent_type_recorded_at",
                schema: "compliance",
                table: "consent_records",
                columns: new[] { "user_id", "consent_type", "recorded_at" });

            migrationBuilder.CreateIndex(
                name: "ix_parental_consents_token_hash",
                schema: "compliance",
                table: "parental_consents",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_parental_consents_user_id",
                schema: "compliance",
                table: "parental_consents",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_parental_consents_user_id_status",
                schema: "compliance",
                table: "parental_consents",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_privacy_requests_status",
                schema: "compliance",
                table: "privacy_requests",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_privacy_requests_status_submitted_at",
                schema: "compliance",
                table: "privacy_requests",
                columns: new[] { "status", "submitted_at" });

            migrationBuilder.CreateIndex(
                name: "ix_privacy_requests_user_id",
                schema: "compliance",
                table: "privacy_requests",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "age_verifications",
                schema: "compliance");

            migrationBuilder.DropTable(
                name: "audit_events",
                schema: "compliance");

            migrationBuilder.DropTable(
                name: "consent_records",
                schema: "compliance");

            migrationBuilder.DropTable(
                name: "parental_consents",
                schema: "compliance");

            migrationBuilder.DropTable(
                name: "privacy_requests",
                schema: "compliance");
        }
    }
}

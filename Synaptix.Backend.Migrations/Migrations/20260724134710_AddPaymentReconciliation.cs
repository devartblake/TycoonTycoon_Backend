using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synaptix.Backend.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentReconciliation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "payment_checkout_attempts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    sku = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    expected_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    provider_ref = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    provider_capture_ref = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    player_transaction_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    resolved_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_checkout_attempts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "payment_reconciliation_issues",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<int>(type: "integer", nullable: false),
                    provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    provider_ref = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    payment_checkout_attempt_id = table.Column<Guid>(type: "uuid", nullable: true),
                    player_id = table.Column<Guid>(type: "uuid", nullable: true),
                    expected_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    actual_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    details = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    resolved_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    resolved_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    resolution_notes = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_reconciliation_issues", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_payment_checkout_attempts_player_id",
                table: "payment_checkout_attempts",
                column: "player_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_checkout_attempts_provider_provider_ref",
                table: "payment_checkout_attempts",
                columns: new[] { "provider", "provider_ref" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payment_checkout_attempts_status_created_at_utc",
                table: "payment_checkout_attempts",
                columns: new[] { "status", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_reconciliation_issues_payment_checkout_attempt_id",
                table: "payment_reconciliation_issues",
                column: "payment_checkout_attempt_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_reconciliation_issues_resolved_at_utc_created_at_utc",
                table: "payment_reconciliation_issues",
                columns: new[] { "resolved_at_utc", "created_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payment_checkout_attempts");

            migrationBuilder.DropTable(
                name: "payment_reconciliation_issues");
        }
    }
}

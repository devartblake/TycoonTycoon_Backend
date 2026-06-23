using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synaptix.Backend.Migrations.Migrations
{
    /// <inheritdoc />
    [Migration("20260616000000_AddCompliancePhase1")]
    public partial class AddCompliancePhase1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // StoreItem compliance columns
            migrationBuilder.AddColumn<bool>(
                name: "is_randomized",
                table: "store_items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "age_min",
                table: "store_items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "requires_parent_approval",
                table: "store_items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "is_refundable",
                table: "store_items",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            // ParentalPurchaseControl table
            migrationBuilder.CreateTable(
                name: "parental_purchase_controls",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    child_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    purchases_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    monthly_spend_limit_cents = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ads_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    loot_boxes_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_parental_purchase_controls", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_parental_purchase_controls_child_user_id",
                table: "parental_purchase_controls",
                column: "child_user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "parental_purchase_controls");

            migrationBuilder.DropColumn(name: "is_randomized", table: "store_items");
            migrationBuilder.DropColumn(name: "age_min", table: "store_items");
            migrationBuilder.DropColumn(name: "requires_parent_approval", table: "store_items");
            migrationBuilder.DropColumn(name: "is_refundable", table: "store_items");
        }
    }
}

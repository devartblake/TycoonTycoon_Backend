using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synaptix.Backend.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreStockSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "store_stock_policies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sku = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    max_quantity_per_user = table.Column<int>(type: "integer", nullable: false),
                    reset_interval = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_store_stock_policies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "player_store_stock_states",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sku = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    quantity_used = table.Column<int>(type: "integer", nullable: false),
                    last_reset_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    next_reset_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_player_store_stock_states", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_store_stock_policies_sku",
                table: "store_stock_policies",
                column: "sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_player_store_stock_states_player_id",
                table: "player_store_stock_states",
                column: "player_id");

            migrationBuilder.CreateIndex(
                name: "ix_player_store_stock_states_player_id_sku",
                table: "player_store_stock_states",
                columns: new[] { "player_id", "sku" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "player_store_stock_states");
            migrationBuilder.DropTable(name: "store_stock_policies");
        }
    }
}

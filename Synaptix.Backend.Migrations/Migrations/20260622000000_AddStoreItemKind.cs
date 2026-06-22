using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synaptix.Backend.Migrations.Migrations
{
    /// <inheritdoc />
    [Migration("20260622000000_AddStoreItemKind")]
    public partial class AddStoreItemKind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "item_kind",
                table: "store_items",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE store_items SET item_kind = 1 WHERE max_per_player = 1");
            migrationBuilder.Sql("UPDATE store_items SET item_kind = 2 WHERE sku LIKE 'sub:%'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "item_kind",
                table: "store_items");
        }
    }
}

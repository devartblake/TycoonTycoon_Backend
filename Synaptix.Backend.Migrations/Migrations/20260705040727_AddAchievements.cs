using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synaptix.Backend.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddAchievements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "achievements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    description = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    category = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    points = table.Column<int>(type: "integer", nullable: false),
                    icon_url = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    is_secret = table.Column<bool>(type: "boolean", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_achievements", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "player_achievements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    achievement_key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    unlocked_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_player_achievements", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_achievements_category",
                table: "achievements",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "ix_achievements_key",
                table: "achievements",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_player_achievements_player_id",
                table: "player_achievements",
                column: "player_id");

            migrationBuilder.CreateIndex(
                name: "ix_player_achievements_player_id_achievement_key",
                table: "player_achievements",
                columns: new[] { "player_id", "achievement_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "player_achievements");

            migrationBuilder.DropTable(
                name: "achievements");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synaptix.Backend.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddSeasonRewardRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "season_reward_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tier = table.Column<int>(type: "integer", nullable: false),
                    max_tier_rank = table.Column<int>(type: "integer", nullable: false),
                    reward_xp = table.Column<int>(type: "integer", nullable: false),
                    reward_coins = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_season_reward_rules", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_season_reward_rules_tier_max_tier_rank",
                table: "season_reward_rules",
                columns: new[] { "tier", "max_tier_rank" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "season_reward_rules");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tycoon.Backend.Migrations.Migrations
{
    public partial class AddSeasonRankSnapshotRows : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "season_rank_snapshot_rows",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    season_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),

                    rank_points = table.Column<int>(type: "integer", nullable: false),
                    tier = table.Column<int>(type: "integer", nullable: false),
                    tier_rank = table.Column<int>(type: "integer", nullable: false),
                    season_rank = table.Column<int>(type: "integer", nullable: false),

                    wins = table.Column<int>(type: "integer", nullable: false),
                    losses = table.Column<int>(type: "integer", nullable: false),
                    draws = table.Column<int>(type: "integer", nullable: false),
                    matches_played = table.Column<int>(type: "integer", nullable: false),

                    captured_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_season_rank_snapshot_rows", x => x.id);
                });

            // One row per player per season (immutable snapshot uniqueness)
            migrationBuilder.CreateIndex(
                name: "ux_season_rank_snapshot_rows_season_id_player_id",
                table: "season_rank_snapshot_rows",
                columns: new[] { "season_id", "player_id" },
                unique: true);

            // Fast global/season leaderboard ordering
            migrationBuilder.CreateIndex(
                name: "ix_season_rank_snapshot_rows_season_id_season_rank",
                table: "season_rank_snapshot_rows",
                columns: new[] { "season_id", "season_rank" });

            // Fast tier leaderboard ordering
            migrationBuilder.CreateIndex(
                name: "ix_season_rank_snapshot_rows_season_id_tier_tier_rank",
                table: "season_rank_snapshot_rows",
                columns: new[] { "season_id", "tier", "tier_rank" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "season_rank_snapshot_rows");
        }
    }
}

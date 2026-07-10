using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synaptix.Backend.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddSeasonTiebreakers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "season_tiebreakers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    season_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scope = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    tier = table.Column<int>(type: "integer", nullable: false),
                    boundary_rank = table.Column<int>(type: "integer", nullable: false),
                    rank_points = table.Column<int>(type: "integer", nullable: false),
                    player_ids = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    scheduled_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    match_id = table.Column<Guid>(type: "uuid", nullable: true),
                    winner_player_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resolution_note = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    resolved_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_season_tiebreakers", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_season_tiebreakers_match_id",
                table: "season_tiebreakers",
                column: "match_id");

            migrationBuilder.CreateIndex(
                name: "ix_season_tiebreakers_season_id",
                table: "season_tiebreakers",
                column: "season_id");

            migrationBuilder.CreateIndex(
                name: "ix_season_tiebreakers_status_expires_at_utc",
                table: "season_tiebreakers",
                columns: new[] { "status", "expires_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "season_tiebreakers");
        }
    }
}

using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tycoon.Backend.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddArcadeSpinClaims : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "arcade_spin_claims",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    segment_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    spin_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    coins_granted = table.Column<int>(type: "integer", nullable: false),
                    claimed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_arcade_spin_claims", x => x.id);
                });

            // Idempotency guard — one spinId can only ever be claimed once
            migrationBuilder.CreateIndex(
                name: "ix_arcade_spin_claims_spin_id",
                table: "arcade_spin_claims",
                column: "spin_id",
                unique: true);

            // Per-player claim history queries
            migrationBuilder.CreateIndex(
                name: "ix_arcade_spin_claims_player_id_claimed_at_utc",
                table: "arcade_spin_claims",
                columns: ["player_id", "claimed_at_utc"]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "arcade_spin_claims");
        }
    }
}

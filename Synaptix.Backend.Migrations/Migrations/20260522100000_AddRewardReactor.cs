using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synaptix.Backend.Migrations.Migrations
{
    public partial class AddRewardReactor : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reward_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    spin_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mechanism = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    reward_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    reward_lines_json = table.Column<string>(type: "text", nullable: false),
                    animation_json = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    claim_token_hash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    claimed_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    policy_snapshot_json = table.Column<string>(type: "text", nullable: true),
                    reactor_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reward_sessions", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_reward_sessions_spin_id",
                table: "reward_sessions",
                column: "spin_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_reward_sessions_player_id_idempotency_key",
                table: "reward_sessions",
                columns: ["player_id", "idempotency_key"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_reward_sessions_player_id_mechanism_created_at_utc",
                table: "reward_sessions",
                columns: ["player_id", "mechanism", "created_at_utc"]);

            migrationBuilder.CreateTable(
                name: "reward_claim_ledger",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mechanism = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    spin_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    reward_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    reward_lines_json = table.Column<string>(type: "text", nullable: false),
                    claim_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    applied_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_correlation_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reward_claim_ledger", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_reward_claim_ledger_player_id_spin_id",
                table: "reward_claim_ledger",
                columns: ["player_id", "spin_id"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_reward_claim_ledger_player_id_idempotency_key",
                table: "reward_claim_ledger",
                columns: ["player_id", "idempotency_key"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_reward_claim_ledger_player_id_mechanism_applied_at_utc",
                table: "reward_claim_ledger",
                columns: ["player_id", "mechanism", "applied_at_utc"]);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "reward_sessions");
            migrationBuilder.DropTable(name: "reward_claim_ledger");
        }
    }
}

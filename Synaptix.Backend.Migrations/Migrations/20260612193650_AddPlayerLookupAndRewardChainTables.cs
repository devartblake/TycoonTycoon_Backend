using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synaptix.Backend.Migrations.Migrations
{
    /// <summary>
    /// Repairs a desynced model snapshot and adds the two tables that had EF
    /// entities but no migration.
    ///
    /// The AppDbModelSnapshot had drifted from the migration history (a bad
    /// merge): it was missing the questions/learning_modules taxonomy columns,
    /// question_taxonomy_suggestions, reward_sessions and reward_claim_ledger —
    /// all of which were ALREADY created by earlier migrations (AddQuestionTaxonomy,
    /// AddQuestionTaxonomySuggestions, AddRewardReactor). Regenerating this
    /// migration rewrote the snapshot to match the model, which fixes the drift
    /// for all future `migrations add` operations.
    ///
    /// Only player_lookup_codes and reward_chain_tickets are genuinely unmigrated
    /// (their entities exist but no migration ever created the tables), so the
    /// Up/Down below is trimmed to just those. The already-migrated operations the
    /// generator emitted were removed — re-running them would fail with
    /// "already exists" on any database that has applied the prior migrations.
    /// </summary>
    public partial class AddPlayerLookupAndRewardChainTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "player_lookup_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    short_code = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_player_lookup_codes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "reward_chain_tickets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    chained_spin_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_spin_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    reward_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    reward_lines_json = table.Column<string>(type: "text", nullable: false),
                    animation_json = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    activated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    generated_spin_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    generated_claim_token = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reward_chain_tickets", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_player_lookup_codes_player_id",
                table: "player_lookup_codes",
                column: "player_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_player_lookup_codes_short_code",
                table: "player_lookup_codes",
                column: "short_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_player_lookup_codes_user_id",
                table: "player_lookup_codes",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_reward_chain_tickets_chained_spin_id",
                table: "reward_chain_tickets",
                column: "chained_spin_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_reward_chain_tickets_player_id_source_spin_id",
                table: "reward_chain_tickets",
                columns: new[] { "player_id", "source_spin_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_reward_chain_tickets_player_id_status_expires_at_utc",
                table: "reward_chain_tickets",
                columns: new[] { "player_id", "status", "expires_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "player_lookup_codes");

            migrationBuilder.DropTable(
                name: "reward_chain_tickets");
        }
    }
}

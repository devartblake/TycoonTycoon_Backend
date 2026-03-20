using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tycoon.Backend.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class haspendingmodelchanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "game_event_participants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntryEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    EliminatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FinalRank = table.Column<int>(type: "integer", nullable: true),
                    RevivesUsed = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_event_participants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "game_event_prize_claims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    AwardedXp = table.Column<int>(type: "integer", nullable: false),
                    AwardedCoins = table.Column<int>(type: "integer", nullable: false),
                    Rank = table.Column<int>(type: "integer", nullable: false),
                    ClaimedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_event_prize_claims", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "game_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TierId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ScheduledAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    OpenAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EntryFeeCoins = table.Column<int>(type: "integer", nullable: false),
                    ReviveCostGems = table.Column<int>(type: "integer", nullable: false),
                    JackpotPool = table.Column<int>(type: "integer", nullable: false),
                    MaxParticipants = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "guardian_challenges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SeasonId = table.Column<Guid>(type: "uuid", nullable: false),
                    TierNumber = table.Column<int>(type: "integer", nullable: false),
                    ChallengerId = table.Column<Guid>(type: "uuid", nullable: false),
                    GuardianId = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResolvedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guardian_challenges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "territory_duels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GameEventId = table.Column<Guid>(type: "uuid", nullable: true),
                    SeasonId = table.Column<Guid>(type: "uuid", nullable: false),
                    TierNumber = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ChallengerId = table.Column<Guid>(type: "uuid", nullable: false),
                    DefenderId = table.Column<Guid>(type: "uuid", nullable: true),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Outcome = table.Column<int>(type: "integer", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResolvedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_territory_duels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "territory_tiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SeasonId = table.Column<Guid>(type: "uuid", nullable: false),
                    TierNumber = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: true),
                    CapturedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    XpMultiplierBps = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_territory_tiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tier_guardians",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SeasonId = table.Column<Guid>(type: "uuid", nullable: false),
                    TierNumber = table.Column<int>(type: "integer", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PassiveCoins = table.Column<int>(type: "integer", nullable: false),
                    PassiveXp = table.Column<int>(type: "integer", nullable: false),
                    DefencesWon = table.Column<int>(type: "integer", nullable: false),
                    DefencesLost = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tier_guardians", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "votes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Option = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Topic = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    TimestampUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_votes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_game_event_participants_EntryEventId",
                table: "game_event_participants",
                column: "EntryEventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_game_event_participants_GameEventId_PlayerId",
                table: "game_event_participants",
                columns: new[] { "GameEventId", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_game_event_prize_claims_EventId",
                table: "game_event_prize_claims",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_game_event_prize_claims_GameEventId_PlayerId",
                table: "game_event_prize_claims",
                columns: new[] { "GameEventId", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_game_events_Kind_Status",
                table: "game_events",
                columns: new[] { "Kind", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_game_events_ScheduledAtUtc",
                table: "game_events",
                column: "ScheduledAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_game_events_Status",
                table: "game_events",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_guardian_challenges_MatchId",
                table: "guardian_challenges",
                column: "MatchId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_guardian_challenges_SeasonId_TierNumber_ChallengerId",
                table: "guardian_challenges",
                columns: new[] { "SeasonId", "TierNumber", "ChallengerId" });

            migrationBuilder.CreateIndex(
                name: "IX_territory_duels_MatchId",
                table: "territory_duels",
                column: "MatchId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_territory_duels_SeasonId_TierNumber_Category",
                table: "territory_duels",
                columns: new[] { "SeasonId", "TierNumber", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_territory_tiles_OwnerId",
                table: "territory_tiles",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_territory_tiles_SeasonId_TierNumber_Category",
                table: "territory_tiles",
                columns: new[] { "SeasonId", "TierNumber", "Category" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tier_guardians_SeasonId_TierNumber",
                table: "tier_guardians",
                columns: new[] { "SeasonId", "TierNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_tier_guardians_SeasonId_TierNumber_PlayerId",
                table: "tier_guardians",
                columns: new[] { "SeasonId", "TierNumber", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_votes_PlayerId_Topic",
                table: "votes",
                columns: new[] { "PlayerId", "Topic" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_votes_Topic",
                table: "votes",
                column: "Topic");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "game_event_participants");

            migrationBuilder.DropTable(
                name: "game_event_prize_claims");

            migrationBuilder.DropTable(
                name: "game_events");

            migrationBuilder.DropTable(
                name: "guardian_challenges");

            migrationBuilder.DropTable(
                name: "territory_duels");

            migrationBuilder.DropTable(
                name: "territory_tiles");

            migrationBuilder.DropTable(
                name: "tier_guardians");

            migrationBuilder.DropTable(
                name: "votes");
        }
    }
}

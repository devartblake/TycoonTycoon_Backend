using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tycoon.Backend.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"""
                CREATE TABLE IF NOT EXISTS anti_cheat_flags (
                    "Id" uuid NOT NULL,
                    "MatchId" uuid NOT NULL,
                    "PlayerId" uuid,
                    "RuleKey" character varying(64) NOT NULL,
                    "Severity" integer NOT NULL,
                    "Action" integer NOT NULL,
                    "Message" character varying(300) NOT NULL,
                    "EvidenceJson" text,
                    "CreatedAtUtc" timestamp with time zone NOT NULL,
                    "ReviewedAtUtc" timestamp with time zone,
                    "ReviewedBy" character varying(64),
                    "ReviewNote" character varying(400),
                    CONSTRAINT "PK_anti_cheat_flags" PRIMARY KEY ("Id")
                );
                """);

            migrationBuilder.CreateTable(
                name: "economy_transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReversalOfTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Note = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_transactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "friend_edges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    FriendPlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_friend_edges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "friend_requests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FromPlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToPlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RespondedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_friend_requests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeaderboardEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    GlobalRank = table.Column<int>(type: "integer", nullable: false),
                    TierRank = table.Column<int>(type: "integer", nullable: false),
                    TierId = table.Column<int>(type: "integer", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    XpProgress = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaderboardEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "match_results",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmitEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    QuestionCount = table.Column<int>(type: "integer", nullable: false),
                    EndedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_match_results", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "matches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HostPlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FinishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_matches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "matchmaking_tickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Mode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Tier = table.Column<int>(type: "integer", nullable: false),
                    Scope = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_matchmaking_tickets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MissionClaims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    MissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Progress = table.Column<int>(type: "integer", nullable: false),
                    Completed = table.Column<bool>(type: "boolean", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Claimed = table.Column<bool>(type: "boolean", nullable: false),
                    ClaimedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastResetAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MissionClaims", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Missions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Key = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Goal = table.Column<int>(type: "integer", nullable: false),
                    RewardXp = table.Column<int>(type: "integer", nullable: false),
                    RewardCoins = table.Column<int>(type: "integer", nullable: false),
                    RewardDiamonds = table.Column<int>(type: "integer", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Missions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "moderation_action_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    NewStatus = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SetByAdmin = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RelatedFlagId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_moderation_action_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "parties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LeaderPlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_parties", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "party_invites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromPlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToPlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RespondedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_party_invites", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "party_match_links",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClosedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_party_match_links", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "party_match_members",
                columns: table => new
                {
                    PartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CapturedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_party_match_members", x => new { x.PartyId, x.MatchId, x.PlayerId });
                });

            migrationBuilder.CreateTable(
                name: "party_matchmaking_tickets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    LeaderPlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Mode = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Tier = table.Column<int>(type: "integer", nullable: false),
                    Scope = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    PartySize = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RowVersion = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_party_matchmaking_tickets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "party_members",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    JoinedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_party_members", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "player_moderation_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SetByAdmin = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    SetAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_moderation_profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "player_powerups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    CooldownUntilUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_powerups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "player_season_profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SeasonId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    RankPoints = table.Column<int>(type: "integer", nullable: false),
                    Wins = table.Column<int>(type: "integer", nullable: false),
                    Losses = table.Column<int>(type: "integer", nullable: false),
                    Draws = table.Column<int>(type: "integer", nullable: false),
                    MatchesPlayed = table.Column<int>(type: "integer", nullable: false),
                    Tier = table.Column<int>(type: "integer", nullable: false),
                    TierRank = table.Column<int>(type: "integer", nullable: false),
                    SeasonRank = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PlacementMatchesCompleted = table.Column<int>(type: "integer", nullable: false),
                    LastPromotionAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastDemotionAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_season_profiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "player_skill_unlocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    NodeKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    UnlockedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_skill_unlocks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "player_wallets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Xp = table.Column<int>(type: "integer", nullable: false),
                    Coins = table.Column<int>(type: "integer", nullable: false),
                    Diamonds = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_player_wallets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "players",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CountryCode = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    TierId = table.Column<Guid>(type: "uuid", nullable: true),
                    Level = table.Column<int>(type: "integer", nullable: false),
                    Xp = table.Column<double>(type: "double precision", precision: 12, scale: 2, nullable: false),
                    Coins = table.Column<int>(type: "integer", nullable: false),
                    Diamonds = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_players", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "processed_gameplay_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_processed_gameplay_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "qr_scan_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    StoredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_qr_scan_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QuestionAnsweredAnalyticsEvents",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<string>(type: "text", nullable: false),
                    Mode = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Difficulty = table.Column<int>(type: "integer", nullable: false),
                    IsCorrect = table.Column<bool>(type: "boolean", nullable: false),
                    PointsAwarded = table.Column<int>(type: "integer", nullable: false),
                    AnswerTimeMs = table.Column<int>(type: "integer", nullable: false),
                    AnsweredAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionAnsweredAnalyticsEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QuestionAnsweredDailyRollups",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Day = table.Column<DateOnly>(type: "date", nullable: false),
                    Mode = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Difficulty = table.Column<int>(type: "integer", nullable: false),
                    TotalAnswers = table.Column<int>(type: "integer", nullable: false),
                    CorrectAnswers = table.Column<int>(type: "integer", nullable: false),
                    WrongAnswers = table.Column<int>(type: "integer", nullable: false),
                    SumAnswerTimeMs = table.Column<long>(type: "bigint", nullable: false),
                    MinAnswerTimeMs = table.Column<int>(type: "integer", nullable: false),
                    MaxAnswerTimeMs = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionAnsweredDailyRollups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QuestionAnsweredPlayerDailyRollups",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Day = table.Column<DateOnly>(type: "date", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Mode = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Difficulty = table.Column<int>(type: "integer", nullable: false),
                    TotalAnswers = table.Column<int>(type: "integer", nullable: false),
                    CorrectAnswers = table.Column<int>(type: "integer", nullable: false),
                    WrongAnswers = table.Column<int>(type: "integer", nullable: false),
                    SumAnswerTimeMs = table.Column<long>(type: "bigint", nullable: false),
                    MinAnswerTimeMs = table.Column<int>(type: "integer", nullable: false),
                    MaxAnswerTimeMs = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QuestionAnsweredPlayerDailyRollups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "questions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Category = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Difficulty = table.Column<int>(type: "integer", nullable: false),
                    CorrectOptionId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MediaKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_questions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "referral_codes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    OwnerPlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_referral_codes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "referral_redemptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReferralCodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerPlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    RedeemerPlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    AwardXpToOwner = table.Column<int>(type: "integer", nullable: false),
                    AwardCoinsToOwner = table.Column<int>(type: "integer", nullable: false),
                    AwardXpToRedeemer = table.Column<int>(type: "integer", nullable: false),
                    AwardCoinsToRedeemer = table.Column<int>(type: "integer", nullable: false),
                    RedeemedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_referral_redemptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DeviceId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "season_point_transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeasonId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    Delta = table.Column<int>(type: "integer", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_season_point_transactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "season_rank_snapshot_rows",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SeasonId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    RankPoints = table.Column<int>(type: "integer", nullable: false),
                    Tier = table.Column<int>(type: "integer", nullable: false),
                    TierRank = table.Column<int>(type: "integer", nullable: false),
                    SeasonRank = table.Column<int>(type: "integer", nullable: false),
                    Wins = table.Column<int>(type: "integer", nullable: false),
                    Losses = table.Column<int>(type: "integer", nullable: false),
                    Draws = table.Column<int>(type: "integer", nullable: false),
                    MatchesPlayed = table.Column<int>(type: "integer", nullable: false),
                    CapturedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_season_rank_snapshot_rows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "season_reward_claims",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SeasonId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    RewardDay = table.Column<DateOnly>(type: "date", nullable: false),
                    AwardedCoins = table.Column<int>(type: "integer", nullable: false),
                    AwardedXp = table.Column<int>(type: "integer", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_season_reward_claims", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "seasons",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SeasonNumber = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ClosedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StartsAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndsAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seasons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "skill_nodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Branch = table.Column<int>(type: "integer", nullable: false),
                    Tier = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(600)", maxLength: 600, nullable: false),
                    PrereqKeysJson = table.Column<string>(type: "text", nullable: false),
                    CostsJson = table.Column<string>(type: "text", nullable: false),
                    EffectsJson = table.Column<string>(type: "text", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skill_nodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tiers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    MinScore = table.Column<int>(type: "integer", nullable: false),
                    MaxScore = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tiers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Handle = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Country = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    Tier = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Mmr = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastLoginAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "economy_transaction_lines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EconomyTransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Currency = table.Column<int>(type: "integer", nullable: false),
                    Delta = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_transaction_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_economy_transaction_lines_economy_transactions_EconomyTrans~",
                        column: x => x.EconomyTransactionId,
                        principalTable: "economy_transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "match_participant_results",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchResultId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlayerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    Correct = table.Column<int>(type: "integer", nullable: false),
                    Wrong = table.Column<int>(type: "integer", nullable: false),
                    AvgAnswerTimeMs = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_match_participant_results", x => x.Id);
                    table.ForeignKey(
                        name: "FK_match_participant_results_match_results_MatchResultId",
                        column: x => x.MatchResultId,
                        principalTable: "match_results",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MatchRounds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Index = table.Column<int>(type: "integer", nullable: false),
                    Correct = table.Column<bool>(type: "boolean", nullable: false),
                    AnswerTimeMs = table.Column<int>(type: "integer", nullable: false),
                    Points = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchRounds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchRounds_matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "question_options",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    OptionId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Text = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_question_options", x => x.Id);
                    table.ForeignKey(
                        name: "FK_question_options_questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "question_tags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Tag = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_question_tags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_question_tags_questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(@"""
                CREATE INDEX IF NOT EXISTS "IX_anti_cheat_flags_CreatedAtUtc"
                    ON anti_cheat_flags ("CreatedAtUtc");
                """);

            migrationBuilder.Sql(@"""
                CREATE INDEX IF NOT EXISTS "IX_anti_cheat_flags_MatchId"
                    ON anti_cheat_flags ("MatchId");
                """);

            migrationBuilder.Sql(@"""
                CREATE INDEX IF NOT EXISTS "IX_anti_cheat_flags_PlayerId"
                    ON anti_cheat_flags ("PlayerId");
                """);

            migrationBuilder.Sql(@"""
                CREATE INDEX IF NOT EXISTS "IX_anti_cheat_flags_ReviewedAtUtc"
                    ON anti_cheat_flags ("ReviewedAtUtc");
                """);

            migrationBuilder.Sql(@"""
                CREATE INDEX IF NOT EXISTS "IX_anti_cheat_flags_Severity_CreatedAtUtc"
                    ON anti_cheat_flags ("Severity", "CreatedAtUtc");
                """);

            migrationBuilder.Sql(@"""
                CREATE INDEX IF NOT EXISTS "IX_anti_cheat_flags_Severity_ReviewedAtUtc_CreatedAtUtc"
                    ON anti_cheat_flags ("Severity", "ReviewedAtUtc", "CreatedAtUtc");
                """);

            migrationBuilder.CreateIndex(
                name: "IX_economy_transaction_lines_EconomyTransactionId",
                table: "economy_transaction_lines",
                column: "EconomyTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_economy_transactions_EventId",
                table: "economy_transactions",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_transactions_PlayerId",
                table: "economy_transactions",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_economy_transactions_ReversalOfTransactionId",
                table: "economy_transactions",
                column: "ReversalOfTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_friend_edges_PlayerId",
                table: "friend_edges",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_friend_edges_PlayerId_FriendPlayerId",
                table: "friend_edges",
                columns: new[] { "PlayerId", "FriendPlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_friend_requests_FromPlayerId_ToPlayerId_Status",
                table: "friend_requests",
                columns: new[] { "FromPlayerId", "ToPlayerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_friend_requests_ToPlayerId_Status_CreatedAtUtc",
                table: "friend_requests",
                columns: new[] { "ToPlayerId", "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_match_participant_results_MatchResultId",
                table: "match_participant_results",
                column: "MatchResultId");

            migrationBuilder.CreateIndex(
                name: "IX_match_participant_results_MatchResultId_PlayerId",
                table: "match_participant_results",
                columns: new[] { "MatchResultId", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_match_results_EndedAtUtc",
                table: "match_results",
                column: "EndedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_match_results_MatchId",
                table: "match_results",
                column: "MatchId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_match_results_SubmitEventId",
                table: "match_results",
                column: "SubmitEventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_matches_HostPlayerId",
                table: "matches",
                column: "HostPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_matches_StartedAt",
                table: "matches",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_matchmaking_tickets_Mode_Tier_Scope_Status_CreatedAtUtc",
                table: "matchmaking_tickets",
                columns: new[] { "Mode", "Tier", "Scope", "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_matchmaking_tickets_PlayerId_Status",
                table: "matchmaking_tickets",
                columns: new[] { "PlayerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_matchmaking_tickets_Status_ExpiresAtUtc",
                table: "matchmaking_tickets",
                columns: new[] { "Status", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MatchRounds_MatchId",
                table: "MatchRounds",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_MissionClaims_PlayerId_MissionId",
                table: "MissionClaims",
                columns: new[] { "PlayerId", "MissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Missions_Type_Key",
                table: "Missions",
                columns: new[] { "Type", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_moderation_action_logs_CreatedAtUtc",
                table: "moderation_action_logs",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_moderation_action_logs_NewStatus_CreatedAtUtc",
                table: "moderation_action_logs",
                columns: new[] { "NewStatus", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_moderation_action_logs_PlayerId",
                table: "moderation_action_logs",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_moderation_action_logs_RelatedFlagId",
                table: "moderation_action_logs",
                column: "RelatedFlagId");

            migrationBuilder.CreateIndex(
                name: "IX_parties_LeaderPlayerId_Status",
                table: "parties",
                columns: new[] { "LeaderPlayerId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_parties_Status_CreatedAtUtc",
                table: "parties",
                columns: new[] { "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_party_invites_FromPlayerId_PartyId_Status",
                table: "party_invites",
                columns: new[] { "FromPlayerId", "PartyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_party_invites_PartyId_Status",
                table: "party_invites",
                columns: new[] { "PartyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_party_invites_ToPlayerId_Status_CreatedAtUtc",
                table: "party_invites",
                columns: new[] { "ToPlayerId", "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_party_match_links_MatchId_Status",
                table: "party_match_links",
                columns: new[] { "MatchId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_party_match_links_PartyId_MatchId",
                table: "party_match_links",
                columns: new[] { "PartyId", "MatchId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_party_match_members_MatchId",
                table: "party_match_members",
                column: "MatchId");

            migrationBuilder.CreateIndex(
                name: "IX_party_match_members_PartyId",
                table: "party_match_members",
                column: "PartyId");

            migrationBuilder.CreateIndex(
                name: "IX_party_matchmaking_tickets_PartyId_Status",
                table: "party_matchmaking_tickets",
                columns: new[] { "PartyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_party_matchmaking_tickets_Status_Mode_Scope_Tier_PartySize_~",
                table: "party_matchmaking_tickets",
                columns: new[] { "Status", "Mode", "Scope", "Tier", "PartySize", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_party_members_PartyId_PlayerId",
                table: "party_members",
                columns: new[] { "PartyId", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_party_members_PlayerId",
                table: "party_members",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_player_moderation_profiles_PlayerId",
                table: "player_moderation_profiles",
                column: "PlayerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_player_moderation_profiles_SetAtUtc",
                table: "player_moderation_profiles",
                column: "SetAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_player_moderation_profiles_Status",
                table: "player_moderation_profiles",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_player_powerups_PlayerId_Type",
                table: "player_powerups",
                columns: new[] { "PlayerId", "Type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_player_season_profiles_SeasonId_PlayerId",
                table: "player_season_profiles",
                columns: new[] { "SeasonId", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_player_season_profiles_SeasonId_RankPoints",
                table: "player_season_profiles",
                columns: new[] { "SeasonId", "RankPoints" });

            migrationBuilder.CreateIndex(
                name: "IX_player_season_profiles_SeasonId_SeasonRank",
                table: "player_season_profiles",
                columns: new[] { "SeasonId", "SeasonRank" });

            migrationBuilder.CreateIndex(
                name: "IX_player_season_profiles_SeasonId_Tier_TierRank",
                table: "player_season_profiles",
                columns: new[] { "SeasonId", "Tier", "TierRank" });

            migrationBuilder.CreateIndex(
                name: "IX_player_skill_unlocks_PlayerId",
                table: "player_skill_unlocks",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_player_skill_unlocks_PlayerId_NodeKey",
                table: "player_skill_unlocks",
                columns: new[] { "PlayerId", "NodeKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_player_wallets_PlayerId",
                table: "player_wallets",
                column: "PlayerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_players_Score",
                table: "players",
                column: "Score");

            migrationBuilder.CreateIndex(
                name: "IX_players_TierId",
                table: "players",
                column: "TierId");

            migrationBuilder.CreateIndex(
                name: "IX_players_Username",
                table: "players",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_processed_gameplay_events_EventId",
                table: "processed_gameplay_events",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_qr_scan_events_EventId",
                table: "qr_scan_events",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_qr_scan_events_PlayerId",
                table: "qr_scan_events",
                column: "PlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_qr_scan_events_PlayerId_Type_OccurredAtUtc",
                table: "qr_scan_events",
                columns: new[] { "PlayerId", "Type", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_question_options_QuestionId_OptionId",
                table: "question_options",
                columns: new[] { "QuestionId", "OptionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_question_tags_QuestionId_Tag",
                table: "question_tags",
                columns: new[] { "QuestionId", "Tag" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_question_tags_Tag",
                table: "question_tags",
                column: "Tag");

            migrationBuilder.CreateIndex(
                name: "IX_QuestionAnsweredAnalyticsEvents_PlayerId_QuestionId_Answere~",
                table: "QuestionAnsweredAnalyticsEvents",
                columns: new[] { "PlayerId", "QuestionId", "AnsweredAtUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_questions_Category",
                table: "questions",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_questions_Difficulty",
                table: "questions",
                column: "Difficulty");

            migrationBuilder.CreateIndex(
                name: "IX_questions_UpdatedAtUtc",
                table: "questions",
                column: "UpdatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_referral_codes_Code",
                table: "referral_codes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_referral_codes_OwnerPlayerId",
                table: "referral_codes",
                column: "OwnerPlayerId");

            migrationBuilder.CreateIndex(
                name: "IX_referral_redemptions_EventId",
                table: "referral_redemptions",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_referral_redemptions_OwnerPlayerId_RedeemerPlayerId",
                table: "referral_redemptions",
                columns: new[] { "OwnerPlayerId", "RedeemerPlayerId" });

            migrationBuilder.CreateIndex(
                name: "IX_referral_redemptions_ReferralCodeId",
                table: "referral_redemptions",
                column: "ReferralCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ExpiresAt",
                table: "RefreshTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId_DeviceId_IsRevoked",
                table: "RefreshTokens",
                columns: new[] { "UserId", "DeviceId", "IsRevoked" });

            migrationBuilder.CreateIndex(
                name: "IX_season_point_transactions_CreatedAtUtc",
                table: "season_point_transactions",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_season_point_transactions_EventId",
                table: "season_point_transactions",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_season_point_transactions_SeasonId_PlayerId",
                table: "season_point_transactions",
                columns: new[] { "SeasonId", "PlayerId" });

            migrationBuilder.CreateIndex(
                name: "IX_season_rank_snapshot_rows_SeasonId_PlayerId",
                table: "season_rank_snapshot_rows",
                columns: new[] { "SeasonId", "PlayerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_season_rank_snapshot_rows_SeasonId_SeasonRank",
                table: "season_rank_snapshot_rows",
                columns: new[] { "SeasonId", "SeasonRank" });

            migrationBuilder.CreateIndex(
                name: "IX_season_rank_snapshot_rows_SeasonId_Tier_TierRank",
                table: "season_rank_snapshot_rows",
                columns: new[] { "SeasonId", "Tier", "TierRank" });

            migrationBuilder.CreateIndex(
                name: "IX_season_reward_claims_EventId",
                table: "season_reward_claims",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_season_reward_claims_SeasonId_PlayerId_RewardDay",
                table: "season_reward_claims",
                columns: new[] { "SeasonId", "PlayerId", "RewardDay" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_seasons_EndsAtUtc",
                table: "seasons",
                column: "EndsAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_seasons_SeasonNumber",
                table: "seasons",
                column: "SeasonNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_seasons_StartsAtUtc",
                table: "seasons",
                column: "StartsAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_seasons_Status",
                table: "seasons",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_skill_nodes_Branch",
                table: "skill_nodes",
                column: "Branch");

            migrationBuilder.CreateIndex(
                name: "IX_skill_nodes_Key",
                table: "skill_nodes",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_skill_nodes_Tier",
                table: "skill_nodes",
                column: "Tier");

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedAt",
                table: "Users",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Handle",
                table: "Users",
                column: "Handle",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "anti_cheat_flags");

            migrationBuilder.DropTable(
                name: "economy_transaction_lines");

            migrationBuilder.DropTable(
                name: "friend_edges");

            migrationBuilder.DropTable(
                name: "friend_requests");

            migrationBuilder.DropTable(
                name: "LeaderboardEntries");

            migrationBuilder.DropTable(
                name: "match_participant_results");

            migrationBuilder.DropTable(
                name: "matchmaking_tickets");

            migrationBuilder.DropTable(
                name: "MatchRounds");

            migrationBuilder.DropTable(
                name: "MissionClaims");

            migrationBuilder.DropTable(
                name: "Missions");

            migrationBuilder.DropTable(
                name: "moderation_action_logs");

            migrationBuilder.DropTable(
                name: "parties");

            migrationBuilder.DropTable(
                name: "party_invites");

            migrationBuilder.DropTable(
                name: "party_match_links");

            migrationBuilder.DropTable(
                name: "party_match_members");

            migrationBuilder.DropTable(
                name: "party_matchmaking_tickets");

            migrationBuilder.DropTable(
                name: "party_members");

            migrationBuilder.DropTable(
                name: "player_moderation_profiles");

            migrationBuilder.DropTable(
                name: "player_powerups");

            migrationBuilder.DropTable(
                name: "player_season_profiles");

            migrationBuilder.DropTable(
                name: "player_skill_unlocks");

            migrationBuilder.DropTable(
                name: "player_wallets");

            migrationBuilder.DropTable(
                name: "players");

            migrationBuilder.DropTable(
                name: "processed_gameplay_events");

            migrationBuilder.DropTable(
                name: "qr_scan_events");

            migrationBuilder.DropTable(
                name: "question_options");

            migrationBuilder.DropTable(
                name: "question_tags");

            migrationBuilder.DropTable(
                name: "QuestionAnsweredAnalyticsEvents");

            migrationBuilder.DropTable(
                name: "QuestionAnsweredDailyRollups");

            migrationBuilder.DropTable(
                name: "QuestionAnsweredPlayerDailyRollups");

            migrationBuilder.DropTable(
                name: "referral_codes");

            migrationBuilder.DropTable(
                name: "referral_redemptions");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "season_point_transactions");

            migrationBuilder.DropTable(
                name: "season_rank_snapshot_rows");

            migrationBuilder.DropTable(
                name: "season_reward_claims");

            migrationBuilder.DropTable(
                name: "seasons");

            migrationBuilder.DropTable(
                name: "skill_nodes");

            migrationBuilder.DropTable(
                name: "Tiers");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "economy_transactions");

            migrationBuilder.DropTable(
                name: "match_results");

            migrationBuilder.DropTable(
                name: "matches");

            migrationBuilder.DropTable(
                name: "questions");
        }
    }
}

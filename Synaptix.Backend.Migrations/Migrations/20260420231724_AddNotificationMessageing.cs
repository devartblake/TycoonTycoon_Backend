using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synaptix.Backend.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationMessageing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "direct_message_conversations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_direct_message_conversations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "player_notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    body = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    unread = table.Column<bool>(type: "boolean", nullable: false),
                    action_route = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    payload_json = table.Column<string>(type: "text", nullable: false),
                    icon = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    avatar_url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    read_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_player_notifications", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "direct_message_conversation_participants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    joined_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_read_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_read_message_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_direct_message_conversation_participants", x => x.id);
                    table.ForeignKey(
                        name: "fk_direct_message_conversation_participants_direct_message_con",
                        column: x => x.conversation_id,
                        principalTable: "direct_message_conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "direct_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sender_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    client_message_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_direct_messages", x => x.id);
                    table.ForeignKey(
                        name: "fk_direct_messages_direct_message_conversations_conversation_id",
                        column: x => x.conversation_id,
                        principalTable: "direct_message_conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_direct_message_conversation_participants_conversation_id_pl",
                table: "direct_message_conversation_participants",
                columns: new[] { "conversation_id", "player_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_direct_message_conversation_participants_player_id_last_rea",
                table: "direct_message_conversation_participants",
                columns: new[] { "player_id", "last_read_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_direct_message_conversations_type_updated_at_utc",
                table: "direct_message_conversations",
                columns: new[] { "type", "updated_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_direct_messages_conversation_id_created_at_utc",
                table: "direct_messages",
                columns: new[] { "conversation_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_direct_messages_conversation_id_sender_id_client_message_id",
                table: "direct_messages",
                columns: new[] { "conversation_id", "sender_id", "client_message_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_player_notifications_player_id_created_at_utc",
                table: "player_notifications",
                columns: new[] { "player_id", "created_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_player_notifications_player_id_unread_created_at_utc",
                table: "player_notifications",
                columns: new[] { "player_id", "unread", "created_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "direct_message_conversation_participants");

            migrationBuilder.DropTable(
                name: "direct_messages");

            migrationBuilder.DropTable(
                name: "player_notifications");

            migrationBuilder.DropTable(
                name: "direct_message_conversations");
        }
    }
}

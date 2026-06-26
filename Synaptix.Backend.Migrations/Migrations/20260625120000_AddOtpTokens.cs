using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Synaptix.Backend.Migrations.Migrations
{
    /// <inheritdoc />
    [Migration("20260625120000_AddOtpTokens")]
    public partial class AddOtpTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "otp_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    otp_hash = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_used = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    verification_attempts = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_otp_tokens", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_otp_tokens_email",
                table: "otp_tokens",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "ix_otp_tokens_expires_at",
                table: "otp_tokens",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_otp_tokens_email_expires_at",
                table: "otp_tokens",
                columns: new[] { "email", "expires_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "otp_tokens");
        }
    }
}

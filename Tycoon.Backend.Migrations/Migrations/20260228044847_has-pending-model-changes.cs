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
            migrationBuilder.AddColumn<string>(
                name: "ClientType",
                table: "RefreshTokens",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LastError",
                table: "admin_notification_schedules",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxRetries",
                table: "admin_notification_schedules",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ProcessedAtUtc",
                table: "admin_notification_schedules",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                table: "admin_notification_schedules",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId_ClientType_IsRevoked",
                table: "RefreshTokens",
                columns: new[] { "UserId", "ClientType", "IsRevoked" });

            migrationBuilder.CreateIndex(
                name: "IX_admin_notification_schedules_Status",
                table: "admin_notification_schedules",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_UserId_ClientType_IsRevoked",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_admin_notification_schedules_Status",
                table: "admin_notification_schedules");

            migrationBuilder.DropColumn(
                name: "ClientType",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "LastError",
                table: "admin_notification_schedules");

            migrationBuilder.DropColumn(
                name: "MaxRetries",
                table: "admin_notification_schedules");

            migrationBuilder.DropColumn(
                name: "ProcessedAtUtc",
                table: "admin_notification_schedules");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                table: "admin_notification_schedules");
        }
    }
}

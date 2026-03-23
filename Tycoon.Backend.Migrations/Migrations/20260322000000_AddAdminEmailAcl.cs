using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tycoon.Backend.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminEmailAcl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminEmailAcls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ListType = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Role = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AddedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminEmailAcls", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminEmailAcls_ListType",
                table: "AdminEmailAcls",
                column: "ListType");

            migrationBuilder.CreateIndex(
                name: "IX_AdminEmailAcls_NormalizedEmail",
                table: "AdminEmailAcls",
                column: "NormalizedEmail",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AdminEmailAcls");
        }
    }
}

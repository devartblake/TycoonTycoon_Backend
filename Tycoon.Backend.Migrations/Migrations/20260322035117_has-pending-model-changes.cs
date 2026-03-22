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
            migrationBuilder.DropPrimaryKey(
                name: "PK_admin_email_acls",
                table: "admin_email_acls");

            migrationBuilder.RenameTable(
                name: "admin_email_acls",
                newName: "AdminEmailAcls");

            migrationBuilder.RenameIndex(
                name: "IX_admin_email_acls_NormalizedEmail",
                table: "AdminEmailAcls",
                newName: "IX_AdminEmailAcls_NormalizedEmail");

            migrationBuilder.RenameIndex(
                name: "IX_admin_email_acls_ListType",
                table: "AdminEmailAcls",
                newName: "IX_AdminEmailAcls_ListType");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AdminEmailAcls",
                table: "AdminEmailAcls",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_AdminEmailAcls",
                table: "AdminEmailAcls");

            migrationBuilder.RenameTable(
                name: "AdminEmailAcls",
                newName: "admin_email_acls");

            migrationBuilder.RenameIndex(
                name: "IX_AdminEmailAcls_NormalizedEmail",
                table: "admin_email_acls",
                newName: "IX_admin_email_acls_NormalizedEmail");

            migrationBuilder.RenameIndex(
                name: "IX_AdminEmailAcls_ListType",
                table: "admin_email_acls",
                newName: "IX_admin_email_acls_ListType");

            migrationBuilder.AddPrimaryKey(
                name: "PK_admin_email_acls",
                table: "admin_email_acls",
                column: "Id");
        }
    }
}

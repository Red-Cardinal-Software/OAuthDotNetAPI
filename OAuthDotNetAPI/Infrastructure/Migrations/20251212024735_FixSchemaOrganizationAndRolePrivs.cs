using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixSchemaOrganizationAndRolePrivs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Privileges_Roles_RoleId",
                table: "Privileges");

            migrationBuilder.DropIndex(
                name: "IX_Privileges_RoleId",
                table: "Privileges");

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "Privileges");

            migrationBuilder.EnsureSchema(
                name: "Identity");

            migrationBuilder.EnsureSchema(
                name: "Configuration");

            migrationBuilder.RenameTable(
                name: "Roles",
                newName: "Roles",
                newSchema: "Identity");

            migrationBuilder.RenameTable(
                name: "RefreshTokens",
                newName: "RefreshTokens",
                newSchema: "Identity");

            migrationBuilder.RenameTable(
                name: "Privileges",
                newName: "Privileges",
                newSchema: "Identity");

            migrationBuilder.RenameTable(
                name: "PasswordResetTokens",
                newName: "PasswordResetTokens",
                newSchema: "Identity");

            migrationBuilder.RenameTable(
                name: "Organizations",
                newName: "Organizations",
                newSchema: "Identity");

            migrationBuilder.RenameTable(
                name: "LoginAttempts",
                newName: "LoginAttempts",
                newSchema: "Security");

            migrationBuilder.RenameTable(
                name: "EmailTemplates",
                newName: "EmailTemplates",
                newSchema: "Configuration");

            migrationBuilder.RenameTable(
                name: "BlacklistedPasswords",
                newName: "BlacklistedPasswords",
                newSchema: "Security");

            migrationBuilder.RenameTable(
                name: "AppUsers",
                newName: "AppUsers",
                newSchema: "Identity");

            migrationBuilder.RenameTable(
                name: "AppUserRole",
                newName: "AppUserRole",
                newSchema: "Identity");

            migrationBuilder.RenameTable(
                name: "AccountLockouts",
                newName: "AccountLockouts",
                newSchema: "Security");

            migrationBuilder.CreateTable(
                name: "RolePrivileges",
                schema: "Identity",
                columns: table => new
                {
                    PrivilegesId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePrivileges", x => new { x.PrivilegesId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_RolePrivileges_Privileges_PrivilegesId",
                        column: x => x.PrivilegesId,
                        principalSchema: "Identity",
                        principalTable: "Privileges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePrivileges_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "Identity",
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RolePrivileges_RoleId",
                schema: "Identity",
                table: "RolePrivileges",
                column: "RoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RolePrivileges",
                schema: "Identity");

            migrationBuilder.RenameTable(
                name: "Roles",
                schema: "Identity",
                newName: "Roles");

            migrationBuilder.RenameTable(
                name: "RefreshTokens",
                schema: "Identity",
                newName: "RefreshTokens");

            migrationBuilder.RenameTable(
                name: "Privileges",
                schema: "Identity",
                newName: "Privileges");

            migrationBuilder.RenameTable(
                name: "PasswordResetTokens",
                schema: "Identity",
                newName: "PasswordResetTokens");

            migrationBuilder.RenameTable(
                name: "Organizations",
                schema: "Identity",
                newName: "Organizations");

            migrationBuilder.RenameTable(
                name: "LoginAttempts",
                schema: "Security",
                newName: "LoginAttempts");

            migrationBuilder.RenameTable(
                name: "EmailTemplates",
                schema: "Configuration",
                newName: "EmailTemplates");

            migrationBuilder.RenameTable(
                name: "BlacklistedPasswords",
                schema: "Security",
                newName: "BlacklistedPasswords");

            migrationBuilder.RenameTable(
                name: "AppUsers",
                schema: "Identity",
                newName: "AppUsers");

            migrationBuilder.RenameTable(
                name: "AppUserRole",
                schema: "Identity",
                newName: "AppUserRole");

            migrationBuilder.RenameTable(
                name: "AccountLockouts",
                schema: "Security",
                newName: "AccountLockouts");

            migrationBuilder.AddColumn<Guid>(
                name: "RoleId",
                table: "Privileges",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Privileges_RoleId",
                table: "Privileges",
                column: "RoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Privileges_Roles_RoleId",
                table: "Privileges",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id");
        }
    }
}

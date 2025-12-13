using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountLockout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountLockouts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FailedAttemptCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsLockedOut = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    LockedOutAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastFailedAttemptAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    LockoutReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    LockedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountLockouts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoginAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttemptedUsername = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsSuccessful = table.Column<bool>(type: "bit", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AttemptedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Metadata = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginAttempts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountLockouts_IsLockedOut_LockoutExpiresAt",
                table: "AccountLockouts",
                columns: new[] { "IsLockedOut", "LockoutExpiresAt" },
                filter: "IsLockedOut = 1");

            migrationBuilder.CreateIndex(
                name: "IX_AccountLockouts_LastFailedAttemptAt",
                table: "AccountLockouts",
                column: "LastFailedAttemptAt");

            migrationBuilder.CreateIndex(
                name: "IX_AccountLockouts_LockedByUserId",
                table: "AccountLockouts",
                column: "LockedByUserId",
                filter: "LockedByUserId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AccountLockouts_LockoutExpiresAt",
                table: "AccountLockouts",
                column: "LockoutExpiresAt",
                filter: "LockoutExpiresAt IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_AccountLockouts_UserId",
                table: "AccountLockouts",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_AttemptedAt",
                table: "LoginAttempts",
                column: "AttemptedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_AttemptedUsername",
                table: "LoginAttempts",
                column: "AttemptedUsername");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_IpAddress",
                table: "LoginAttempts",
                column: "IpAddress",
                filter: "IpAddress IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_IpAddress_IsSuccessful_AttemptedAt",
                table: "LoginAttempts",
                columns: new[] { "IpAddress", "IsSuccessful", "AttemptedAt" },
                filter: "IpAddress IS NOT NULL AND IsSuccessful = 0");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_UserId",
                table: "LoginAttempts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_UserId_IsSuccessful_AttemptedAt",
                table: "LoginAttempts",
                columns: new[] { "UserId", "IsSuccessful", "AttemptedAt" },
                filter: "IsSuccessful = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountLockouts");

            migrationBuilder.DropTable(
                name: "LoginAttempts");
        }
    }
}

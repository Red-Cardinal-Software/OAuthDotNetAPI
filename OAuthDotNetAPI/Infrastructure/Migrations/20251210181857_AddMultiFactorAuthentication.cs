using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiFactorAuthentication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Security");

            migrationBuilder.CreateTable(
                name: "MfaMethods",
                schema: "Security",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Secret = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    VerifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MfaMethods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MfaMethods_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MfaChallenges",
                schema: "Security",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChallengeToken = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    MfaMethodId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsInvalid = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Metadata = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastAttemptAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MfaChallenges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MfaChallenges_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MfaChallenges_MfaMethods_MfaMethodId",
                        column: x => x.MfaMethodId,
                        principalSchema: "Security",
                        principalTable: "MfaMethods",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MfaRecoveryCodes",
                schema: "Security",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MfaMethodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HashedCode = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UsedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MfaRecoveryCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MfaRecoveryCodes_MfaMethods_MfaMethodId",
                        column: x => x.MfaMethodId,
                        principalSchema: "Security",
                        principalTable: "MfaMethods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WebAuthnCredentials",
                schema: "Security",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MfaMethodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CredentialId = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    PublicKey = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    SignCount = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    AuthenticatorType = table.Column<int>(type: "int", nullable: false),
                    Transports = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SupportsUserVerification = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AttestationType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Aaguid = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUsedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    RegistrationIpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    RegistrationUserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebAuthnCredentials", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WebAuthnCredentials_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WebAuthnCredentials_MfaMethods_MfaMethodId",
                        column: x => x.MfaMethodId,
                        principalSchema: "Security",
                        principalTable: "MfaMethods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MfaEmailCodes",
                schema: "Security",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MfaChallengeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmailAddress = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    HashedCode = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UsedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AttemptCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MfaEmailCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MfaEmailCodes_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MfaEmailCodes_MfaChallenges_MfaChallengeId",
                        column: x => x.MfaChallengeId,
                        principalSchema: "Security",
                        principalTable: "MfaChallenges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MfaChallenges_ChallengeToken",
                schema: "Security",
                table: "MfaChallenges",
                column: "ChallengeToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MfaChallenges_CreatedAt",
                schema: "Security",
                table: "MfaChallenges",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MfaChallenges_ExpiresAt",
                schema: "Security",
                table: "MfaChallenges",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_MfaChallenges_MfaMethodId",
                schema: "Security",
                table: "MfaChallenges",
                column: "MfaMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_MfaChallenges_UserId",
                schema: "Security",
                table: "MfaChallenges",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MfaChallenges_UserId_IsCompleted",
                schema: "Security",
                table: "MfaChallenges",
                columns: new[] { "UserId", "IsCompleted" });

            migrationBuilder.CreateIndex(
                name: "IX_MfaEmailCodes_ChallengeId",
                schema: "Security",
                table: "MfaEmailCodes",
                column: "MfaChallengeId");

            migrationBuilder.CreateIndex(
                name: "IX_MfaEmailCodes_ExpiresAt",
                schema: "Security",
                table: "MfaEmailCodes",
                column: "ExpiresAt",
                filter: "[IsUsed] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_MfaEmailCodes_User_Status",
                schema: "Security",
                table: "MfaEmailCodes",
                columns: new[] { "UserId", "IsUsed", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MfaEmailCodes_UserId",
                schema: "Security",
                table: "MfaEmailCodes",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MfaMethods_UserId",
                schema: "Security",
                table: "MfaMethods",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MfaMethods_UserId_IsDefault",
                schema: "Security",
                table: "MfaMethods",
                columns: new[] { "UserId", "IsDefault" },
                filter: "[IsDefault] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_MfaMethods_UserId_IsEnabled",
                schema: "Security",
                table: "MfaMethods",
                columns: new[] { "UserId", "IsEnabled" });

            migrationBuilder.CreateIndex(
                name: "IX_MfaMethods_UserId_Type",
                schema: "Security",
                table: "MfaMethods",
                columns: new[] { "UserId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_MfaRecoveryCodes_HashedCode",
                schema: "Security",
                table: "MfaRecoveryCodes",
                column: "HashedCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MfaRecoveryCodes_MfaMethodId",
                schema: "Security",
                table: "MfaRecoveryCodes",
                column: "MfaMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_MfaRecoveryCodes_MfaMethodId_IsUsed",
                schema: "Security",
                table: "MfaRecoveryCodes",
                columns: new[] { "MfaMethodId", "IsUsed" });

            migrationBuilder.CreateIndex(
                name: "IX_WebAuthnCredentials_CredentialId",
                schema: "Security",
                table: "WebAuthnCredentials",
                column: "CredentialId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WebAuthnCredentials_LastUsed",
                schema: "Security",
                table: "WebAuthnCredentials",
                column: "LastUsedAt",
                filter: "[LastUsedAt] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WebAuthnCredentials_MfaMethodId",
                schema: "Security",
                table: "WebAuthnCredentials",
                column: "MfaMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_WebAuthnCredentials_User_Active",
                schema: "Security",
                table: "WebAuthnCredentials",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_WebAuthnCredentials_UserId",
                schema: "Security",
                table: "WebAuthnCredentials",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MfaEmailCodes",
                schema: "Security");

            migrationBuilder.DropTable(
                name: "MfaRecoveryCodes",
                schema: "Security");

            migrationBuilder.DropTable(
                name: "WebAuthnCredentials",
                schema: "Security");

            migrationBuilder.DropTable(
                name: "MfaChallenges",
                schema: "Security");

            migrationBuilder.DropTable(
                name: "MfaMethods",
                schema: "Security");
        }
    }
}

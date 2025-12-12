using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingMFATables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MfaPushDevices",
                schema: "Security",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MfaMethodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DeviceName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Platform = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PushToken = table.Column<string>(type: "nvarchar(4096)", maxLength: 4096, nullable: false),
                    PublicKey = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    TrustScore = table.Column<int>(type: "int", nullable: false, defaultValue: 50)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MfaPushDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MfaPushDevices_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MfaPushDevices_MfaMethods_MfaMethodId",
                        column: x => x.MfaMethodId,
                        principalSchema: "Security",
                        principalTable: "MfaMethods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MfaPushChallenges",
                schema: "Security",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChallengeCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SessionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Response = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    ResponseSignature = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    ContextData = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MfaPushChallenges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MfaPushChallenges_AppUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AppUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MfaPushChallenges_MfaPushDevices_DeviceId",
                        column: x => x.DeviceId,
                        principalSchema: "Security",
                        principalTable: "MfaPushDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MfaPushChallenges_ChallengeCode",
                schema: "Security",
                table: "MfaPushChallenges",
                column: "ChallengeCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MfaPushChallenges_CreatedAt",
                schema: "Security",
                table: "MfaPushChallenges",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_MfaPushChallenges_DeviceId",
                schema: "Security",
                table: "MfaPushChallenges",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_MfaPushChallenges_ExpiresAt",
                schema: "Security",
                table: "MfaPushChallenges",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_MfaPushChallenges_SessionId",
                schema: "Security",
                table: "MfaPushChallenges",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_MfaPushChallenges_UserId",
                schema: "Security",
                table: "MfaPushChallenges",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MfaPushChallenges_UserId_Status",
                schema: "Security",
                table: "MfaPushChallenges",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_MfaPushDevices_MfaMethodId",
                schema: "Security",
                table: "MfaPushDevices",
                column: "MfaMethodId");

            migrationBuilder.CreateIndex(
                name: "IX_MfaPushDevices_PushToken",
                schema: "Security",
                table: "MfaPushDevices",
                column: "PushToken");

            migrationBuilder.CreateIndex(
                name: "IX_MfaPushDevices_UserId",
                schema: "Security",
                table: "MfaPushDevices",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_MfaPushDevices_UserId_DeviceId",
                schema: "Security",
                table: "MfaPushDevices",
                columns: new[] { "UserId", "DeviceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MfaPushDevices_UserId_IsActive",
                schema: "Security",
                table: "MfaPushDevices",
                columns: new[] { "UserId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MfaPushChallenges",
                schema: "Security");

            migrationBuilder.DropTable(
                name: "MfaPushDevices",
                schema: "Security");
        }
    }
}

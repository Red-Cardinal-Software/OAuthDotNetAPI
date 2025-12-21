using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Audit");

            migrationBuilder.CreateTable(
                name: "AuditArchiveManifest",
                schema: "Audit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PartitionBoundary = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FirstSequenceNumber = table.Column<long>(type: "bigint", nullable: false),
                    LastSequenceNumber = table.Column<long>(type: "bigint", nullable: false),
                    RecordCount = table.Column<long>(type: "bigint", nullable: false),
                    FirstRecordHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    LastRecordHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    LedgerDigest = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ArchiveUri = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    ArchiveBlobHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ArchiveSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ArchivedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ArchivedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PurgedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetentionPolicy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditArchiveManifest", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLedger",
                schema: "Audit",
                columns: table => new
                {
                    SequenceNumber = table.Column<long>(type: "bigint", nullable: false),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PreviousHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Hash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Username = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    EntityId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdditionalData = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    Dispatched = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    DispatchedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLedger", x => x.SequenceNumber);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditArchiveManifest_PartitionBoundary",
                schema: "Audit",
                table: "AuditArchiveManifest",
                column: "PartitionBoundary",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLedger_Dispatched",
                schema: "Audit",
                table: "AuditLedger",
                column: "Dispatched",
                filter: "[Dispatched] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLedger_EntityType_EntityId",
                schema: "Audit",
                table: "AuditLedger",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLedger_EventId",
                schema: "Audit",
                table: "AuditLedger",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLedger_UserId_OccurredAt",
                schema: "Audit",
                table: "AuditLedger",
                columns: new[] { "UserId", "OccurredAt" });

            // ============================================================================
            // SQL Server 2022+ Ledger and Partitioning Setup
            // ============================================================================

            // Create partition function for monthly boundaries (sliding window)
            // Generates boundaries dynamically: 12 months back + 24 months forward from migration date
            // Background service (AuditArchiveBackgroundService) adds new partitions automatically
            var partitionBoundaries = GeneratePartitionBoundaries(
                monthsBack: 12,
                monthsForward: 24);

            migrationBuilder.Sql($@"
                CREATE PARTITION FUNCTION [AuditLedger_PF] (datetime2)
                AS RANGE RIGHT FOR VALUES (
                    {partitionBoundaries}
                );
            ");

            // Create partition scheme (all partitions on PRIMARY filegroup for simplicity)
            // Production: Consider separate filegroups per partition for easier management
            migrationBuilder.Sql(@"
                CREATE PARTITION SCHEME [AuditLedger_PS]
                AS PARTITION [AuditLedger_PF]
                ALL TO ([PRIMARY]);
            ");

            // Drop and recreate AuditLedger as partitioned table with ledger enabled
            // We must recreate because you cannot add partitioning to an existing table
            migrationBuilder.Sql(@"
                -- Drop the EF-created table
                DROP TABLE [Audit].[AuditLedger];

                -- Recreate as partitioned ledger table
                CREATE TABLE [Audit].[AuditLedger] (
                    [SequenceNumber] bigint NOT NULL,
                    [EventId] uniqueidentifier NOT NULL,
                    [OccurredAt] datetime2 NOT NULL,
                    [PreviousHash] nvarchar(64) NOT NULL,
                    [Hash] nvarchar(64) NOT NULL,
                    [UserId] uniqueidentifier NULL,
                    [Username] nvarchar(256) NULL,
                    [IpAddress] nvarchar(45) NULL,
                    [UserAgent] nvarchar(512) NULL,
                    [CorrelationId] nvarchar(128) NULL,
                    [EventType] int NOT NULL,
                    [Action] int NOT NULL,
                    [EntityType] nvarchar(128) NULL,
                    [EntityId] nvarchar(128) NULL,
                    [OldValues] nvarchar(max) NULL,
                    [NewValues] nvarchar(max) NULL,
                    [AdditionalData] nvarchar(max) NULL,
                    [Success] bit NOT NULL,
                    [FailureReason] nvarchar(1024) NULL,
                    [Dispatched] bit NOT NULL DEFAULT 0,
                    [DispatchedAt] datetime2 NULL,
                    CONSTRAINT [PK_AuditLedger] PRIMARY KEY CLUSTERED ([SequenceNumber], [OccurredAt])
                ) ON [AuditLedger_PS]([OccurredAt])
                WITH (LEDGER = ON (APPEND_ONLY = ON));

                -- Recreate indexes on partitioned table
                -- Note: Unique indexes on partitioned tables must include the partition column
                CREATE UNIQUE INDEX [IX_AuditLedger_EventId] ON [Audit].[AuditLedger] ([EventId], [OccurredAt]);
                CREATE INDEX [IX_AuditLedger_UserId_OccurredAt] ON [Audit].[AuditLedger] ([UserId], [OccurredAt]);
                CREATE INDEX [IX_AuditLedger_EntityType_EntityId] ON [Audit].[AuditLedger] ([EntityType], [EntityId]);
                CREATE INDEX [IX_AuditLedger_Dispatched] ON [Audit].[AuditLedger] ([Dispatched]) WHERE [Dispatched] = 0;
            ");

            // Create staging table for partition switching (same schema, NOT a ledger table)
            // Must match structure exactly for SWITCH to work
            migrationBuilder.Sql(@"
                CREATE TABLE [Audit].[AuditLedger_Staging] (
                    [SequenceNumber] bigint NOT NULL,
                    [EventId] uniqueidentifier NOT NULL,
                    [OccurredAt] datetime2 NOT NULL,
                    [PreviousHash] nvarchar(64) NOT NULL,
                    [Hash] nvarchar(64) NOT NULL,
                    [UserId] uniqueidentifier NULL,
                    [Username] nvarchar(256) NULL,
                    [IpAddress] nvarchar(45) NULL,
                    [UserAgent] nvarchar(512) NULL,
                    [CorrelationId] nvarchar(128) NULL,
                    [EventType] int NOT NULL,
                    [Action] int NOT NULL,
                    [EntityType] nvarchar(128) NULL,
                    [EntityId] nvarchar(128) NULL,
                    [OldValues] nvarchar(max) NULL,
                    [NewValues] nvarchar(max) NULL,
                    [AdditionalData] nvarchar(max) NULL,
                    [Success] bit NOT NULL,
                    [FailureReason] nvarchar(1024) NULL,
                    [Dispatched] bit NOT NULL DEFAULT 0,
                    [DispatchedAt] datetime2 NULL,
                    CONSTRAINT [PK_AuditLedger_Staging] PRIMARY KEY CLUSTERED ([SequenceNumber], [OccurredAt])
                ) ON [AuditLedger_PS]([OccurredAt]);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditArchiveManifest",
                schema: "Audit");

            // Drop staging table first
            migrationBuilder.Sql("DROP TABLE IF EXISTS [Audit].[AuditLedger_Staging];");

            // Drop ledger table (note: ledger history tables are dropped automatically)
            migrationBuilder.DropTable(
                name: "AuditLedger",
                schema: "Audit");

            // Drop partition scheme and function
            migrationBuilder.Sql("DROP PARTITION SCHEME [AuditLedger_PS];");
            migrationBuilder.Sql("DROP PARTITION FUNCTION [AuditLedger_PF];");
        }

        /// <summary>
        /// Generates partition boundary dates dynamically based on current date.
        /// Creates monthly boundaries from (now - monthsBack) to (now + monthsForward).
        /// </summary>
        private static string GeneratePartitionBoundaries(int monthsBack, int monthsForward)
        {
            var boundaries = new List<string>();
            var now = DateTime.UtcNow;

            // Start from the first of the month, N months back
            var startMonth = new DateTime(now.Year, now.Month, 1).AddMonths(-monthsBack);

            // Generate boundaries for the range
            var totalMonths = monthsBack + monthsForward;
            for (var i = 0; i <= totalMonths; i++)
            {
                var boundaryDate = startMonth.AddMonths(i);
                boundaries.Add($"'{boundaryDate:yyyy-MM-dd}'");
            }

            return string.Join(", ", boundaries);
        }
    }
}

using Domain.Entities.Audit;
using Infrastructure.Persistence.EntityConfigurations.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations;

/// <summary>
/// EF Core configuration for the AuditLedgerEntry entity.
/// Configures as a SQL Server 2022+ Ledger table for cryptographic tamper-evidence.
///
/// IMPORTANT: The migration must be manually modified to add:
///
/// 1. LEDGER TABLE - Enable append-only ledger for tamper-evidence:
///    migrationBuilder.Sql("ALTER TABLE [Audit].[AuditLedger] SET (LEDGER = ON (APPEND_ONLY = ON));");
///
/// 2. PARTITIONING - Enable monthly partitions for archive workflow:
///    - Create partition function on OccurredAt (monthly boundaries)
///    - Create partition scheme mapping to filegroups
///    - Recreate table with partition scheme (or use raw SQL CREATE TABLE)
///
/// 3. STAGING TABLE - For partition switch operations:
///    CREATE TABLE [Audit].[AuditLedger_Staging] (same schema, no ledger, same partition scheme)
///
/// Archive workflow: SWITCH partition to staging → Export to blob → Get ledger digest → TRUNCATE staging
/// See AuditArchiveManifest for tracking archived partitions.
/// </summary>
internal class AuditLedgerEntryConfiguration : EntityTypeConfiguration<AuditLedgerEntry>
{
    protected override void PerformConfiguration(EntityTypeBuilder<AuditLedgerEntry> builder)
    {
        // SQL Server 2022+ Ledger table (append-only with cryptographic verification)
        // Note: LEDGER = ON must be set in the migration using raw SQL
        builder.ToTable("AuditLedger", "Audit");

        // Primary key is the sequence number (append-only, no updates)
        builder.HasKey(e => e.SequenceNumber);
        builder.Property(e => e.SequenceNumber)
            .ValueGeneratedNever(); // We manage sequence numbers ourselves

        // Event identification
        builder.Property(e => e.EventId)
            .IsRequired();
        builder.HasIndex(e => e.EventId)
            .IsUnique();

        builder.Property(e => e.OccurredAt)
            .IsRequired();

        // Hash chain
        builder.Property(e => e.PreviousHash)
            .IsRequired()
            .HasMaxLength(64); // SHA-256 hex string

        builder.Property(e => e.Hash)
            .IsRequired()
            .HasMaxLength(64);

        // Who - composite index for user activity queries
        builder.Property(e => e.UserId);
        builder.HasIndex(e => new { e.UserId, e.OccurredAt });

        builder.Property(e => e.Username)
            .HasMaxLength(256);

        builder.Property(e => e.IpAddress)
            .HasMaxLength(45); // IPv6 max length

        builder.Property(e => e.UserAgent)
            .HasMaxLength(512);

        builder.Property(e => e.CorrelationId)
            .HasMaxLength(128);

        // What - composite index for entity history queries
        builder.Property(e => e.EventType)
            .IsRequired();

        builder.Property(e => e.Action)
            .IsRequired();

        builder.Property(e => e.EntityType)
            .HasMaxLength(128);
        builder.HasIndex(e => new { e.EntityType, e.EntityId });

        builder.Property(e => e.EntityId)
            .HasMaxLength(128);

        // JSON data columns - database-specific types
//#if (UsePostgreSql)
        builder.Property(e => e.OldValues)
            .HasColumnType("jsonb");
        builder.Property(e => e.NewValues)
            .HasColumnType("jsonb");
        builder.Property(e => e.AdditionalData)
            .HasColumnType("jsonb");
//#elseif (UseOracle)
        builder.Property(e => e.OldValues)
            .HasColumnType("CLOB");
        builder.Property(e => e.NewValues)
            .HasColumnType("CLOB");
        builder.Property(e => e.AdditionalData)
            .HasColumnType("CLOB");
//#else
        builder.Property(e => e.OldValues)
            .HasColumnType("nvarchar(max)");
        builder.Property(e => e.NewValues)
            .HasColumnType("nvarchar(max)");
        builder.Property(e => e.AdditionalData)
            .HasColumnType("nvarchar(max)");
//#endif

        // Outcome
        builder.Property(e => e.Success)
            .IsRequired();

        builder.Property(e => e.FailureReason)
            .HasMaxLength(1024);

        // Outbox - filtered index for undispatched only
        builder.Property(e => e.Dispatched)
            .IsRequired()
            .HasDefaultValue(false);
//#if (UsePostgreSql)
        builder.HasIndex(e => e.Dispatched)
            .HasFilter("\"Dispatched\" = false");
//#elseif (UseOracle)
        // Oracle doesn't support filtered indexes directly; use function-based index via migration
        builder.HasIndex(e => e.Dispatched);
//#else
        builder.HasIndex(e => e.Dispatched)
            .HasFilter("[Dispatched] = 0");
//#endif

        builder.Property(e => e.DispatchedAt);
    }
}
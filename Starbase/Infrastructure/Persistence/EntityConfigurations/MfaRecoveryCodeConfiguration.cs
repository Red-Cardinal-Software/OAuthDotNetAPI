using Domain.Entities.Security;
using Infrastructure.Persistence.EntityConfigurations.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations;

/// <summary>
/// Entity Framework Core configuration for the MfaRecoveryCode entity.
/// </summary>
internal class MfaRecoveryCodeConfiguration : EntityTypeConfiguration<MfaRecoveryCode>
{
    protected override void PerformConfiguration(EntityTypeBuilder<MfaRecoveryCode> builder)
    {
        // Table configuration
        builder.ToTable("MfaRecoveryCodes", "Security");

        // Primary key
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .ValueGeneratedNever(); // GUID generated client-side

        // Properties
        builder.Property(r => r.MfaMethodId)
            .IsRequired();

        builder.Property(r => r.HashedCode)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(r => r.IsUsed)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        builder.Property(r => r.UsedAt)
            .IsRequired(false);

        // Ignore the plain text Code property (not persisted)
        builder.Ignore(r => r.Code);

        // Indexes
        builder.HasIndex(r => r.MfaMethodId)
            .HasDatabaseName("IX_MfaRecoveryCodes_MfaMethodId");

        builder.HasIndex(r => new { r.MfaMethodId, r.IsUsed })
            .HasDatabaseName("IX_MfaRecoveryCodes_MfaMethodId_IsUsed");

        builder.HasIndex(r => r.HashedCode)
            .HasDatabaseName("IX_MfaRecoveryCodes_HashedCode")
            .IsUnique();

        // Relationships
        builder.HasOne(r => r.MfaMethod)
            .WithMany(m => m.RecoveryCodes)
            .HasForeignKey(r => r.MfaMethodId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

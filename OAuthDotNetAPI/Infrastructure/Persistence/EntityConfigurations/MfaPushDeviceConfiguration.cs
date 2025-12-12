using Domain.Entities.Security;
using Infrastructure.Persistence.EntityConfigurations.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations;

internal class MfaPushDeviceConfiguration : EntityTypeConfiguration<MfaPushDevice>
{
    protected override void PerformConfiguration(EntityTypeBuilder<MfaPushDevice> builder)
    {
        // Table configuration
        builder.ToTable("MfaPushDevices", "Security");

        // Primary Key
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .ValueGeneratedNever();

        // Properties
        builder.Property(p => p.UserId)
            .IsRequired();

        builder.Property(p => p.MfaMethodId)
            .IsRequired();

        builder.Property(p => p.DeviceId)
            .IsRequired()
            .HasMaxLength(256); // Device identifiers can be quite long

        builder.Property(p => p.DeviceName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Platform)
            .IsRequired()
            .HasMaxLength(50); // iOS, Android, etc.

        builder.Property(p => p.PushToken)
            .IsRequired()
            .HasMaxLength(4096); // Push tokens can be very long

        builder.Property(p => p.PublicKey)
            .IsRequired()
            .HasMaxLength(2048); // Public keys can be large

        builder.Property(p => p.RegisteredAt)
            .IsRequired();

        builder.Property(p => p.LastUsedAt)
            .IsRequired(false);

        builder.Property(p => p.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(p => p.TrustScore)
            .IsRequired()
            .HasDefaultValue(50);

        // Indexes
        builder.HasIndex(p => p.UserId)
            .HasDatabaseName("IX_MfaPushDevices_UserId");

        builder.HasIndex(p => new { p.UserId, p.DeviceId })
            .IsUnique()
            .HasDatabaseName("IX_MfaPushDevices_UserId_DeviceId");

        builder.HasIndex(p => new { p.UserId, p.IsActive })
            .HasDatabaseName("IX_MfaPushDevices_UserId_IsActive");

        builder.HasIndex(p => p.MfaMethodId)
            .HasDatabaseName("IX_MfaPushDevices_MfaMethodId");

        builder.HasIndex(p => p.PushToken)
            .HasDatabaseName("IX_MfaPushDevices_PushToken");

        // Relationships
        builder.HasOne(p => p.MfaMethod)
            .WithMany()
            .HasForeignKey(p => p.MfaMethodId)
            .OnDelete(DeleteBehavior.Cascade);

        // User relationship without cascade to avoid circular paths
        builder.HasOne<Domain.Entities.Identity.AppUser>()
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
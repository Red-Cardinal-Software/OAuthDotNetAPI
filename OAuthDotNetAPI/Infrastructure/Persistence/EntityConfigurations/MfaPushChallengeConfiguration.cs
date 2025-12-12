using Domain.Entities.Security;
using Infrastructure.Persistence.EntityConfigurations.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations;

/// <summary>
/// Entity Framework Core configuration for the MfaPushChallenge entity.
/// </summary>
internal class MfaPushChallengeConfiguration : EntityTypeConfiguration<MfaPushChallenge>
{
    protected override void PerformConfiguration(EntityTypeBuilder<MfaPushChallenge> builder)
    {
        // Table configuration
        builder.ToTable("MfaPushChallenges", "Security");
        
        // Primary Key
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .ValueGeneratedNever();
        
        // Properties
        builder.Property(c => c.UserId)
            .IsRequired();

        builder.Property(c => c.DeviceId)
            .IsRequired();

        builder.Property(c => c.ChallengeCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.SessionId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.IpAddress)
            .IsRequired()
            .HasMaxLength(45); // IPv6 max length

        builder.Property(c => c.UserAgent)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(c => c.Location)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.ExpiresAt)
            .IsRequired();

        builder.Property(c => c.RespondedAt)
            .IsRequired(false);

        builder.Property(c => c.Status)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(ChallengeStatus.Pending);

        builder.Property(c => c.Response)
            .HasConversion<int>()
            .HasDefaultValue(ChallengeResponse.None)
            .IsRequired(false);

        builder.Property(c => c.ResponseSignature)
            .HasMaxLength(1024)
            .IsRequired(false);

        builder.Property(c => c.ContextData)
            .HasMaxLength(2000)
            .IsRequired(false);

        // Indexes
        builder.HasIndex(c => c.UserId)
            .HasDatabaseName("IX_MfaPushChallenges_UserId");

        builder.HasIndex(c => c.DeviceId)
            .HasDatabaseName("IX_MfaPushChallenges_DeviceId");

        builder.HasIndex(c => new { c.UserId, c.Status })
            .HasDatabaseName("IX_MfaPushChallenges_UserId_Status");

        builder.HasIndex(c => c.SessionId)
            .HasDatabaseName("IX_MfaPushChallenges_SessionId");

        builder.HasIndex(c => c.ChallengeCode)
            .IsUnique()
            .HasDatabaseName("IX_MfaPushChallenges_ChallengeCode");

        builder.HasIndex(c => c.ExpiresAt)
            .HasDatabaseName("IX_MfaPushChallenges_ExpiresAt");

        builder.HasIndex(c => c.CreatedAt)
            .HasDatabaseName("IX_MfaPushChallenges_CreatedAt");

        // Relationships
        builder.HasOne(c => c.Device)
            .WithMany()
            .HasForeignKey(c => c.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);
            
        // User relationship without cascade to avoid circular paths
        builder.HasOne<Domain.Entities.Identity.AppUser>()
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
using Domain.Entities.Identity;
using Infrastructure.Persistence.EntityConfigurations.Base;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations;

internal class PasswordResetTokenConfiguration : EntityTypeConfiguration<PasswordResetToken>
{
    protected override void PerformConfiguration(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.Property(x => x.CreatedByIp).IsRequired().HasMaxLength(50);
        builder.Property(x => x.ClaimedByIp).HasMaxLength(50);
    }
}
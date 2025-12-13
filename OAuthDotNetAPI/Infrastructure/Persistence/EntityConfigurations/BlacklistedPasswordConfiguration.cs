using Infrastructure.Persistence.EntityConfigurations.Base;
using Infrastructure.Security.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations;

internal class BlacklistedPasswordConfiguration : EntityTypeConfiguration<BlacklistedPassword>
{
    protected override void PerformConfiguration(EntityTypeBuilder<BlacklistedPassword> builder)
    {
        builder.ToTable("BlacklistedPasswords", "Security");

        builder.Property(p => p.HashedPassword).IsRequired();
    }
}
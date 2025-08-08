using Domain.Entities.Identity;
using Infrastructure.Persistence.EntityConfigurations.Base;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations;

internal class PrivilegeConfiguration : EntityTypeConfiguration<Privilege>
{
    protected override void PerformConfiguration(EntityTypeBuilder<Privilege> builder)
    {
        builder.Property(p => p.Name).IsRequired().HasMaxLength(50);
        builder.Property(p => p.Description).HasMaxLength(200);
    }
}

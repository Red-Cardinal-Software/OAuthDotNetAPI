using Domain.Entities.Identity;
using Infrastructure.Persistence.EntityConfigurations.Base;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations;

internal class RoleConfiguration : EntityTypeConfiguration<Role>
{
    protected override void PerformConfiguration(EntityTypeBuilder<Role> builder)
    {
        builder.Property(x => x.Name).IsRequired().HasMaxLength(50);
    }
}

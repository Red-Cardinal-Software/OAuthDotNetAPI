using Domain.Entities.Identity;
using Infrastructure.Persistence.EntityConfigurations.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations;

internal class RoleConfiguration : EntityTypeConfiguration<Role>
{
    protected override void PerformConfiguration(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles", "Identity");

        builder.Property(x => x.Name).IsRequired().HasMaxLength(50);

        // Configure many-to-many relationship with Privilege
        builder.HasMany(r => r.Privileges)
               .WithMany()
               .UsingEntity("RolePrivileges",
                   j => j.ToTable("RolePrivileges", "Identity"));
    }
}

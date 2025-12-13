using Domain.Entities.Identity;
using Infrastructure.Persistence.EntityConfigurations.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations;

internal class OrganizationConfiguration : EntityTypeConfiguration<Organization>
{
    protected override void PerformConfiguration(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("Organizations", "Identity");

        builder.Property(p => p.Name).IsRequired().HasMaxLength(100);
    }
}

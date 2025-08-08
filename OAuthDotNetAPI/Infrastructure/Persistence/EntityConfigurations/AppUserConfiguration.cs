using Domain.Entities.Identity;
using Infrastructure.Persistence.EntityConfigurations.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations;

internal class AppUserConfiguration : EntityTypeConfiguration<AppUser>
{
    protected override void PerformConfiguration(EntityTypeBuilder<AppUser> builder)
    {
        builder.Property(p => p.Username).IsRequired().HasMaxLength(200);
        builder.Property(p => p.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(p => p.LastName).IsRequired().HasMaxLength(100);
        builder.OwnsOne(u => u.Password, owned =>
        {
            owned.Property(p => p.Value)
                .HasColumnName("PasswordHash")
                .IsRequired();
        });
    }
}

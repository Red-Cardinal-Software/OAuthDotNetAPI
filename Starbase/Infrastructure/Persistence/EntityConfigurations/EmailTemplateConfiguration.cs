using Domain.Entities.Configuration;
using Infrastructure.Persistence.EntityConfigurations.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations;

internal class EmailTemplateConfiguration : EntityTypeConfiguration<EmailTemplate>
{
    protected override void PerformConfiguration(EntityTypeBuilder<EmailTemplate> builder)
    {
        builder.ToTable("EmailTemplates", "Configuration");

        builder.Property(e => e.Key).IsRequired();
        builder.Property(e => e.Subject).IsRequired();
        builder.Property(e => e.Body).IsRequired();
        builder.Property(e => e.IsHtml).IsRequired();
    }
}
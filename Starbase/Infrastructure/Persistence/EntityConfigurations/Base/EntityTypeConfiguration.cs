using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.EntityConfigurations.Base;

/// <summary>
/// Base class for all entity type configurations.
/// Override <see cref="PerformConfiguration"/> instead of implementing Configure directly.
/// </summary>
internal abstract class EntityTypeConfiguration<T> : IEntityTypeConfiguration<T> where T : class
{
    /// <summary>
    /// Configures the entity type using the provided <see cref="EntityTypeBuilder{T}"/>.
    /// </summary>
    /// <param name="builder">An <see cref="EntityTypeBuilder{T}"/> object used to configure the entity type.</param>
    protected abstract void PerformConfiguration(EntityTypeBuilder<T> builder);

    /// <summary>
    /// Invokes the <see cref="PerformConfiguration"/> method to configure the entity type with the given <see cref="EntityTypeBuilder{T}"/>.
    /// </summary>
    /// <param name="builder">An <see cref="EntityTypeBuilder{T}"/> object used to configure the entity type.</param>
    public void Configure(EntityTypeBuilder<T> builder)
    {
        PerformConfiguration(builder);
    }
}
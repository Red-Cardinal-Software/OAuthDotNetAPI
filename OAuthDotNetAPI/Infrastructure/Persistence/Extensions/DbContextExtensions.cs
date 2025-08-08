using System.Reflection;
using Infrastructure.Persistence.Seeders;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Extensions;

/// <summary>
/// Provides extension methods for performing operations on DbContext related to entity seeding.
/// </summary>
/// <remarks>
/// This class contains methods that automatically locate and execute seeders implementing the IEntitySeeder interface.
/// It relies on the presence of types in loaded assemblies that are decorated with the DbDataSeederAttribute.
/// </remarks>
public static class DbContextExtensions
{
    /// <summary>
    /// Applies seed data to the specified <see cref="DbContext"/> by executing all configured entity seeders.
    /// </summary>
    /// <param name="context">
    /// The <see cref="DbContext"/> instance on which the seeders will operate.
    /// </param>
    /// <remarks>
    /// This method identifies all available entity seeders that implement the <see cref="IEntitySeeder"/> interface
    /// and executes their seeding logic. The seeders are located dynamically from the loaded assemblies.
    /// </remarks>
    public static void ApplySeedData(this DbContext context)
    {
        var seederTypes = GetSeederTypes();

        foreach (var seeder in seederTypes)
        {
            if (Activator.CreateInstance(seeder) is IEntitySeeder entitySeeder)
            {
                entitySeeder.PerformSeeding(context);
            }
        }
    }

    /// <summary>
    /// Asynchronously applies seed data to the specified <see cref="DbContext"/> by executing all configured entity seeders.
    /// </summary>
    /// <param name="context">
    /// The <see cref="DbContext"/> instance on which the asynchronous seeding operations will be performed.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> that represents the asynchronous operation. The task will complete once all seeders have finished execution.
    /// </returns>
    /// <remarks>
    /// This method dynamically locates all entity seeders that implement the <see cref="IEntitySeeder"/> interface
    /// from the loaded assemblies and performs their seeding logic asynchronously.
    /// </remarks>
    public static async Task ApplySeedDataAsync(this DbContext context)
    {
        var seederTypes = GetSeederTypes();

        foreach(var seeder in seederTypes)
        {
            if (Activator.CreateInstance(seeder) is IEntitySeeder entitySeeder)
            {
                await entitySeeder.PerformSeedingAsync(context);
            }
        }
    }

    /// <summary>
    /// Retrieves all types implementing the <see cref="IEntitySeeder"/> interface that are
    /// decorated with the <see cref="DbDataSeederAttribute"/> from the currently loaded assemblies.
    /// </summary>
    /// <returns>
    /// A collection of <see cref="Type"/> instances representing the entity seeders that match the specified criteria.
    /// </returns>
    /// <remarks>
    /// This method scans the loaded assemblies in the application's current domain to identify classes that:
    /// - Implement the <see cref="IEntitySeeder"/> interface.
    /// - Are non-abstract, concrete classes.
    /// - Are marked with the <see cref="DbDataSeederAttribute"/>.
    /// </remarks>
    private static IEnumerable<Type> GetSeederTypes()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location) &&
                               assembly.Location.Contains("Infrastructure"))
            .SelectMany(x => x.GetTypes())
            .Where(type => type.IsClass && !type.IsAbstract && typeof(IEntitySeeder).IsAssignableFrom(type) &&
                           type.GetCustomAttribute<DbDataSeederAttribute>() is not null);
    }
}

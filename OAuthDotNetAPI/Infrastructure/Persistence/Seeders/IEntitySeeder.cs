using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Seeders;

/// <summary>
/// Defines a contract for seeding entities into a database.
/// </summary>
/// <remarks>
/// Implement this interface to provide logic for populating a database with seed data.
/// This can be done synchronously or asynchronously, depending on the needs of the implementing class.
/// Though to make things easier, it is recommended to have the synchronous method wait on the asynchronous one.
/// </remarks>
public interface IEntitySeeder
{
    /// <summary>
    /// Performs the synchronous seeding of data into the specified database context.
    /// </summary>
    /// <param name="dbContext">
    /// An instance of <see cref="DbContext"/> representing the database into which the data will be seeded.
    /// </param>
    /// <remarks>
    /// This method executes the seeding process synchronously by invoking the asynchronous counterpart
    /// and waiting for its completion. The seeding process is responsible for initializing and populating
    /// the database with predefined entities or default values.
    /// </remarks>
    void PerformSeeding(DbContext dbContext);

    /// <summary>
    /// Asynchronously performs the seeding of data into the specified database context.
    /// </summary>
    /// <param name="dbContext">
    /// An instance of <see cref="DbContext"/> representing the database into which the seed data will be inserted.
    /// </param>
    /// <returns>
    /// A <see cref="Task"/> representing the asynchronous operation of the seeding process.
    /// </returns>
    /// <remarks>
    /// This method initializes and populates the database with predefined entities or default values.
    /// Implementations should ensure idempotency, allowing the method to be safely invoked multiple times without duplicating data.
    /// </remarks>
    Task PerformSeedingAsync(DbContext dbContext);
}
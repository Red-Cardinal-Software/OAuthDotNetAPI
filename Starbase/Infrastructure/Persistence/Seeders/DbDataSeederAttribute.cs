namespace Infrastructure.Persistence.Seeders;

/// <summary>
/// Represents an attribute used to mark a class as a database data seeder.
/// </summary>
/// <remarks>
/// Classes decorated with this attribute are identified as data seeders responsible for populating
/// a database with initial or predefined data during application setup or migrations.
/// </remarks>
/// <seealso cref="IEntitySeeder"/>
/// <example>
/// This attribute is used in conjunction with classes that implement the IEntitySeeder interface.
/// </example>
public class DbDataSeederAttribute : Attribute;

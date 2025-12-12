using Domain.Entities.Configuration;
using Domain.Entities.Identity;
using Domain.Entities.Security;
using Infrastructure.Persistence.Extensions;
using Infrastructure.Security.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options, IConfiguration configuration) : DbContext(options)
{
    // Auth Tables
    public DbSet<AppUser> AppUsers { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<Privilege> Privileges { get; set; }
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
    public DbSet<BlacklistedPassword> BlacklistedPasswords { get; set; }

    // Configuration Tables
    public DbSet<EmailTemplate> EmailTemplates { get; set; }

    // Security Tables
    public DbSet<LoginAttempt> LoginAttempts { get; set; }
    public DbSet<AccountLockout> AccountLockouts { get; set; }

    // App Tables
    public DbSet<Organization> Organizations { get; set; }

    // Model Creating and Configuring
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        base.OnConfiguring(options);

        // Use any database for connection, this example is using SqlServer
        // The configuration path shown makes it easy to set up config in Azure if it is hosted there
        options.UseSqlServer(configuration["ConnectionStrings-DefaultConnection"])
            .UseSeeding((context, _) =>
            {
                context.ApplySeedData();
                context.SaveChanges();
            })
            .UseAsyncSeeding(async (context, _, cancellationToken) =>
            {
                await context.ApplySeedDataAsync();
                await context.SaveChangesAsync(cancellationToken);
            });

        // If you want to use other databases, here are some examples

        //PostgreSQL
        // options.UseNpgsql(connectionString);

        //MySql
        // options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

        //Sqlite - Note: Change "yourdbfile.db" to whatever file you are using
        // options.UseSqlite("Data Source=yourdbfile.db");

        //Oracle
        // options.UseOracle(connectionString);
    }

}

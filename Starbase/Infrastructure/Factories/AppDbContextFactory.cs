using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Factories;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.Development.json", optional: false)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        var connectionString = configuration.GetConnectionString("SqlConnection");

//#if (UsePostgreSql)
        optionsBuilder.UseNpgsql(connectionString);
//#elseif (UseOracle)
        optionsBuilder.UseOracle(connectionString);
//#else
        optionsBuilder.UseSqlServer(connectionString);
//#endif

        return new AppDbContext(optionsBuilder.Options, configuration);
    }
}

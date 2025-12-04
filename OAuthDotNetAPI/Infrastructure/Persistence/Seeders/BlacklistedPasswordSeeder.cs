using System.Reflection;
using System.Text;
using Application.Common.Utilities;
using Infrastructure.Security.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence.Seeders;

[DbDataSeeder]
public class BlacklistedPasswordSeeder : IEntitySeeder
{
    public void PerformSeeding(DbContext dbContext)
    {
        PerformSeedingAsync(dbContext).Wait();
    }

    public async Task PerformSeedingAsync(DbContext dbContext)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "Infrastructure.Security.BlacklistedPasswordSet.txt";

        await using var stream = assembly.GetManifestResourceStream(resourceName) ?? throw new FileNotFoundException("Embedded blacklist file not found");

        var blacklistedHashes = dbContext.Set<BlacklistedPassword>();
        var existingHashes = new HashSet<string>(await blacklistedHashes.Select(x => x.HashedPassword).ToListAsync());

        var newEntries = new List<BlacklistedPassword>();

        using var reader = new StreamReader(stream, Encoding.UTF8);

        string? line;
        while ((line = await reader.ReadLineAsync()) is not null)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            var hash = BlacklistedPasswordHasher.GenerateHashedPasswordStringForBlacklistCheck(line);

            if (!existingHashes.Contains(hash))
            {
                newEntries.Add(new BlacklistedPassword { HashedPassword = hash });
            }
        }

        if (newEntries.Count != 0)
        {
            await dbContext.AddRangeAsync(newEntries);
            await dbContext.SaveChangesAsync();
        }
    }
}

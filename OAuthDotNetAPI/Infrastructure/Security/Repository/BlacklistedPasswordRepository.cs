using Application.Common.Utilities;
using Application.Interfaces.Security;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Security.Repository;

public class BlacklistedPasswordRepository(AppDbContext context) : IBlacklistedPasswordRepository
{
    public async Task<bool> IsPasswordBlacklistedAsync(string password)
    {
        var hash = BlacklistedPasswordHasher.GenerateHashedPasswordStringForBlacklistCheck(password);
        return await context.BlacklistedPasswords.AnyAsync(p => p.HashedPassword == hash);
    }
}
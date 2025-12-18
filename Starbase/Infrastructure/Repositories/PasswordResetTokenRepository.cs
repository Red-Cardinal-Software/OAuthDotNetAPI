using Application.Interfaces.Persistence;
using Application.Interfaces.Repositories;
using Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Provides a repository for managing operations related to PasswordResetToken entities.
/// </summary>
public class PasswordResetTokenRepository(ICrudOperator<PasswordResetToken> passwordResetTokenCrudOperator) : IPasswordResetTokenRepository
{
    public Task<PasswordResetToken?> GetPasswordResetTokenAsync(Guid id) =>
        GetAllWithChildren()
            .FirstOrDefaultAsync(prt => prt.Id == id);

    public async Task<PasswordResetToken> CreateResetPasswordTokenAsync(PasswordResetToken token)
    {
        var newToken = await passwordResetTokenCrudOperator.AddAsync(token);
        return newToken;
    }

    public Task<List<PasswordResetToken>> GetAllUnclaimedResetTokensForUserAsync(Guid userId) =>
        GetAllWithChildren()
            .Where(prt => prt.ClaimedDate == null && prt.AppUserId == userId)
            .ToListAsync();

    /// <summary>
    /// Retrieves all password reset tokens along with their related child entities.
    /// </summary>
    /// <returns>An <see cref="IQueryable{T}"/> representing all password reset tokens with their associated child entities included.</returns>
    private IQueryable<PasswordResetToken> GetAllWithChildren() =>
        passwordResetTokenCrudOperator.GetAll()
            .Include(prt => prt.AppUser);
}

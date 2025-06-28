using Application.Interfaces.Persistence;
using Application.Interfaces.Repositories;
using Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Represents a repository implementation for managing refresh tokens.
/// </summary>
public class RefreshTokenRepository(ICrudOperator<RefreshToken> refreshTokenCrudOperator) : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetRefreshTokenAsync(Guid refreshTokenId, Guid userId) =>
        refreshTokenCrudOperator.GetAll().FirstOrDefaultAsync(rt => rt.Id == refreshTokenId && rt.AppUserId == userId);

    public Task<RefreshToken> SaveRefreshTokenAsync(RefreshToken token) => refreshTokenCrudOperator.AddAsync(token);

    public async Task<bool> RevokeRefreshTokenFamilyAsync(Guid tokenFamily)
    {
        var refreshTokensInFamily = await refreshTokenCrudOperator.GetAll().Where(rt => rt.TokenFamily == tokenFamily).ToListAsync();
        refreshTokenCrudOperator.DeleteMany(refreshTokensInFamily);
        return true;
    }
}
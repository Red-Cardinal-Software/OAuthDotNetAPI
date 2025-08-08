using Domain.Entities.Identity;

namespace Application.Interfaces.Repositories;

/// <summary>
/// Defines the contract for managing refresh tokens in the application.
/// </summary>
public interface IRefreshTokenRepository
{
    /// <summary>
    /// Retrieves a refresh token by token ID and user ID.
    /// </summary>
    Task<RefreshToken?> GetRefreshTokenAsync(Guid refreshTokenId, Guid userId);

    /// <summary>
    /// Saves a new refresh token for a user.
    /// </summary>
    Task<RefreshToken> SaveRefreshTokenAsync(RefreshToken token);

    /// <summary>
    /// Revokes all refresh tokens in the specified family.
    /// </summary>
    Task<bool> RevokeRefreshTokenFamilyAsync(Guid tokenFamily);
}

using Domain.Entities.Identity;

namespace Application.Interfaces.Repositories;

/// <summary>
/// Represents a repository interface for managing password reset tokens.
/// </summary>
public interface IPasswordResetTokenRepository
{
    /// <summary>
    /// Retrieves a password reset token entity based on the provided identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the password reset token.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains
    /// the <see cref="PasswordResetToken"/> if found, or null if no matching token exists.</returns>
    Task<PasswordResetToken?> GetPasswordResetTokenAsync(Guid id);

    /// <summary>
    /// Retrieves all unclaimed password reset tokens associated with a specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user for whom to retrieve unclaimed reset tokens.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains
    /// a list of <see cref="PasswordResetToken"/> objects that are unclaimed for the specified user.</returns>
    public Task<List<PasswordResetToken>> GetAllUnclaimedResetTokensForUserAsync(Guid userId);

    /// <summary>
    /// Creates and persists a new password reset token in the repository.
    /// </summary>
    /// <param name="token">The <see cref="PasswordResetToken"/> entity to create and store.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the created <see cref="PasswordResetToken"/>.</returns>
    public Task<PasswordResetToken> CreateResetPasswordTokenAsync(PasswordResetToken token);
}

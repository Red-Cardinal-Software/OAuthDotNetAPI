using Application.Models;

namespace Application.Interfaces.Services;

/// <summary>
/// Defines a contract for password reset operations.
/// </summary>
public interface IPasswordResetService
{
    /// <summary>
    /// Resets a user's password using a provided reset token.
    /// </summary>
    /// <param name="token">The password reset token issued to the user.</param>
    /// <param name="password">The new password to be set for the user.</param>
    /// <param name="claimedByIpAddress">The IP address from which the password reset is being claimed.</param>
    /// <returns>A service response indicating whether the password reset was successful.</returns>
    Task<ServiceResponse<bool>> ResetPasswordWithTokenAsync(string token, string password, string claimedByIpAddress);

    /// <summary>
    /// Forces a password reset for a user by setting a new password.
    /// </summary>
    /// <param name="userId">The unique identifier of the user whose password is to be reset.</param>
    /// <param name="newPassword">The new password to be set for the user.</param>
    /// <returns>A service response indicating whether the password reset operation was successful.</returns>
    Task<ServiceResponse<bool>> ForcePasswordResetAsync(Guid userId, string newPassword);
}

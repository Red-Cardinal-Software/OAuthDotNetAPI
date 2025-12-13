using Domain.Entities.Identity;

namespace Application.Interfaces.Services;

/// <summary>
/// Defines the contract for a service that handles the sending of password-reset emails.
/// </summary>
public interface IPasswordResetEmailService
{
    /// <summary>
    /// Sends a password-reset email to the specified user with the provided reset token.
    /// </summary>
    /// <param name="user">The user who requested the password reset.</param>
    /// <param name="token">The password reset token associated with the user.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SendPasswordResetEmail(AppUser user, PasswordResetToken token);
}

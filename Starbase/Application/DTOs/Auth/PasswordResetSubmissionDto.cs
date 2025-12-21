using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Auth;

/// <summary>
/// Represents the data required to submit a password reset request.
/// Typically used in conjunction with a secure password reset link sent via email.
/// </summary>
public class PasswordResetSubmissionDto
{
    /// <summary>
    /// The unique token issued to the user for resetting their password.
    /// This is usually embedded in the reset link sent to the user's email.
    /// </summary>
    [Required(ErrorMessage = "Token is required")]
    [StringLength(512, ErrorMessage = "Token must not exceed 512 characters")]
    public required string Token { get; set; }

    /// <summary>
    /// The new password the user wishes to set.
    /// Should meet application-specific password complexity requirements.
    /// </summary>
    [Required(ErrorMessage = "Password is required")]
    [StringLength(128, ErrorMessage = "Password must not exceed 128 characters")]
    public required string Password { get; set; }
}
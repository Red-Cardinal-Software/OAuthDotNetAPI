using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Mfa;

/// <summary>
/// DTO for starting email MFA setup
/// </summary>
public class StartEmailMfaSetupDto
{
    /// <summary>
    /// The email address to use for MFA (can be different from login email)
    /// </summary>
    [Required(ErrorMessage = "Email address is required")]
    [EmailAddress(ErrorMessage = "Invalid email address format")]
    [StringLength(256, ErrorMessage = "Email address must not exceed 256 characters")]
    public string EmailAddress { get; set; } = string.Empty;
}
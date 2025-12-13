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
    [Required]
    [EmailAddress]
    public string EmailAddress { get; set; } = string.Empty;
}

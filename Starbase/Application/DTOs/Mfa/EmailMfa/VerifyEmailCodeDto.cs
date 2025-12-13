using System.ComponentModel.DataAnnotations;
using Application.DTOs.Validation;

namespace Application.DTOs.Mfa.EmailMfa;

/// <summary>
/// Request DTO for verifying an email MFA code.
/// </summary>
public class VerifyEmailCodeDto : IValidatableDto
{
    /// <summary>
    /// The MFA challenge ID this code verification is for.
    /// </summary>
    [Required(ErrorMessage = "Challenge ID is required")]
    public Guid ChallengeId { get; set; }

    /// <summary>
    /// The verification code received via email.
    /// </summary>
    [Required(ErrorMessage = "Verification code is required")]
    [StringLength(8, MinimumLength = 8, ErrorMessage = "Verification code must be 8 digits")]
    [RegularExpression(@"^\d{8}$", ErrorMessage = "Verification code must be 8 digits")]
    public string Code { get; set; } = string.Empty;
}
using System.ComponentModel.DataAnnotations;
using Application.DTOs.Validation;

namespace Application.DTOs.Mfa.EmailMfa;

/// <summary>
/// Request DTO for sending an email MFA verification code.
/// </summary>
public class SendEmailCodeDto : IValidatableDto
{
    /// <summary>
    /// The MFA challenge ID this code is being sent for.
    /// </summary>
    [Required(ErrorMessage = "Challenge ID is required")]
    public Guid ChallengeId { get; set; }

    /// <summary>
    /// The email address where the code should be sent.
    /// If not provided, will use the user's registered email.
    /// </summary>
    [EmailAddress(ErrorMessage = "Invalid email address format")]
    public string? EmailAddress { get; set; }
}
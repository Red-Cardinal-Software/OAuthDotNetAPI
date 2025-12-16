using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Mfa.WebAuthn;

/// <summary>
/// Request DTO for starting WebAuthn credential registration.
/// </summary>
public class StartRegistrationDto
{
    /// <summary>
    /// The MFA method ID this credential will be associated with.
    /// </summary>
    [Required(ErrorMessage = "MFA method ID is required")]
    public Guid MfaMethodId { get; set; }

    /// <summary>
    /// Optional display name for the credential.
    /// </summary>
    [StringLength(100, ErrorMessage = "Credential name must not exceed 100 characters")]
    public string? CredentialName { get; set; }
}

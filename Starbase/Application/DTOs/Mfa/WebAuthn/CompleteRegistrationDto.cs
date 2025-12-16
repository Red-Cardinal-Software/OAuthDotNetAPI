using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Mfa.WebAuthn;

/// <summary>
/// Request DTO for completing WebAuthn credential registration.
/// </summary>
public class CompleteRegistrationDto
{
    /// <summary>
    /// The MFA method ID this credential is being registered for.
    /// </summary>
    [Required(ErrorMessage = "MFA method ID is required")]
    public Guid MfaMethodId { get; set; }

    /// <summary>
    /// The challenge that was provided during registration start.
    /// </summary>
    [Required(ErrorMessage = "Challenge is required")]
    public string Challenge { get; set; } = string.Empty;

    /// <summary>
    /// The attestation response from the authenticator.
    /// </summary>
    [Required(ErrorMessage = "Attestation response is required")]
    public PublicKeyCredentialCreationResponse Response { get; set; } = null!;

    /// <summary>
    /// Optional name for the credential.
    /// </summary>
    [StringLength(100, ErrorMessage = "Credential name must not exceed 100 characters")]
    public string? CredentialName { get; set; }
}

/// <summary>
/// The public key credential creation response from the authenticator.
/// </summary>
public class PublicKeyCredentialCreationResponse
{
    /// <summary>
    /// The credential ID (base64 encoded).
    /// </summary>
    [Required]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The raw credential ID (base64 encoded).
    /// </summary>
    [Required]
    public string RawId { get; set; } = string.Empty;

    /// <summary>
    /// The credential type (should be "public-key").
    /// </summary>
    [Required]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The attestation response data.
    /// </summary>
    [Required]
    public AttestationResponse Response { get; set; } = null!;
}

/// <summary>
/// The attestation response containing the credential data.
/// </summary>
public class AttestationResponse
{
    /// <summary>
    /// The client data JSON (base64 encoded).
    /// </summary>
    [Required]
    public string ClientDataJSON { get; set; } = string.Empty;

    /// <summary>
    /// The attestation object containing the credential public key (base64 encoded).
    /// </summary>
    [Required]
    public string AttestationObject { get; set; } = string.Empty;
}

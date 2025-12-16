using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Mfa.WebAuthn;

/// <summary>
/// Request DTO for completing WebAuthn authentication.
/// </summary>
public class CompleteAuthenticationDto
{
    /// <summary>
    /// The credential ID being used for authentication.
    /// </summary>
    [Required(ErrorMessage = "Credential ID is required")]
    public string CredentialId { get; set; } = string.Empty;

    /// <summary>
    /// The challenge that was provided during authentication start.
    /// </summary>
    [Required(ErrorMessage = "Challenge is required")]
    public string Challenge { get; set; } = string.Empty;

    /// <summary>
    /// The assertion response from the authenticator.
    /// </summary>
    [Required(ErrorMessage = "Assertion response is required")]
    public PublicKeyCredentialAssertionResponse Response { get; set; } = null!;
}

/// <summary>
/// The public key credential assertion response from the authenticator.
/// </summary>
public class PublicKeyCredentialAssertionResponse
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
    /// The assertion response data.
    /// </summary>
    [Required]
    public AssertionResponse Response { get; set; } = null!;
}

/// <summary>
/// The assertion response containing the authentication data.
/// </summary>
public class AssertionResponse
{
    /// <summary>
    /// The client data JSON (base64 encoded).
    /// </summary>
    [Required]
    public string ClientDataJSON { get; set; } = string.Empty;

    /// <summary>
    /// The authenticator data (base64 encoded).
    /// </summary>
    [Required]
    public string AuthenticatorData { get; set; } = string.Empty;

    /// <summary>
    /// The assertion signature (base64 encoded).
    /// </summary>
    [Required]
    public string Signature { get; set; } = string.Empty;

    /// <summary>
    /// The user handle (base64 encoded) - optional.
    /// </summary>
    public string? UserHandle { get; set; }
}

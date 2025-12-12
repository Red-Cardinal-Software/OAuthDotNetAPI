using System.ComponentModel.DataAnnotations;

namespace Application.Common.Configuration;

/// <summary>
/// Configuration options for WebAuthn/FIDO2 authentication.
/// </summary>
public class WebAuthnOptions
{
    public const string SectionName = "WebAuthn";

    /// <summary>
    /// Gets or sets the allowed origins for WebAuthn requests.
    /// </summary>
    [Required(ErrorMessage = "At least one origin must be specified")]
    [MinLength(1, ErrorMessage = "At least one origin must be specified")]
    public string[] Origins { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the relying party name.
    /// </summary>
    [Required(ErrorMessage = "Relying party name is required")]
    [StringLength(100, ErrorMessage = "Relying party name cannot exceed 100 characters")]
    public string RelyingPartyName { get; set; } = "Starbase Template .NET API";

    /// <summary>
    /// Gets or sets the relying party identifier.
    /// </summary>
    [Required(ErrorMessage = "Relying party ID is required")]
    [StringLength(50, ErrorMessage = "Relying party ID cannot exceed 50 characters")]
    public string RelyingPartyId { get; set; } = "localhost";

    /// <summary>
    /// Gets or sets the timestamp drift tolerance in milliseconds.
    /// </summary>
    [Range(0, 600000, ErrorMessage = "Timestamp drift tolerance must be between 0 and 600000 milliseconds (10 minutes)")]
    public int TimestampDriftTolerance { get; set; } = 300000; // 5 minutes
}

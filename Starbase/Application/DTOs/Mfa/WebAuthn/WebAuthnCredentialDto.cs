namespace Application.DTOs.Mfa.WebAuthn;

/// <summary>
/// DTO representing a WebAuthn credential for display purposes.
/// </summary>
public class WebAuthnCredentialDto
{
    /// <summary>
    /// The credential ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The user-friendly name of the credential.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The type of authenticator (Platform or CrossPlatform).
    /// </summary>
    public string AuthenticatorType { get; set; } = string.Empty;

    /// <summary>
    /// Supported transports for this credential.
    /// </summary>
    public string[] Transports { get; set; } = Array.Empty<string>();

    /// <summary>
    /// When the credential was registered.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// When the credential was last used.
    /// </summary>
    public DateTimeOffset? LastUsedAt { get; set; }

    /// <summary>
    /// Whether the credential is currently active.
    /// </summary>
    public bool IsActive { get; set; }
}

namespace Application.DTOs.Mfa;

/// <summary>
/// DTO containing the information needed for a user to set up Email MFA.
/// </summary>
public class EmailSetupDto
{
    /// <summary>
    /// The unique ID of the MFA method being set up.
    /// </summary>
    public Guid MfaMethodId { get; init; }

    /// <summary>
    /// The email address where verification codes will be sent.
    /// </summary>
    public string EmailAddress { get; init; } = string.Empty;

    /// <summary>
    /// Instructions for the user on how to complete setup.
    /// </summary>
    public string Instructions { get; init; } = string.Empty;

    /// <summary>
    /// When this setup request expires.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; init; }

    /// <summary>
    /// Whether a verification code has been sent.
    /// </summary>
    public bool CodeSent { get; init; }

    /// <summary>
    /// Optional message about the verification process.
    /// </summary>
    public string? Message { get; init; }
}

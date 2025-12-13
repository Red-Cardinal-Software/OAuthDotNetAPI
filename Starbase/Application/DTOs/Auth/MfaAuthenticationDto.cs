using Domain.Entities.Security;

namespace Application.DTOs.Auth;

/// <summary>
/// DTO for completing MFA verification during authentication.
/// </summary>
public class CompleteMfaDto
{
    /// <summary>
    /// The challenge token received from the initial login attempt.
    /// </summary>
    public required string ChallengeToken { get; set; }

    /// <summary>
    /// The MFA verification code (6-digit TOTP, recovery code, etc.).
    /// </summary>
    public required string Code { get; set; }

    /// <summary>
    /// Whether this code is a recovery code rather than a TOTP code.
    /// </summary>
    public bool IsRecoveryCode { get; set; } = false;

    /// <summary>
    /// Optional: Specific MFA method ID to use for verification.
    /// If not provided, will use the default method or challenge-specified method.
    /// </summary>
    public Guid? MfaMethodId { get; set; }
}

/// <summary>
/// DTO containing available MFA methods for a challenge.
/// </summary>
public class AvailableMfaMethodDto
{
    /// <summary>
    /// The MFA method ID.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// The type of MFA method.
    /// </summary>
    public MfaType Type { get; init; }

    /// <summary>
    /// User-friendly name for this method.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Whether this is the user's default method.
    /// </summary>
    public bool IsDefault { get; init; }

    /// <summary>
    /// Display information for the client (e.g., "Authenticator App", "***-***-1234").
    /// </summary>
    public string DisplayInfo { get; init; } = string.Empty;

    /// <summary>
    /// Instructions for using this method.
    /// </summary>
    public string Instructions { get; init; } = string.Empty;
}

/// <summary>
/// DTO for MFA challenge result from login attempt.
/// </summary>
public class MfaChallengeDto
{
    /// <summary>
    /// The unique challenge token for this MFA session.
    /// </summary>
    public string ChallengeToken { get; init; } = string.Empty;

    /// <summary>
    /// Available MFA methods for this user.
    /// </summary>
    public AvailableMfaMethodDto[] AvailableMethods { get; init; } = Array.Empty<AvailableMfaMethodDto>();

    /// <summary>
    /// When this challenge expires.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; init; }

    /// <summary>
    /// The Number of verification attempts remaining.
    /// </summary>
    public int AttemptsRemaining { get; init; }

    /// <summary>
    /// Instructions for the user.
    /// </summary>
    public string Instructions { get; init; } = string.Empty;
}

using Application.DTOs.Auth;

namespace Application.DTOs.Jwt;

/// <summary>
/// Token response sent to the client after successful authentication or MFA challenge.
/// </summary>
public class JwtResponseDto
{
    /// <summary>JWT access token (only present after complete authentication)</summary>
    public string? Token { get; set; }

    /// <summary>Refresh token used to obtain a new access token (only present after complete authentication)</summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Indicates if the user must reset their password before continuing.
    /// </summary>
    public bool ForceReset { get; set; }

    /// <summary>
    /// Whether this response requires MFA verification before tokens are issued.
    /// </summary>
    public bool RequiresMfa { get; set; }

    /// <summary>
    /// MFA challenge information (only present when RequiresMfa is true).
    /// </summary>
    public MfaChallengeDto? MfaChallenge { get; set; }
}

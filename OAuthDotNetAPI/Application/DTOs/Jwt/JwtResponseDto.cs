namespace Application.DTOs.Jwt;

/// <summary>
/// Token response sent to the client after successful authentication.
/// </summary>
public class JwtResponseDto
{
    /// <summary>JWT access token</summary>
    public required string Token { get; set; } = null!;

    /// <summary>Refresh token used to obtain a new access token</summary>
    public required string RefreshToken { get; set; } = null!;

    /// <summary>
    /// Indicates if the user must reset their password before continuing.
    /// </summary>
    public bool ForceReset { get; set; }
}

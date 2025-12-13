namespace Application.DTOs.Mfa.WebAuthn;

/// <summary>
/// Request DTO for starting WebAuthn authentication.
/// No body parameters needed as user is identified from the auth token.
/// </summary>
public class StartAuthenticationDto
{
    // No properties needed - user ID comes from authentication context
}

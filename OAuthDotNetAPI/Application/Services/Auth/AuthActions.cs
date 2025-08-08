namespace Application.Services.Auth;

/// <summary>
/// Defines constant actions related to authentication operations.
/// </summary>
public static class AuthActions
{
    public const string Login = "LOGIN";
    public const string Logout = "LOGOUT";
    public const string RefreshJwtToken = "REFRESH_TOKEN";
    public const string RequestPasswordReset = "REQUEST_PASSWORD_RESET";
}

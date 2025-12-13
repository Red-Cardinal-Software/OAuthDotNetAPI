namespace Application.Services.Auth;

/// <summary>
/// Provides utility methods for creating target information strings, primarily used for logging purposes within the authentication process.
/// </summary>
public static class AuthTargets
{
    public static string User(string username) => $"Username:{username}";
}

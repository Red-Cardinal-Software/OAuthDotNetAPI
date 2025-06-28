namespace Application.Common.Constants;

/// <summary>
/// Provides a collection of custom claim type identifiers used for managing user-related claims within the application.
/// </summary>
/// <remarks>
/// These claim types are utilized in conjunction with user identity and role-based authentication,
/// enabling extensions to the standard claim types to include application-specific properties.
/// The constants defined within this class are used to assign and retrieve user-specific claims,
/// such as roles, status, and personal information.
/// </remarks>
public static class CustomClaimTypes
{
    public const string FirstName = "FirstName";
    public const string LastName = "LastName";
    public const string Admin = "Admin";
    public const string SuperAdmin = "SuperAdmin";
    public const string IsUserActive = "IsUserActive";
}
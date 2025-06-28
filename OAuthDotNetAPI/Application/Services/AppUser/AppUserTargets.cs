namespace Application.Services.AppUser;

/// <summary>
/// Provides utility methods to generate string representations of user
/// and organization identifiers for logging and tracking purposes.
/// </summary>
public static class AppUserTargets
{
    public static string Org(Guid id) => $"OrgId:{id}";
    public static string User(Guid id) => $"UserId:{id}";
    public static string UserInOrg(Guid userId, Guid orgId) => $"UserId:{userId},OrgId:{orgId}";
}
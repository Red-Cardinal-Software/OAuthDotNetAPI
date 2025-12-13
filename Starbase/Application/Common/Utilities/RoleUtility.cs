using System.Security.Claims;
using Domain.Constants;
using Domain.Entities.Identity;

namespace Application.Common.Utilities;

public static class RoleUtility
{
    public static Guid GetOrgIdFromClaims(ClaimsPrincipal user) =>
        Guid.Parse(user.Claims.AsQueryable().FirstOrDefault(uc => uc.Type.Equals("Organization"))!.Value);


    public static bool IsUserSuperAdmin(ClaimsPrincipal user)
    {
        var roleClaims = GetClaimsFromPrincipal(user);
        return roleClaims.Any(role => role.Value == PredefinedRoles.SuperAdmin);
    }

    public static bool IsUserAdmin(ClaimsPrincipal user)
    {
        var roleClaims = GetClaimsFromPrincipal(user);
        return roleClaims.Any(role => role.Value == PredefinedRoles.Admin);
    }

    public static bool IsUserAdminOrSuperAdmin(ClaimsPrincipal user) => IsUserAdmin(user) || IsUserSuperAdmin(user);

    public static Guid GetUserIdFromClaims(ClaimsPrincipal user)
    {
        return Guid.Parse(user.Claims.AsQueryable().FirstOrDefault(uc => uc.Type.Equals(ClaimTypes.NameIdentifier))!.Value);
    }

    public static string GetUserNameFromClaim(ClaimsPrincipal user)
    {
        return user.Claims.AsQueryable().FirstOrDefault(uc => uc.Type.Equals(ClaimTypes.Name))!.Value;
    }

    private static List<Claim> GetClaimsFromPrincipal(ClaimsPrincipal user)
    {
        return user.Claims.AsQueryable().Where(uc => uc.Type.Equals(ClaimTypes.Role)).ToList();
    }

    public static IEnumerable<Role> GetRoleNamesFromClaims(ClaimsPrincipal user, List<Role> roles)
    {
        var result = new List<Role>();
        var roleClaims = GetClaimsFromPrincipal(user);
        foreach (var roleClaim in roleClaims)
        {
            var thisRole = roles.FirstOrDefault(r => r.Name.ToLowerInvariant() == roleClaim.Value.ToLowerInvariant());
            if (thisRole is not null)
            {
                result.Add(thisRole);
            }
        }

        return result;
    }

    public static bool IsUserActive(ClaimsPrincipal user)
    {
        return user.Claims.AsQueryable().FirstOrDefault(uc => uc.Type.Equals("IsUserActive"))!.Value == "True";
    }
}

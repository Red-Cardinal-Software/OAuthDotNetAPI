using System.Security.Claims;
using Application.Common.Constants;
using Domain.Constants;
using Domain.Entities.Identity;

namespace Application.Common.Utilities;

/// <summary>
/// Utility class providing methods to build claims for an authenticated user.
/// </summary>
public static class ClaimsUtility
{
    /// <summary>
    /// Builds a list of claims for a given user, based on their assigned roles and system role definitions.
    /// </summary>
    /// <param name="user">The user for whom claims are to be generated.</param>
    /// <param name="allSystemRoles">A collection of all available system roles, used to identify role names.</param>
    /// <returns>A list of claims containing role-based and user-specific information.</returns>
    public static List<Claim> BuildClaimsForUser(AppUser user, IReadOnlyList<Role> allSystemRoles)
    {
        var claimsToAdd = user.Roles
            .Select(role => new Claim(ClaimTypes.Role, role.Id.ToString()))
            .Where(c => !string.IsNullOrWhiteSpace(c.Value))
            .ToList();

        var roleNames = user.Roles
            .Select(r => allSystemRoles.FirstOrDefault(sys => sys.Id == r.Id)?.Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToList();

        var isAdmin = roleNames.Contains(PredefinedRoles.Admin) || roleNames.Contains(PredefinedRoles.SuperAdmin);
        var isSuperAdmin = roleNames.Contains(PredefinedRoles.SuperAdmin);

        claimsToAdd.Add(new Claim(CustomClaimTypes.FirstName, user.FirstName));
        claimsToAdd.Add(new Claim(CustomClaimTypes.LastName, user.LastName));
        claimsToAdd.Add(new Claim(CustomClaimTypes.Admin, isAdmin.ToString()));
        claimsToAdd.Add(new Claim(CustomClaimTypes.SuperAdmin, isSuperAdmin.ToString()));
        claimsToAdd.Add(new Claim(CustomClaimTypes.IsUserActive, user.Active.ToString()));

        return claimsToAdd;
    }

}

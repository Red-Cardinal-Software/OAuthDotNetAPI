using System.Security.Claims;
using Application.Common.Interfaces;
using Application.Common.Utilities;
using Domain.Constants;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Security;

public class UserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    public ClaimsPrincipal User => httpContextAccessor.HttpContext?.User ??
                                    throw new InvalidOperationException("No HttpContext available.");

    public Guid GetUserId() => RoleUtility.GetUserIdFromClaims(User);
    public Guid GetOrganizationId() => RoleUtility.GetOrgIdFromClaims(User);
    public bool IsInRole(string role) => User.IsInRole(role);
    public bool IsSuperAdmin() => IsInRole(PredefinedRoles.SuperAdmin);

}

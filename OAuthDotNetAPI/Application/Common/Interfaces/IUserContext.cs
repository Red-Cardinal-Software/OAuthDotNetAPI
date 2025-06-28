using System.Security.Claims;

namespace Application.Common.Interfaces;

public interface IUserContext
{
    Guid GetUserId();
    Guid GetOrganizationId();
    bool IsInRole(string role);
    bool IsSuperAdmin();
    ClaimsPrincipal User { get; }
}